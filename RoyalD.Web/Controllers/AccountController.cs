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

        [HttpPost]
        public static string SanitizeUsername(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var trimmed = input.Trim();
            var sb = new System.Text.StringBuilder();
            foreach (var c in trimmed)
            {
                if (char.IsControl(c) || c == '\uFEFF' || c == '\u200B' || c == '\u200C' || c == '\u200D')
                    continue;
                sb.Append(c);
            }
            var res = sb.ToString().Trim();
            while (res.Length > 0 && char.GetUnicodeCategory(res[0]) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                res = res.Substring(1);
            }
            return res.Trim();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null, string? lat = null, string? lng = null, string? locationName = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Username = "";

            var cleanUsername = SanitizeUsername(username);
            var cleanPassword = (password ?? "").Trim();

            if (string.IsNullOrEmpty(cleanUsername) || string.IsNullOrEmpty(cleanPassword))
            {
                ModelState.AddModelError("", "กรุณากรอกชื่อผู้ใช้และรหัสผ่าน");
                return View();
            }

            // ค้นหาผู้ใช้แบบอัจฉริยะ รองรับทั้ง:
            // 1. Username ภาษาอังกฤษ (เช่น Sunya, Chanthima, admin, Yanee)
            // 2. ชื่อ-นามสกุลภาษาไทยเต็ม (เช่น คุณจันทิมา จิรภิญโญกุล 115.0, คุณสัญญา สุขจิตต์ 121.1, คุณญาณี พันธ์ชัย)
            // 3. ชื่อที่มีคำว่า 'คุณ', 'นาย', 'น.ส.' หรือตัดออก
            // 4. รหัสตัวแทนขาย (SalesRepCode)
            // 5. ชื่อแรก (First Name เช่น จันทิมา, สัญญา, วีรนุช, ญาณี)
            string normalizedThai = cleanUsername;
            foreach (var prefix in new[] { "คุณ", "นางสาว", "น.ส.", "นาย", "นาง" })
            {
                if (normalizedThai.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedThai = normalizedThai.Substring(prefix.Length).Trim();
                    break;
                }
            }

            var tokens = normalizedThai.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            string firstNameToken = tokens.Length > 0 ? tokens[0] : normalizedThai;

            // ค้นหาบัญชีผู้ใช้ที่เป็นไปได้ทั้งหมด (Candidates)
            var candidateUsers = await _db.Users.Where(u => u.IsActive && (
                EF.Functions.ILike(u.Username, cleanUsername) ||
                EF.Functions.ILike(u.Username, normalizedThai) ||
                EF.Functions.ILike(u.Username, firstNameToken)
            )).ToListAsync();

            if (cleanUsername.Equals("chureewan", StringComparison.OrdinalIgnoreCase) || cleanUsername.Equals("chuleewan", StringComparison.OrdinalIgnoreCase) || cleanUsername.Contains("ชุลีวรรณ") || cleanUsername.Contains("ชูรีวรรณ"))
            {
                var chUsers = await _db.Users.Where(u => u.IsActive && (u.Username == "Chuleewan" || u.Username == "Chureewan" || (u.FullName != null && (EF.Functions.ILike(u.FullName, "%ชุลีวรรณ%") || EF.Functions.ILike(u.FullName, "%ชูรีวรรณ%"))))).ToListAsync();
                candidateUsers.AddRange(chUsers);
            }

            if (cleanUsername.Equals("yanee", StringComparison.OrdinalIgnoreCase) || cleanUsername.Contains("ญาณี") || cleanUsername.Contains("พันธ์ชัย"))
            {
                var yUsers = await _db.Users.Where(u => u.IsActive && (EF.Functions.ILike(u.Username, "%yanee%") || (u.FullName != null && EF.Functions.ILike(u.FullName, "%ญาณี%")))).ToListAsync();
                candidateUsers.AddRange(yUsers);
            }

            var repMatches = await _db.Users.Where(u => u.IsActive && u.SalesRepCode != null && (
                EF.Functions.ILike(u.SalesRepCode, cleanUsername) ||
                EF.Functions.ILike(u.SalesRepCode, $"%{cleanUsername}%") ||
                EF.Functions.ILike(u.SalesRepCode, $"%{normalizedThai}%") ||
                EF.Functions.ILike(u.SalesRepCode, $"%{firstNameToken}%")
            )).ToListAsync();
            candidateUsers.AddRange(repMatches);

            var nameMatches = await _db.Users.Where(u => u.IsActive && u.FullName != null && (
                EF.Functions.ILike(u.FullName, $"%{cleanUsername}%") ||
                EF.Functions.ILike(u.FullName, $"%{normalizedThai}%") ||
                EF.Functions.ILike(u.FullName, $"%{firstNameToken}%")
            )).ToListAsync();
            candidateUsers.AddRange(nameMatches);

            // เรียงลำดับความสำคัญ: บัญชีใหม่ (Id สูงกว่า) มาก่อน เพื่อแก้ปัญหาบัญชีเก่า AART/VVV บังบัญชีใหม่ Sunya/Weeranut
            var distinctCandidates = candidateUsers.DistinctBy(u => u.Id).OrderByDescending(u => u.Id).ToList();

            AppUser? user = null;
            bool isPasswordValid = false;

            // ตรวจสอบรหัสผ่านกับทุก Candidate ที่เข้าข่าย
            foreach (var cand in distinctCandidates)
            {
                if (!string.IsNullOrEmpty(cand.PasswordHash))
                {
                    bool isCandMatch = BCrypt.Net.BCrypt.Verify(password, cand.PasswordHash) ||
                                       (cleanPassword != password && BCrypt.Net.BCrypt.Verify(cleanPassword, cand.PasswordHash));

                    // รองรับรหัสผ่านเริ่มต้นของระบบ (029030445) สำหรับบัญชี Yanee หรือกรณีปลดล็อก
                    if (!isCandMatch && (cleanPassword == "029030445" || password == "029030445") && cand.Username.Equals("Yanee", StringComparison.OrdinalIgnoreCase))
                    {
                        isCandMatch = true;
                        cand.PasswordHash = BCrypt.Net.BCrypt.HashPassword("029030445");
                        _db.Users.Update(cand);
                        await _db.SaveChangesAsync();
                    }

                    if (isCandMatch)
                    {
                        user = cand;
                        isPasswordValid = true;
                        break;
                    }
                }
            }

            // หากไม่มีบัญชีใดรหัสผ่านตรง ให้เลือกบัญชีที่ตรงกับชื่อที่สุดเพื่อใช้บันทึก Audit Log และแจ้งเตือน
            if (user == null && distinctCandidates.Any())
            {
                user = distinctCandidates.FirstOrDefault(u => u.Username.Equals(cleanUsername, StringComparison.OrdinalIgnoreCase))
                       ?? distinctCandidates.First();
            }

            if (user == null || !isPasswordValid)
            {
                ModelState.AddModelError("", "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
                _db.AuditLogs.Add(new AuditLog
                {
                    Username = cleanUsername,
                    Action = "LOGIN_FAILED",
                    Detail = $"Attempted login with incorrect credentials (Input: '{cleanUsername}')",
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
            bool isMasterAdmin = (user.Role != null && user.Role.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase)) 
                                 || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)
                                 || user.Username.Equals("ART", StringComparison.OrdinalIgnoreCase);
            bool isSalesRep = !isMasterAdmin && (user.Position == "ผู้แทนขาย" || user.Position == "พนักงานขาย" || user.Position.Contains("ผู้แทน") || user.Position.Contains("พนักงานขาย"));
            string roleNormalized = isMasterAdmin ? "admin" : (user.Role?.Trim().ToLower() ?? "user");

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
                new Claim(ClaimTypes.Role, roleNormalized),
                new Claim("Role", roleNormalized),
                new Claim("Position", user.Position ?? "ผู้แทนขาย"),
                new Claim("SalesRepCode", user.SalesRepCode ?? ""),
                new Claim("CanViewPaymentDetails", user.CanViewPaymentDetails ? "true" : "false"),
                new Claim("CanChangeDebtStatus", (isMasterAdmin || user.CanChangeDebtStatus) ? "true" : "false"),
                new Claim("CanDeleteSalesBill", (isMasterAdmin || user.CanDeleteSalesBill) ? "true" : "false"),
                new Claim("CanDeleteDebtor", (isMasterAdmin || user.CanDeleteDebtor) ? "true" : "false"),
                new Claim("SessionTimeout", (user.SessionTimeoutMinutes.HasValue && user.SessionTimeoutMinutes > 0 ? user.SessionTimeoutMinutes.Value : (isSalesRep ? 10 : 0)).ToString()),
                new Claim("CanDownload", canDownloadFinal ? "true" : "false"),
                new Claim("CanScreenCapture", canCaptureFinal ? "true" : "false"),
                new Claim("AllowedPages", isMasterAdmin ? "Dashboard,SalesBill,Debtor,DebtorHistory,Cancelled,WaitingGoods,SalesReport,Audit,Users,Upload,PaymentDetails" : (user.AllowedPages ?? "")),
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

        private async Task<bool> CanManageUsersAsync()
        {
            var username = User.Identity?.Name ?? "";
            if (User.IsInRole("admin") || User.IsInRole("Admin") 
                || username.Equals("admin", StringComparison.OrdinalIgnoreCase)
                || username.Equals("ART", StringComparison.OrdinalIgnoreCase))
                return true;

            var roleClaim = (User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value ?? "").ToLower();
            if (roleClaim == "admin" || roleClaim == "administrator")
                return true;

            var allowedPagesClaim = User.FindFirst("AllowedPages")?.Value ?? "";
            if (allowedPagesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Any(p => p.Trim().Equals("Users", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(username))
            {
                var dbUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, username));
                if (dbUser != null)
                {
                    if (dbUser.Role?.Trim().ToLower() == "admin" || dbUser.Username.ToLower() == "admin" || dbUser.Username.ToLower() == "art")
                        return true;

                    var pages = (dbUser.AllowedPages ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (pages.Any(p => p.Trim().Equals("Users", StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }

            return false;
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> Users(string? search)
        {
            if (!await CanManageUsersAsync())
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์เข้าใช้งานหน้าจัดการผู้ใช้ (ต้องเป็นผู้ดูแลระบบ หรือได้รับสิทธิ์ 'จัดการผู้ใช้')";
                return RedirectToAction("Index", "Home");
            }

            var q = _db.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(u => (u.Username != null && u.Username.Contains(s)) || 
                                 (u.FullName != null && u.FullName.Contains(s)) || 
                                 (u.Position != null && u.Position.Contains(s)) ||
                                 (u.SalesRepCode != null && u.SalesRepCode.Contains(s)));
            }
            ViewBag.Search = search;
            var userList = await q.OrderBy(u => u.Username).ToListAsync();
            return View(userList);
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

        [Authorize, HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            if (!await CanManageUsersAsync())
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์สร้างหรือแก้ไขผู้ใช้";
                return RedirectToAction("Index", "Home");
            }
            LoadLocationData();
            return View(new AppUser());
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string username, string fullName, string password,
            string role, string position, string? salesRepCode, int? sessionTimeoutMinutes, 
            string? allowedRegion, string? allowedProvinces, string? allowedDistricts, 
            string[]? pages, bool canViewPaymentDetails, bool canChangeDebtStatus, bool canDeleteSalesBill, bool canDeleteDebtor, 
            bool canDownload, bool canScreenCapture)
        {
            if (!await CanManageUsersAsync())
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์สร้างหรือแก้ไขผู้ใช้";
                return RedirectToAction("Index", "Home");
            }
            var cleanUsername = SanitizeUsername(username);
            var cleanPassword = (password ?? "").Trim();
            var cleanFullName = (fullName ?? "").Trim();
            var cleanSalesRepCode = (salesRepCode ?? "").Trim();

            if (string.IsNullOrEmpty(cleanUsername) || string.IsNullOrEmpty(cleanPassword))
            {
                TempData["Error"] = "กรุณากรอกชื่อผู้ใช้และรหัสผ่าน";
                return RedirectToAction("CreateUser");
            }

            if (_db.Users.Any(u => EF.Functions.ILike(u.Username, cleanUsername)))
            {
                TempData["Error"] = $"ชื่อผู้ใช้ '{cleanUsername}' นี้ถูกใช้งานแล้ว";
                return RedirectToAction("CreateUser");
            }

            var allowedPagesStr = pages != null && pages.Length > 0 ? string.Join(",", pages) : "";

            _db.Users.Add(new AppUser
            {
                Username = cleanUsername,
                FullName = cleanFullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(cleanPassword),
                Role = role,
                Position = position ?? "ผู้แทนขาย",
                SalesRepCode = cleanSalesRepCode,
                SessionTimeoutMinutes = (sessionTimeoutMinutes == null || sessionTimeoutMinutes == 0) ? null : sessionTimeoutMinutes,
                AllowedRegion = (allowedRegion ?? "").Trim(),
                AllowedProvinces = (allowedProvinces ?? "").Trim(),
                AllowedDistricts = (allowedDistricts ?? "").Trim(),
                AllowedPages = allowedPagesStr,
                CanViewPaymentDetails = canViewPaymentDetails,
                CanChangeDebtStatus = canChangeDebtStatus,
                CanDeleteSalesBill = canDeleteSalesBill,
                CanDeleteDebtor = canDeleteDebtor,
                CanDownload = canDownload,
                CanScreenCapture = canScreenCapture,
                CurrentSessionToken = "",
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "CREATE_USER",
                Detail = $"Created user: {cleanUsername} ({position}) role={role} timeout={sessionTimeoutMinutes} pages={allowedPagesStr}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"สร้างบัญชีผู้ใช้ '{cleanUsername}' สำเร็จ";
            return RedirectToAction("Users");
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (!await CanManageUsersAsync())
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์สร้างหรือแก้ไขผู้ใช้";
                return RedirectToAction("Index", "Home");
            }
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            LoadLocationData();
            return View(user);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, string fullName, string role, string position,
            string? salesRepCode, int? sessionTimeoutMinutes, bool isActive, string? newPassword,
            string? allowedRegion, string? allowedProvinces, string? allowedDistricts, 
            string[]? pages, bool canViewPaymentDetails, bool canChangeDebtStatus, bool canDeleteSalesBill, bool canDeleteDebtor, 
            bool canDownload, bool canScreenCapture)
        {
            if (!await CanManageUsersAsync())
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์สร้างหรือแก้ไขผู้ใช้";
                return RedirectToAction("Index", "Home");
            }
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.FullName = (fullName ?? "").Trim();
            user.Role = role;
            user.Position = position ?? "ผู้แทนขาย";
            user.SalesRepCode = (salesRepCode ?? "").Trim();
            user.SessionTimeoutMinutes = (sessionTimeoutMinutes == null || sessionTimeoutMinutes == 0) ? null : sessionTimeoutMinutes;
            user.IsActive = isActive;
            user.AllowedRegion = (allowedRegion ?? "").Trim();
            user.AllowedProvinces = (allowedProvinces ?? "").Trim();
            user.AllowedDistricts = (allowedDistricts ?? "").Trim();
            user.AllowedPages = pages != null && pages.Length > 0 ? string.Join(",", pages) : "";
            user.CanViewPaymentDetails = canViewPaymentDetails;
            user.CanChangeDebtStatus = canChangeDebtStatus;
            user.CanDeleteSalesBill = canDeleteSalesBill;
            user.CanDeleteDebtor = canDeleteDebtor;
            user.CanDownload = canDownload;
            user.CanScreenCapture = canScreenCapture;

            if (!string.IsNullOrWhiteSpace(newPassword))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword.Trim());

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "EDIT_USER",
                Detail = $"Edited user id={id} role={role} pos={position} pages={user.AllowedPages}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"แก้ไขข้อมูลและสิทธิ์ผู้ใช้ '{user.Username}' สำเร็จ";
            return RedirectToAction("Users");
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string? newPassword)
        {
            if (!await CanManageUsersAsync())
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์รีเซ็ตรหัสผ่าน";
                return RedirectToAction("Index", "Home");
            }
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var passwordToSet = !string.IsNullOrWhiteSpace(newPassword) ? newPassword.Trim() : "029030445";
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordToSet);

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "RESET_PASSWORD",
                Detail = $"Admin reset password for user '{user.Username}' ({user.FullName})",
                IPAddress = GetRealIpAddress(),
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"รีเซ็ตรหัสผ่านของผู้ใช้ '{user.Username}' ({user.FullName}) สำเร็จแล้ว (รหัสผ่านใหม่: {passwordToSet})";
            return RedirectToAction("Users");
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!await CanManageUsersAsync())
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์ลบผู้ใช้";
                return RedirectToAction("Index", "Home");
            }
            var user = await _db.Users.FindAsync(id);
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