using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class DebtorController : Controller
    {
        private readonly AppDbContext _db;
        private readonly DebtorService _svc;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        public DebtorController(AppDbContext db, DebtorService svc, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, IMemoryCache cache)
        {
            _db = db;
            _svc = svc;
            _env = env;
            _cache = cache;
        }

        public async Task<IActionResult> Index(
            string? search, 
            string? salesRep, 
            string? status = "outstanding", 
            string? poSearch = null, 
            string? region = null, 
            string? province = null, 
            string? district = null,
            string? credit = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode))
            {
                salesRep = currentUser.SalesRepCode;
            }
            if (isRestricted && !string.IsNullOrEmpty(userAllowedRegion))
            {
                region = userAllowedRegion;
            }

            var debts = await _svc.GetDebtorsAsync(
                search: search,
                region: region,
                province: province,
                salesRep: salesRep,
                status: null, // we will filter status locally
                credit: credit,
                userAllowedRegion: userAllowedRegion,
                userAllowedProvinces: userAllowedProvinces,
                userAllowedDistricts: userAllowedDistricts
            );

            var today = DateTime.Today;
            debts = debts.Where(d => {
                bool isInstallment = d.Status == DebtStatus.Installment || (int)d.Status == 100;
                if (isInstallment) return true;
                bool isPaid = d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue || 
                              d.Status == DebtStatus.PaidTransfer || 
                              d.Status == DebtStatus.PaidCash || 
                              d.Status == DebtStatus.PaidCheck;
                if (!isPaid) return true;
                
                var dateToCheck = d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate ?? d.BillDate;
                return (today - dateToCheck.Date).TotalDays <= 7;
            }).ToList();
            
            if (status == "outstanding")
            {
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding || d.Status == DebtStatus.Installment || (int)d.Status == 100).ToList();
            }
            else if (status == "paid")
            {
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && d.Status != DebtStatus.Installment && (int)d.Status != 100 && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            }
            else if (status == "overdue120")
            {
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).ToList();
            }
            else if (!string.IsNullOrEmpty(status) && (status.Equals("Installment", StringComparison.OrdinalIgnoreCase) || status == "100"))
            {
                debts = debts.Where(d => d.Status == DebtStatus.Installment || (int)d.Status == 100).ToList();
            }
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<DebtStatus>(status, out var parsedStatus))
            {
                debts = debts.Where(d => d.Status == parsedStatus).ToList();
            }
            
            // กรองบิลที่ชำระครบแล้วและเลย 1 วัน (ย้ายไปประวัติ) ยกเว้นบิลผ่อนชำระ
            debts = debts.Where(d => 
            {
                bool isInstallment = d.Status == DebtStatus.Installment || (int)d.Status == 100;
                if (isInstallment) return true;
                if (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue || 
                    d.Status == DebtStatus.PaidTransfer || d.Status == DebtStatus.PaidCash || d.Status == DebtStatus.PaidCheck)
                {
                    var dateToCheck = d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate;
                    if (dateToCheck.HasValue && dateToCheck.Value.Date < DateTime.Today)
                    {
                        return false; // กรองออก เพราะเก่ากว่า 1 วัน
                    }
                }
                return true;
            }).ToList();

            if (!string.IsNullOrEmpty(district))
            {
                debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && d.District.Contains(district)).ToList();
            }

            if (startDate.HasValue)
            {
                debts = debts.Where(d => d.BillDate >= startDate.Value.Date).ToList();
            }

            if (endDate.HasValue)
            {
                debts = debts.Where(d => d.BillDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            }

            if (!string.IsNullOrEmpty(poSearch))
            {
                debts = debts.Where(d => !string.IsNullOrEmpty(d.PoNumber) && d.PoNumber.Contains(poSearch)).ToList();
            }

            var rawCredits = await _cache.GetOrCreateAsync("all_debtor_credits", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.Credit).Distinct().OrderBy(c => c).ToListAsync();
            }) ?? new List<int>();

            var creditOptions = new List<string>();
            if (rawCredits.Any(c => c == 0 || c == 7))
                creditOptions.Add("0_7"); // เน€เธเธดเธเธชเธ” / 7 เธงเธฑเธ
            foreach (var c in rawCredits.Where(c => c != 0 && c != 7))
            {
                creditOptions.Add(c.ToString());
            }

            ViewBag.SearchTerm = search;
            ViewBag.PoSearch = poSearch;
            ViewBag.SelectedSalesRep = salesRep;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedRegion = isRestricted && !string.IsNullOrEmpty(userAllowedRegion) ? userAllowedRegion : region;
            ViewBag.SelectedProvince = province;
            ViewBag.SelectedDistrict = district;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCredit = credit;
            ViewBag.IsRestricted = isRestricted;
            ViewBag.AssignedSalesRep = currentUser?.SalesRepCode;
            ViewBag.IsLockedRegion = !string.IsNullOrEmpty(userAllowedRegion);
            ViewBag.IsLockedProvince = !string.IsNullOrEmpty(userAllowedProvinces);
            ViewBag.IsLockedDistrict = !string.IsNullOrEmpty(userAllowedDistricts);
            ViewBag.Regions = !string.IsNullOrEmpty(userAllowedRegion) ? new List<string> { userAllowedRegion } : RegionHelper.GetRegions();
            
            if (!string.IsNullOrEmpty(userAllowedProvinces))
            {
                ViewBag.Provinces = userAllowedProvinces.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
            }
            else if (!string.IsNullOrEmpty(userAllowedRegion))
            {
                ViewBag.Provinces = RegionHelper.GetDisplayProvinces(userAllowedRegion);
            }
            else
            {
                ViewBag.Provinces = RegionHelper.GetDisplayProvinces(region);
            }

            ViewBag.AllProvincesMap = RegionHelper.DisplayProvinces;

            var rawDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var allDbReps = rawDbReps
                .Where(s => !string.IsNullOrWhiteSpace(s) &&
                            !s.Contains("5%") &&
                            !s.Contains("page", StringComparison.OrdinalIgnoreCase) &&
                            !s.Contains("เธซเธเนเธฒ", StringComparison.OrdinalIgnoreCase) &&
                            !s.All(char.IsDigit))
                .OrderBy(s => s)
                .ToList();

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode))
            {
                var userRepInputs = currentUser.SalesRepCode.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                var matchedForUser = allDbReps.Where(dbRep => 
                    userRepInputs.Any(u => 
                        dbRep.IndexOf(u, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        u.IndexOf(dbRep, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        dbRep.Replace(" ", "").IndexOf(u.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) >= 0 ||
                        u.Replace(" ", "").IndexOf(dbRep.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) >= 0
                    )
                ).ToList();
                ViewBag.SalesReps = matchedForUser.Count > 0 ? matchedForUser : userRepInputs;
                ViewBag.IsLockedSalesRep = true;
            }
            else
            {
                ViewBag.SalesReps = allDbReps;
                ViewBag.IsLockedSalesRep = false;
            }

            string pos = (currentUser?.Position ?? "").Trim();
            bool isSalesRepPos = pos == "เธเธนเนเนเธ—เธเธเธฒเธข" || pos == "เธเธเธฑเธเธเธฒเธเธเธฒเธข" || pos.Contains("เธเธนเนเนเธ—เธ") || pos.Contains("เธเธเธฑเธเธเธฒเธเธเธฒเธข");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);

            ViewBag.CreditOptions = creditOptions;
            ViewBag.TotalAmount = debts.Sum(d => d.RemainingAmount);
            ViewBag.OverdueCount = debts.Count(d => d.DueDate < DateTime.Today && d.Status == DebtStatus.Outstanding);
            ViewBag.CanChangeDebtStatus = currentUser != null && (currentUser.Role == "admin" || currentUser.CanChangeDebtStatus);
            ViewBag.CanDeleteDebtor = currentUser != null && (currentUser.Role == "admin" || currentUser.CanDeleteDebtor);
            ViewBag.CanDownload = canDownload;

            return View(debts);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(
            string? search, 
            string? salesRep, 
            string? status = "outstanding", 
            string? poSearch = null, 
            string? region = null, 
            string? province = null, 
            string? district = null, 
            string? credit = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            string pos = (currentUser?.Position ?? "").Trim();
            bool isSalesRepPos = pos == "เธเธนเนเนเธ—เธเธเธฒเธข" || pos == "เธเธเธฑเธเธเธฒเธเธเธฒเธข" || pos.Contains("เธเธนเนเนเธ—เธ") || pos.Contains("เธเธเธฑเธเธเธฒเธเธเธฒเธข");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);
            if (!canDownload) return Forbid();

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode)) salesRep = currentUser.SalesRepCode;
            if (isRestricted && !string.IsNullOrEmpty(userAllowedRegion)) region = userAllowedRegion;

            var debts = await _svc.GetDebtorsAsync(
                search: search,
                region: region,
                province: province,
                salesRep: salesRep,
                status: null,
                credit: credit,
                userAllowedRegion: userAllowedRegion,
                userAllowedProvinces: userAllowedProvinces,
                userAllowedDistricts: userAllowedDistricts
            );

            var today = DateTime.Today;
            debts = debts.Where(d => {
                bool isPaid = d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue || 
                              d.Status == DebtStatus.PaidTransfer || 
                              d.Status == DebtStatus.PaidCash || 
                              d.Status == DebtStatus.PaidCheck;
                if (!isPaid) return true;
                
                var dateToCheck = d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate ?? d.BillDate;
                return (today - dateToCheck.Date).TotalDays <= 7;
            }).ToList();

            if (status == "outstanding")
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding).ToList();
            else if (status == "paid")
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            else if (status == "overdue120")
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).ToList();
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<DebtStatus>(status, out var parsedStatus))
                debts = debts.Where(d => d.Status == parsedStatus).ToList();

            if (!string.IsNullOrEmpty(district))
                debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && d.District.Contains(district)).ToList();
            if (startDate.HasValue)
                debts = debts.Where(d => d.BillDate >= startDate.Value.Date).ToList();
            if (endDate.HasValue)
                debts = debts.Where(d => d.BillDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            if (!string.IsNullOrEmpty(poSearch))
                debts = debts.Where(d => !string.IsNullOrEmpty(d.PoNumber) && d.PoNumber.Contains(poSearch)).ToList();

            var bytes = await _svc.ExportDebtorsExcelAsync(debts);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Debtors_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPaidExcel(
            string? search, 
            string? salesRep, 
            string? poSearch = null, 
            string? region = null, 
            string? province = null, 
            string? district = null, 
            string? credit = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return await ExportExcel(search, salesRep, "paid", poSearch, region, province, district, credit, startDate, endDate);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            string? search, 
            string? salesRep, 
            string? status = "outstanding", 
            string? poSearch = null, 
            string? region = null, 
            string? province = null, 
            string? district = null, 
            string? credit = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            string pos = (currentUser?.Position ?? "").Trim();
            bool isSalesRepPos = pos == "เธเธนเนเนเธ—เธเธเธฒเธข" || pos == "เธเธเธฑเธเธเธฒเธเธเธฒเธข" || pos.Contains("เธเธนเนเนเธ—เธ") || pos.Contains("เธเธเธฑเธเธเธฒเธเธเธฒเธข");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);
            if (!canDownload) return Forbid();

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode)) salesRep = currentUser.SalesRepCode;
            if (isRestricted && !string.IsNullOrEmpty(userAllowedRegion)) region = userAllowedRegion;

            var debts = await _svc.GetDebtorsAsync(search, region, province, salesRep, null, credit, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);

            var today = DateTime.Today;
            if (status == "outstanding")
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding).ToList();
            else if (status == "paid")
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            else if (status == "overdue120")
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).ToList();

            if (!string.IsNullOrEmpty(district)) debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && d.District.Contains(district)).ToList();
            if (startDate.HasValue) debts = debts.Where(d => d.BillDate >= startDate.Value.Date).ToList();
            if (endDate.HasValue) debts = debts.Where(d => d.BillDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            if (!string.IsNullOrEmpty(poSearch)) debts = debts.Where(d => !string.IsNullOrEmpty(d.PoNumber) && d.PoNumber.Contains(poSearch)).ToList();

            var csv = new System.Text.StringBuilder();
            csv.Append('\uFEFF');
            csv.AppendLine("เน€เธฅเธเธ—เธตเนเธเธดเธฅ,เธงเธฑเธเธ—เธตเนเธเธดเธฅ,เธเธฃเธเธเธณเธซเธเธ”,เธฃเธซเธฑเธชเธฅเธนเธเธเนเธฒ,เธเธทเนเธญเธฅเธนเธเธเนเธฒ,เธญเธณเน€เธ เธญ,เธเธฑเธเธซเธงเธฑเธ”,เธเธนเนเนเธ—เธเธเธฒเธข,เธขเธญเธ”เน€เธ”เธดเธก,เธขเธญเธ”เธเธเธเนเธฒเธ,เธชเธ–เธฒเธเธฐ,เน€เธฅเธเธ—เธตเนเนเธเน€เธชเธฃเนเธ,เธงเธฑเธเธ—เธตเนเธฃเธฑเธเน€เธเธดเธ");

            foreach (var d in debts)
            {
                csv.AppendLine($"\"{d.BillNo}\",\"{d.BillDate:dd/MM/yyyy}\",\"{d.DueDate:dd/MM/yyyy}\",\"{d.CustomerCode}\",\"{d.CustomerName?.Replace("\"", "\"\"")}\",\"{d.District}\",\"{d.Province}\",\"{d.SalesRep}\",\"{d.OriginalAmount:F2}\",\"{d.RemainingAmount:F2}\",\"{d.Status}\",\"{d.ReceiptNo}\",\"{d.ReceiptDate:dd/MM/yyyy}\"");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"Debtors_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(
            string? search, 
            string? salesRep, 
            string? status = "outstanding", 
            string? poSearch = null, 
            string? region = null, 
            string? province = null, 
            string? district = null, 
            string? credit = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            string pos = (currentUser?.Position ?? "").Trim();
            bool isSalesRepPos = pos == "เธเธนเนเนเธ—เธเธเธฒเธข" || pos == "เธเธเธฑเธเธเธฒเธเธเธฒเธข" || pos.Contains("เธเธนเนเนเธ—เธ") || pos.Contains("เธเธเธฑเธเธเธฒเธเธเธฒเธข");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);
            if (!canDownload)
            {
                return Forbid();
            }

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode))
            {
                salesRep = currentUser.SalesRepCode;
            }
            if (isRestricted && !string.IsNullOrEmpty(userAllowedRegion))
            {
                region = userAllowedRegion;
            }

            var debts = await _svc.GetDebtorsAsync(
                search: search,
                region: region,
                province: province,
                salesRep: salesRep,
                status: null, // we will filter status locally
                credit: credit,
                userAllowedRegion: userAllowedRegion,
                userAllowedProvinces: userAllowedProvinces,
                userAllowedDistricts: userAllowedDistricts
            );

            var today = DateTime.Today;
            debts = debts.Where(d => {
                bool isPaid = d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue || 
                              d.Status == DebtStatus.PaidTransfer || 
                              d.Status == DebtStatus.PaidCash || 
                              d.Status == DebtStatus.PaidCheck;
                if (!isPaid) return true;
                
                var dateToCheck = d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate ?? d.BillDate;
                return (today - dateToCheck.Date).TotalDays <= 7;
            }).ToList();

            if (status == "outstanding")
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding).ToList();
            else if (status == "paid")
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            else if (status == "overdue120")
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).ToList();
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<DebtStatus>(status, out var parsedStatus))
                debts = debts.Where(d => d.Status == parsedStatus).ToList();

            if (!string.IsNullOrEmpty(district))
                debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && d.District.Contains(district)).ToList();
            if (startDate.HasValue)
                debts = debts.Where(d => d.BillDate >= startDate.Value.Date).ToList();
            if (endDate.HasValue)
                debts = debts.Where(d => d.BillDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            if (!string.IsNullOrEmpty(poSearch))
                debts = debts.Where(d => !string.IsNullOrEmpty(d.PoNumber) && d.PoNumber.Contains(poSearch)).ToList();

            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.Status = status;
            ViewBag.PrintedBy = currentUser?.FullName ?? User.Identity?.Name ?? "Admin";
            return View("PrintPdf", debts);
        }

        [HttpGet]
        [Route("Debtor/Detail/{*id}")]
        public async Task<IActionResult> Detail(string? id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");
            id = Uri.UnescapeDataString(id).Trim();
            int.TryParse(id, out int intId);
            var debt = await _db.OutstandingDebts
                .Include(d => d.PaymentRecords)
                .ThenInclude(p => p.Attachments)
                .Include(d => d.Customer)
                .Include(d => d.PendingProducts)
                .FirstOrDefaultAsync(d => d.BillNo == id || (intId > 0 && d.Id == intId));

            if (debt == null) return NotFound();

            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            ViewBag.CanViewPaymentDetails = currentUser != null && (currentUser.Role == "admin" || currentUser.CanViewPaymentDetails);
            ViewBag.CanChangeDebtStatus = currentUser != null && (currentUser.Role == "admin" || currentUser.CanChangeDebtStatus);
            ViewBag.CanDeleteDebtor = currentUser != null && (currentUser.Role == "admin" || currentUser.CanDeleteDebtor);
            ViewBag.CanScreenCapture = currentUser != null && (currentUser.Role == "admin" || currentUser.CanScreenCapture);

            var items = await _db.SalesBillItems.AsNoTracking().Where(i => i.BillNo == debt.BillNo).ToListAsync();
            ViewBag.Items = items;

            return View(debt);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDebtor(string id)
        {
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool canDelete = currentUser != null && (currentUser.Role == "admin" || currentUser.CanDeleteDebtor);
            if (!canDelete)
            {
                TempData["Error"] = "เธเธธเธ“เนเธกเนเธกเธตเธชเธดเธ—เธเธดเนเนเธเธเธฒเธฃเธฅเธเธเธฒเธฃเนเธ”เธฅเธนเธเธซเธเธตเน";
                return RedirectToAction("Index");
            }

            var debt = await _db.OutstandingDebts.Include(d => d.PaymentRecords).FirstOrDefaultAsync(d => d.BillNo == id);
            if (debt == null) return NotFound();

            var billNo = debt.BillNo;
            var customerName = debt.CustomerName;
            var amount = debt.RemainingAmount;

            _db.OutstandingDebts.Remove(debt);

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "DELETE_DEBTOR_CARD",
                Detail = $"Deleted Debtor Card {billNo} (Customer: {customerName}, Amount: {amount:N2})",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            _cache.Remove("all_debtor_reps");
            _cache.Remove("all_debtor_credits");
            TempData["Success"] = $"เธฅเธเธเธฒเธฃเนเธ”เธฅเธนเธเธซเธเธตเนเน€เธฅเธเธ—เธตเนเธเธดเธฅ {billNo} เน€เธฃเธตเธขเธเธฃเนเธญเธขเนเธฅเนเธง";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> History(string? search, string? salesRep, DateTime? fromDate = null, DateTime? toDate = null, string? dateType = null)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode))
            {
                salesRep = currentUser.SalesRepCode;
            }

            var debts = await _svc.GetPaidHistoryAsync(search, salesRep, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);
            if (fromDate.HasValue) { if (dateType == "bill") debts = debts.Where(d => d.BillDate >= fromDate.Value.Date).ToList(); else debts = debts.Where(d => (d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate) >= fromDate.Value.Date).ToList(); }
            if (toDate.HasValue) { if (dateType == "bill") debts = debts.Where(d => d.BillDate <= toDate.Value.Date.AddDays(1).AddTicks(-1)).ToList(); else debts = debts.Where(d => (d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate) <= toDate.Value.Date.AddDays(1).AddTicks(-1)).ToList(); }
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd"); ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd"); ViewBag.DateType = dateType;
            
            // Backfill missing or invalid Customer Code/Name for the same ReceiptNo
            var receiptGroups = debts.Where(d => !string.IsNullOrEmpty(d.ReceiptNo)).GroupBy(d => d.ReceiptNo);
            foreach (var g in receiptGroups)
            {
                var validCode = g.FirstOrDefault(d => !string.IsNullOrEmpty(d.CustomerCode) && !d.CustomerCode.Contains("/"))?.CustomerCode;
                var validName = g.FirstOrDefault(d => !string.IsNullOrEmpty(d.CustomerName) && !d.CustomerName.Contains("/") && !d.CustomerName.StartsWith("RD", StringComparison.OrdinalIgnoreCase))?.CustomerName;
                
                // If still missing, fallback to SalesBills
                if (string.IsNullOrEmpty(validName))
                {
                    var firstBill = g.FirstOrDefault()?.BillNo;
                    if (!string.IsNullOrEmpty(firstBill))
                    {
                        var realBill = await _db.SalesBills.FirstOrDefaultAsync(b => b.BillNo == firstBill);
                        if (realBill != null)
                        {
                            validCode = realBill.CustomerCode;
                            validName = realBill.CustomerName;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(validCode) || !string.IsNullOrEmpty(validName))
                {
                    foreach (var d in g)
                    {
                        if (string.IsNullOrEmpty(d.CustomerCode) || d.CustomerCode.Contains("/")) d.CustomerCode = validCode ?? "";
                        if (string.IsNullOrEmpty(d.CustomerName) || d.CustomerName.Contains("/") || d.CustomerName.StartsWith("RD", StringComparison.OrdinalIgnoreCase)) d.CustomerName = validName ?? "";
                    }
                }
            }

            var todayHistory = DateTime.Today;
            debts = debts.Where(d => {
                var dateToCheck = d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate ?? d.BillDate;
                double days = (todayHistory - dateToCheck.Date).TotalDays;
                return days > 7 && days <= 120;
            }).ToList();
            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.SalesReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();
            ViewBag.IsRestricted = isRestricted;

            return View(debts);
        }

        public async Task<IActionResult> Cancelled(string? search, string? salesRep)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode))
            {
                salesRep = currentUser.SalesRepCode;
            }

            var debts = await _svc.GetCancelledDebtsAsync(search, salesRep, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);
            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.SalesReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();
            ViewBag.IsRestricted = isRestricted;

            return View(debts);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(
            string billNo, 
            decimal amount, 
            PaymentMethod method, 
            DateTime? payDate, 
            string? bank, 
            string? checkNo, 
            DateTime? checkDate, 
            string? note, 
            IFormFile? file)
        {
            var debt = await _db.OutstandingDebts.Include(d => d.PaymentRecords).FirstOrDefaultAsync(d => d.BillNo == billNo);
            if (debt == null) return NotFound();

            if (amount <= 0 || amount > debt.RemainingAmount)
            {
                TempData["Error"] = "เธขเธญเธ”เธเธณเธฃเธฐเนเธกเนเธ–เธนเธเธ•เนเธญเธ (เธ•เนเธญเธเธกเธฒเธเธเธงเนเธฒ 0 เนเธฅเธฐเนเธกเนเน€เธเธดเธเธขเธญเธ”เธเธเธเนเธฒเธ)";
                return RedirectToAction("Detail", new { id = billNo });
            }

            var actualPayDate = payDate ?? DateTime.Now;

            var rec = new PaymentRecord
            {
                OutstandingDebtId = debt.Id,
                PaidDate = actualPayDate,
                PaidAmount = amount,
                Method = method,
                BankName = bank ?? "",
                CheckNumber = checkNo ?? "",
                CheckDate = checkDate,
                Note = note ?? "",
                CreatedBy = User.Identity?.Name ?? "system"
            };

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                rec.Attachments.Add(new FileAttachment
                {
                    FileName = file.FileName,
                    FilePath = "/uploads/" + uniqueFileName,
                    UploadedBy = User.Identity?.Name ?? "system"
                });
            }

            _db.PaymentRecords.Add(rec);

            debt.RemainingAmount -= amount;
            if (debt.RemainingAmount <= 0)
            {
                debt.RemainingAmount = 0;
                debt.FullyPaidDate = actualPayDate;
                if (method == PaymentMethod.Cash) debt.Status = DebtStatus.PaidCash;
                else if (method == PaymentMethod.Transfer) debt.Status = DebtStatus.PaidTransfer;
                else if (method == PaymentMethod.Check) debt.Status = DebtStatus.PaidCheck;
                else debt.Status = DebtStatus.PaidCash;
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "RECORD_PAYMENT",
                Detail = $"Recorded payment of {amount:N2} ({method}) for Bill {billNo}. Remaining: {debt.RemainingAmount:N2}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"เธเธฑเธเธ—เธถเธเธเธฒเธฃเธฃเธฑเธเธเธณเธฃเธฐเน€เธเธดเธ {amount:N2} เธเธฒเธ— เธชเธณเน€เธฃเนเธเนเธฅเนเธง";
            return RedirectToAction("Detail", new { id = billNo });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            string billNo, 
            DebtStatus status, 
            DateTime? postponedDate, 
            DateTime? deliveringDate, 
            DateTime? waitingGoodsDate, 
            List<string>? waitingProductCodes,
            List<string>? allProductCodes,
            List<string>? allProductNames,
            decimal? badDebtAmount, 
            DateTime? badDebtDate, 
            decimal? returnAmount, 
            bool? isReturnCutFromBill, 
            string? returnType, 
            string? note, 
            IFormFile? file)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool canChangeStatus = currentUser != null && (currentUser.Role == "admin" || currentUser.CanChangeDebtStatus);
            if (!canChangeStatus)
            {
                TempData["Error"] = "เธเธธเธ“เนเธกเนเธกเธตเธชเธดเธ—เธเธดเนเนเธเธเธฒเธฃเน€เธเธฅเธตเนเธขเธเธชเธ–เธฒเธเธฐเธซเธเธตเน";
                return RedirectToAction("Detail", new { id = billNo });
            }

            var debt = await _db.OutstandingDebts.FirstOrDefaultAsync(d => d.BillNo == billNo);
            if (debt == null) return NotFound();

            var oldStatus = debt.Status;
            if (status == DebtStatus.Installment || (int)status == 100)
            {
                status = DebtStatus.Installment;
                debt.Status = DebtStatus.Installment;
                var payments = await _db.PaymentRecords.Where(p => p.OutstandingDebtId == debt.Id).ToListAsync();
                decimal paid = payments.Sum(p => p.PaidAmount);
                if (debt.RemainingAmount <= 0)
                {
                    debt.RemainingAmount = (debt.OriginalAmount > paid) ? (debt.OriginalAmount - paid) : debt.OriginalAmount;
                }
                debt.FullyPaidDate = null;
                var bill = await _db.SalesBills.FirstOrDefaultAsync(b => b.BillNo == billNo);
                if (bill != null) bill.IsFullyPaid = false;
            }
            else
            {
                debt.Status = status;
            }
            debt.Note = note ?? "";

            if (status == DebtStatus.Postponed)
            {
                debt.PostponedDate = postponedDate;
            }
            else if (status == DebtStatus.Delivering)
            {
                debt.DeliveringDate = deliveringDate;
            }
            else if (status == DebtStatus.WaitingGoods)
            {
                debt.WaitingGoodsDate = waitingGoodsDate ?? DateTime.Today;

                // ลบรายการสินค้าค้างส่งเดิมของบิลนี้ออกก่อนบันทึกใหม่
                var oldPending = await _db.PendingProducts.Where(p => p.OutstandingDebtId == debt.Id || p.BillNo == debt.BillNo).ToListAsync();
                if (oldPending.Any())
                {
                    _db.PendingProducts.RemoveRange(oldPending);
                }

                // บันทึกรายการสินค้าค้างส่งที่เลือกจากบิล
                var savedPendingList = new List<string>();
                if (waitingProductCodes != null && waitingProductCodes.Any())
                {
                    var billItems = await _db.SalesBillItems.AsNoTracking().Where(i => i.BillNo == debt.BillNo).ToListAsync();
                    foreach (var code in waitingProductCodes)
                    {
                        var matchedItem = billItems.FirstOrDefault(i => i.ProductCode == code);
                        string prodName = matchedItem?.ProductName ?? "";
                        if (string.IsNullOrEmpty(prodName) && allProductCodes != null && allProductNames != null)
                        {
                            int codeIdx = allProductCodes.IndexOf(code);
                            if (codeIdx >= 0 && codeIdx < allProductNames.Count)
                            {
                                prodName = allProductNames[codeIdx];
                            }
                        }

                        int qty = 1;
                        if (Request.Form.TryGetValue($"waitingQuantities_{code}", out var qtyVal) && int.TryParse(qtyVal, out int parsedQty) && parsedQty > 0)
                        {
                            qty = parsedQty;
                        }
                        else if (matchedItem != null && matchedItem.Qty > 0)
                        {
                            qty = (int)matchedItem.Qty;
                        }

                        _db.PendingProducts.Add(new PendingProduct
                        {
                            OutstandingDebtId = debt.Id,
                            BillNo = debt.BillNo,
                            ProductCode = code,
                            ProductName = prodName,
                            Quantity = qty,
                            CreatedAt = DateTime.Now
                        });

                        savedPendingList.Add($"{code} ({qty.ToString("N0")})");
                    }
                }

                // สร้างข้อความสรุปสินค้าค้างส่งลงใน Note
                string pendingSummary = savedPendingList.Count > 0 ? $"เธฃเธญเธชเธดเธเธเนเธฒ: {string.Join(", ", savedPendingList)}" : "";
                if (!string.IsNullOrEmpty(note))
                {
                    debt.Note = string.IsNullOrEmpty(pendingSummary) ? note : $"{pendingSummary} | {note}";
                }
                else if (!string.IsNullOrEmpty(pendingSummary))
                {
                    debt.Note = pendingSummary;
                }
            }
            else if (status == DebtStatus.BadDebt)
            {
                debt.BadDebtAmount = badDebtAmount;
                debt.BadDebtDate = badDebtDate ?? DateTime.Today;
                if (badDebtAmount.HasValue && badDebtAmount.Value > 0)
                {
                    debt.RemainingAmount = Math.Max(0, debt.RemainingAmount - badDebtAmount.Value);
                }
            }
            else if (status == DebtStatus.ReturnPending || status == DebtStatus.ReturnIssued)
            {
                debt.ReturnAmount = returnAmount;
                debt.IsReturnCutFromBill = isReturnCutFromBill ?? false;
                if (status == DebtStatus.ReturnIssued && debt.IsReturnCutFromBill == true && returnAmount.HasValue && returnAmount.Value > 0)
                {
                    debt.RemainingAmount = Math.Max(0, debt.RemainingAmount - returnAmount.Value);
                }
            }
            else if (status == DebtStatus.Cancelled)
            {
                debt.CancelledDate = DateTime.Today;
            }

            // เธ–เนเธฒเน€เธเธฅเธตเนเธขเธเน€เธเนเธเธชเธ–เธฒเธเธฐเธญเธทเนเธเธ—เธตเนเนเธกเนเนเธเน WaitingGoods เนเธซเนเน€เธเธฅเธตเธขเธฃเน PendingProducts เนเธฅเธฐ WaitingGoodsDate
            if (status != DebtStatus.WaitingGoods)
            {
                var oldPending = await _db.PendingProducts.Where(p => p.OutstandingDebtId == debt.Id || p.BillNo == debt.BillNo).ToListAsync();
                if (oldPending.Any())
                {
                    _db.PendingProducts.RemoveRange(oldPending);
                }
                debt.WaitingGoodsDate = null;
            }
            if (status != DebtStatus.Delivering)
            {
                debt.DeliveringDate = null;
            }
            if (status != DebtStatus.Postponed)
            {
                debt.PostponedDate = null;
            }

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _db.FileAttachments.Add(new FileAttachment
                {
                    OutstandingDebtId = debt.Id,
                    FileName = file.FileName,
                    FilePath = "/uploads/" + uniqueFileName,
                    UploadedBy = User.Identity?.Name ?? "system"
                });
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "UPDATE_DEBT_STATUS",
                Detail = $"Updated Bill {billNo} status from {oldStatus} to {status}. Note: {note}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"เธญเธฑเธเน€เธ”เธ•เธชเธ–เธฒเธเธฐเน€เธเนเธ '{status.ToThaiString()}' เน€เธฃเธตเธขเธเธฃเนเธญเธขเนเธฅเนเธง";
            return RedirectToAction("Detail", new { id = billNo });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReceipt(int id, string receiptNo, DateTime? receiptDate, string password)
        {
            var debt = await _db.OutstandingDebts.FindAsync(id);
            if (debt == null) return NotFound();

            // รหัสผ่านสำหรับการแก้ไขใบเสร็จ (ตามที่ร้องขอ)
            if (password != "029030445Rd*")
            {
                TempData["Error"] = "เธฃเธซเธฑเธชเธเนเธฒเธเนเธกเนเธ–เธนเธเธ•เนเธญเธ เนเธกเนเธชเธฒเธกเธฒเธฃเธ–เนเธเนเนเธเธเนเธญเธกเธนเธฅเนเธเน€เธชเธฃเนเธเนเธ”เน";
                return RedirectToAction("Detail", new { id = debt.BillNo });
            }

            string oldReceiptNo = debt.ReceiptNo;
            DateTime? oldReceiptDate = debt.ReceiptDate;

            debt.ReceiptNo = receiptNo ?? "";
            debt.ReceiptDate = receiptDate;

            // หากมีการระบุเลขที่ใบเสร็จ ถือว่าชำระครบ
            if (!string.IsNullOrEmpty(receiptNo))
            {
                debt.RemainingAmount = 0;
                debt.Status = DebtStatus.PaidTransfer;
                debt.FullyPaidDate = receiptDate ?? DateTime.Now;
                debt.PaidDate = receiptDate ?? DateTime.Now;
            }
            else
            {
                // หากลบเลขที่ใบเสร็จออก (คืนสถานะค้างชำระ)
                debt.RemainingAmount = debt.OriginalAmount;
                debt.Status = DebtStatus.Outstanding;
                debt.FullyPaidDate = null;
                debt.PaidDate = null;
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "UPDATE_RECEIPT",
                Detail = $"Updated Receipt for Bill {debt.BillNo}. Old: {oldReceiptNo}, New: {receiptNo}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "เธญเธฑเธเน€เธ”เธ•เธเนเธญเธกเธนเธฅเนเธเน€เธชเธฃเนเธเธฃเธฑเธเน€เธเธดเธเธชเธณเน€เธฃเนเธ";
            return RedirectToAction("Detail", new { id = debt.BillNo });
        }

        [HttpGet]
        public async Task<IActionResult> Installment(string? search, string? salesRep)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;
            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode)) salesRep = currentUser.SalesRepCode;

            var q = _db.OutstandingDebts
                .Include(d => d.PaymentRecords)
                .Where(d => d.Status == DebtStatus.Installment || d.PaymentRecords.Count > 1);

            if (!string.IsNullOrEmpty(userAllowedRegion)) q = q.Where(d => d.Province != null && d.Province.Contains(userAllowedRegion));
            if (!string.IsNullOrEmpty(salesRep)) q = q.Where(d => d.SalesRep != null && d.SalesRep.Contains(salesRep));
            if (!string.IsNullOrEmpty(search))
                q = q.Where(d => d.CustomerName.Contains(search) || d.BillNo.Contains(search) || d.CustomerCode.Contains(search));

            var debts = await q.OrderByDescending(d => d.BillDate).ToListAsync();
            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.SalesReps = await _db.OutstandingDebts.AsNoTracking()
                .Where(d => d.Status == DebtStatus.Installment || d.PaymentRecords.Count > 1)
                .Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            return View(debts);
        }
        [HttpGet]
        public async Task<IActionResult> BadDebt(string? search, string? salesRep)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode)) salesRep = currentUser.SalesRepCode;

            var debts = await _svc.GetDebtorsAsync(search, userAllowedRegion, userAllowedProvinces, salesRep, "BadDebt", null, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);

            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.SalesReps = await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            ViewBag.IsRestricted = isRestricted;

            return View(debts);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCancelledExcel(string? search, string? salesRep)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool canDownload = currentUser?.Role == "admin" || currentUser?.CanDownload == true;
            if (!canDownload) return Forbid();

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode)) salesRep = currentUser.SalesRepCode;

            var debts = await _svc.GetCancelledDebtsAsync(search, salesRep, 
                isRestricted ? currentUser?.AllowedRegion : null,
                isRestricted ? currentUser?.AllowedProvinces : null,
                isRestricted ? currentUser?.AllowedDistricts : null);
                
            var fileBytes = await _svc.ExportDebtorsExcelAsync(debts);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CancelledBills_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportCancelledPdf(string? search, string? salesRep)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool canDownload = currentUser?.Role == "admin" || currentUser?.CanDownload == true;
            if (!canDownload) return Forbid();

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            if (isRestricted && !string.IsNullOrEmpty(currentUser?.SalesRepCode)) salesRep = currentUser.SalesRepCode;

            var debts = await _svc.GetCancelledDebtsAsync(search, salesRep, 
                isRestricted ? currentUser?.AllowedRegion : null,
                isRestricted ? currentUser?.AllowedProvinces : null,
                isRestricted ? currentUser?.AllowedDistricts : null);
                
            ViewBag.Search = search; ViewBag.SalesRep = salesRep; ViewBag.Status = "cancelled"; ViewBag.PrintedBy = currentUser?.FullName ?? User.Identity?.Name ?? "Admin"; return View("PrintPdf", debts);
        }
    }
}





