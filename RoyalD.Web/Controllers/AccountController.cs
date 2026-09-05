using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;
using RoyalD.Web.Services;
using System.Security.Claims;

namespace RoyalD.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AccountController> _logger;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public AccountController(AppDbContext db, ILogger<AccountController> logger, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var allowedPages = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
                if (User.IsInRole("admin") || allowedPages.Contains("dashboard")) return RedirectToAction("Index", "Dashboard");
                return RedirectToAction("Index", "SalesBill");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null, string? lat = null, string? lng = null, string? locationName = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Username = username;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
                _db.AuditLogs.Add(new AuditLog
                {
                    Username = username,
                    Action = "LOGIN_FAILED",
                    Detail = "Attempted login with incorrect credentials",
                    Latitude = lat ?? "",
                    Longitude = lng ?? "",
                    Area = !string.IsNullOrEmpty(locationName) ? locationName : GeoLocationHelper.ReverseGeocode(lat, lng),
                    IPAddress = GetRealIpAddress(),
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
                return View();
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "บัญชีนี้ถูกระงับการใช้งาน");
                return View();
            }

            // บังคับส่งพิกัด GPS สำหรับทุกบัญชี
            if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lng))
            {
                ModelState.AddModelError("", "กรุณาอนุญาตการเข้าถึงตำแหน่ง GPS ก่อนเข้าสู่ระบบ");
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Action = "LOGIN_BLOCKED_NOLOCATION",
                    Detail = $"Login blocked: No GPS provided for user {user.FullName}",
                    Latitude = "",
                    Longitude = "",
                    Area = "ไม่ระบุตำแหน่ง",
                    IPAddress = GetRealIpAddress(),
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
                return View();
            }

            // ตรวจสอบสถานะผู้แทนขาย
            bool isMasterAdmin = user.Role == "admin" || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase);
            bool isSalesRep = !isMasterAdmin && (user.Position == "ผู้แทนขาย" || user.Position == "พนักงานขาย" || user.Position.Contains("ผู้แทน") || user.Position.Contains("พนักงานขาย"));

            // สิทธิ์การดาวน์โหลดและแคปหน้าจอ:
            // 1. แอดมิน (Admin) -> ปลดล็อค 100%
            // 2. ผู้แทนขาย (Sales Rep) -> ถูกล็อกความปลอดภัยเสมอ (ห้ามดาวน์โหลด และห้ามแคปจอ)
            // 3. ตำแหน่งอื่นๆ -> ขึ้นอยู่กับที่แอดมินติ๊กเลือก
            bool canDownloadFinal = isMasterAdmin || (!isSalesRep && user.CanDownload);
            bool canCaptureFinal = isMasterAdmin || (!isSalesRep && user.CanScreenCapture);

            // สร้าง Claims รวมสิทธิ์การใช้งาน
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Position", user.Position ?? "ผู้แทนขาย"),
                new Claim("CanViewPaymentDetails", user.CanViewPaymentDetails ? "true" : "false"),
                new Claim("CanChangeDebtStatus", (user.Role == "admin" || user.CanChangeDebtStatus) ? "true" : "false"),
                new Claim("CanDeleteSalesBill", (user.Role == "admin" || user.CanDeleteSalesBill) ? "true" : "false"),
                new Claim("CanDeleteDebtor", (user.Role == "admin" || user.CanDeleteDebtor) ? "true" : "false"),
                new Claim("SessionTimeout", (user.SessionTimeoutMinutes ?? 0).ToString()),
                new Claim("CanDownload", canDownloadFinal ? "true" : "false"),
                new Claim("CanScreenCapture", canCaptureFinal ? "true" : "false"),
                new Claim("AllowedPages", user.Role == "admin" ? "Dashboard,SalesBill,Debtor,DebtorHistory,Cancelled,WaitingGoods,SalesReport,Audit,Users,Upload,PaymentDetails" : (user.AllowedPages ?? "")),
                new Claim("AllowedRegion", user.AllowedRegion ?? ""),
                new Claim("AllowedProvinces", user.AllowedProvinces ?? ""),
                new Claim("AllowedDistricts", user.AllowedDistricts ?? "")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // กำหนดอายุ Cookie ตามสิทธิ์ผู้ใช้ (ผู้แทนขายจำกัด 10 นาที)
            var expireSpan = (user.SessionTimeoutMinutes.HasValue && user.SessionTimeoutMinutes > 0)
                ? TimeSpan.FromMinutes(user.SessionTimeoutMinutes.Value)
                : (isSalesRep ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(12));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.Add(expireSpan) });

            string resolvedArea = !string.IsNullOrEmpty(locationName) 
                ? locationName 
                : GeoLocationHelper.ReverseGeocode(lat, lng);

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Username = user.Username,
                Action = "LOGIN",
                Detail = $"User {user.FullName} ({user.Position}) logged in",
                Latitude = lat ?? "",
                Longitude = lng ?? "",
                Area = resolvedArea,
                IPAddress = GetRealIpAddress(),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            _logger.LogInformation($"User {username} logged in.");
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) 
                && !returnUrl.Contains("AccessDenied", StringComparison.OrdinalIgnoreCase) 
                && !returnUrl.Contains("Login", StringComparison.OrdinalIgnoreCase)
                && !returnUrl.Equals("/Dashboard", StringComparison.OrdinalIgnoreCase)
                && !returnUrl.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(returnUrl);
            }

            var allowedPages = user.AllowedPages?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
            if (user.Role == "admin" || allowedPages.Contains("dashboard"))
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return RedirectToAction("Index", "SalesBill");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown";
            _db.AuditLogs.Add(new AuditLog
            {
                Username = username,
                Action = "LOGOUT",
                Detail = $"User {username} logged out manually",
                IPAddress = GetRealIpAddress(),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> SecurityLogout(string? reason)
        {
            var username = User.Identity?.Name ?? "Unknown";
            _db.AuditLogs.Add(new AuditLog
            {
                Username = username,
                Action = "SECURITY_VIOLATION_LOGOUT",
                Detail = $"Forced Security Logout for {username}: {reason ?? "CAPTURE_VIOLATION"}",
                IPAddress = GetRealIpAddress(),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            string blockedReason = reason == "IDLE_TIMEOUT_5_MINS" ? "idle" : "capture";
            return RedirectToAction("Login", new { blocked = blockedReason });
        }

        [HttpPost]
        public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutRequest req)
        {
            var username = User.Identity?.Name ?? "Unknown";
            _db.AuditLogs.Add(new AuditLog
            {
                Username = username,
                Action = "SECURITY_VIOLATION_LOGOUT",
                Detail = $"System forced logout for {username}: {req?.Reason ?? "TIMEOUT"}",
                Latitude = req?.Lat ?? "",
                Longitude = req?.Lng ?? "",
                Area = GeoLocationHelper.ReverseGeocode(req?.Lat, req?.Lng),
                IPAddress = GetRealIpAddress(),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(".AspNetCore.Cookies");
            return Json(new { success = true });
        }

        [Authorize(Roles = "admin")]
        public IActionResult Users(string? search)
        {
            var q = _db.Users.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(u => u.Username.Contains(search) || u.FullName.Contains(search) || u.Position.Contains(search));
            ViewBag.Search = search;
            return View(q.OrderBy(u => u.Username).ToList());
        }

        private void LoadLocationData()
        {
            ViewBag.Regions = RegionHelper.GetRegions();
            ViewBag.Provinces = RegionHelper.DisplayProvinces;
            ViewBag.AllThailandProvinces = RegionHelper.GetAllProvinces();

            try
            {
                ViewBag.ProvinceDistricts = _cache.GetOrCreate("loc_province_districts", entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                    var customerDistricts = _db.Customers.AsNoTracking()
                        .Where(c => !string.IsNullOrEmpty(c.Province) && !string.IsNullOrEmpty(c.District))
                        .Select(c => new { Province = c.Province.Trim(), District = c.District.Trim() })
                        .Distinct()
                        .ToList();

                    var salesDistricts = _db.SalesBills.AsNoTracking()
                        .Where(b => !string.IsNullOrEmpty(b.Province) && !string.IsNullOrEmpty(b.District))
                        .Select(b => new { Province = b.Province.Trim(), District = b.District.Trim() })
                        .Distinct()
                        .ToList();

                    var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var grp in customerDistricts.Concat(salesDistricts).Distinct().GroupBy(x => x.Province))
                    {
                        dict[grp.Key] = grp.Select(x => x.District).Where(d => !string.IsNullOrEmpty(d)).Distinct().OrderBy(d => d).ToList();
                    }
                    return dict;
                }) ?? new Dictionary<string, List<string>>();

                ViewBag.DbSalesReps = _cache.GetOrCreate("loc_db_sales_reps", entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                    return _db.SalesBills.AsNoTracking()
                        .Select(b => b.SalesRep)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();
                }) ?? new List<string>();
            }
            catch
            {
                ViewBag.ProvinceDistricts = new Dictionary<string, List<string>>();
                ViewBag.DbSalesReps = new List<string>();
            }
        }

        [Authorize(Roles = "admin"), HttpGet]
        public IActionResult CreateUser()
        {
            LoadLocationData();
            return View(new AppUser());
        }

        [Authorize(Roles = "admin"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string username, string fullName, string password,
            string role, string position, string? salesRepCode, int? sessionTimeoutMinutes, 
            string? allowedRegion, string? allowedProvinces, string? allowedDistricts, 
            string[]? pages, bool canViewPaymentDetails, bool canChangeDebtStatus, bool canDeleteSalesBill, bool canDeleteDebtor, 
            bool canDownload, bool canScreenCapture)
        {
            if (_db.Users.Any(u => u.Username == username))
            {
                TempData["Error"] = "ชื่อผู้ใช้นี้ถูกใช้งานแล้ว";
                return RedirectToAction("CreateUser");
            }

            var allowedPagesStr = pages != null && pages.Length > 0 ? string.Join(",", pages) : "";

            _db.Users.Add(new AppUser
            {
                Username = username,
                FullName = fullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                Position = position ?? "ผู้แทนขาย",
                SalesRepCode = salesRepCode ?? "",
                SessionTimeoutMinutes = (sessionTimeoutMinutes == null || sessionTimeoutMinutes == 0) ? null : sessionTimeoutMinutes,
                AllowedRegion = allowedRegion ?? "",
                AllowedProvinces = allowedProvinces ?? "",
                AllowedDistricts = allowedDistricts ?? "",
                AllowedPages = allowedPagesStr,
                CanViewPaymentDetails = canViewPaymentDetails,
                CanChangeDebtStatus = canChangeDebtStatus,
                CanDeleteSalesBill = canDeleteSalesBill,
                CanDeleteDebtor = canDeleteDebtor,
                CanDownload = canDownload,
                CanScreenCapture = canScreenCapture,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "CREATE_USER",
                Detail = $"Created user: {username} ({position}) role={role} timeout={sessionTimeoutMinutes} pages={allowedPagesStr}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"สร้างบัญชีผู้ใช้ '{username}' สำเร็จ";
            return RedirectToAction("Users");
        }

        [Authorize(Roles = "admin"), HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return NotFound();
            LoadLocationData();
            return View(user);
        }

        [Authorize(Roles = "admin"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, string fullName, string role, string position,
            string? salesRepCode, int? sessionTimeoutMinutes, bool isActive, string? newPassword,
            string? allowedRegion, string? allowedProvinces, string? allowedDistricts, 
            string[]? pages, bool canViewPaymentDetails, bool canChangeDebtStatus, bool canDeleteSalesBill, bool canDeleteDebtor, 
            bool canDownload, bool canScreenCapture)
        {
            var user = _db.Users.Find(id);
            if (user == null) return NotFound();
            user.FullName = fullName;
            user.Role = role;
            user.Position = position ?? "ผู้แทนขาย";
            user.SalesRepCode = salesRepCode ?? "";
            user.SessionTimeoutMinutes = (sessionTimeoutMinutes == null || sessionTimeoutMinutes == 0) ? null : sessionTimeoutMinutes;
            user.IsActive = isActive;
            user.AllowedRegion = allowedRegion ?? "";
            user.AllowedProvinces = allowedProvinces ?? "";
            user.AllowedDistricts = allowedDistricts ?? "";
            user.AllowedPages = pages != null && pages.Length > 0 ? string.Join(",", pages) : "";
            user.CanViewPaymentDetails = canViewPaymentDetails;
            user.CanChangeDebtStatus = canChangeDebtStatus;
            user.CanDeleteSalesBill = canDeleteSalesBill;
            user.CanDeleteDebtor = canDeleteDebtor;
            user.CanDownload = canDownload;
            user.CanScreenCapture = canScreenCapture;

            if (!string.IsNullOrEmpty(newPassword))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "EDIT_USER",
                Detail = $"Edited user id={id} role={role} pos={position} pages={user.AllowedPages}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "แก้ไขข้อมูลและสิทธิ์ผู้ใช้สำเร็จ";
            return RedirectToAction("Users");
        }

        [Authorize(Roles = "admin"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return NotFound();
            if (user.Username == "admin")
            {
                TempData["Error"] = "ไม่สามารถลบบัญชี admin หลักได้";
                return RedirectToAction("Users");
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "DELETE_USER",
                Detail = $"Deleted user: {user.Username} (Role: {user.Role}, Position: {user.Position})",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"ลบบัญชีผู้ใช้ '{user.Username}' เรียบร้อยแล้ว";
            return RedirectToAction("Users");
        }

        public IActionResult AccessDenied()
        {
            var allowedPages = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
            if (User.IsInRole("admin") || allowedPages.Contains("dashboard")) return RedirectToAction("Index", "Dashboard");
            return RedirectToAction("Index", "SalesBill");
        }

        private string GetRealIpAddress()
        {
            var xff = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xff))
            {
                var ips = xff.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                    return ips[0].Trim();
            }
            var cfIp = HttpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(cfIp)) return cfIp.Trim();

            var xRealIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp)) return xRealIp.Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
        [HttpGet, AllowAnonymous]
        public IActionResult Ping() => Ok(new { ok = true, t = DateTime.Now });
    }

    public class ForceLogoutRequest
    {
        public string? Reason { get; set; }
        public string? Lat { get; set; }
        public string? Lng { get; set; }
    }
}