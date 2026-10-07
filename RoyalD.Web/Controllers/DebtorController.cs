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
            string? status = null, 
            string? poSearch = null, 
            string? region = null, 
            string? province = null, 
            string? district = null,
            string? credit = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            var allDbReps = await SalesRepHelper.GetAllKnownSalesRepsAsync(_db, _cache);

            var (filterReps, selectedRepForUi, hasMultipleAssigned) = SalesRepHelper.ResolveFilter(
                isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);

            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;

            var areaScope = RegionHelper.ResolveAreaFilter(
                isRestricted, userAllowedRegion, userAllowedProvinces, region, province);

            var debts = await _svc.GetDebtorsAsync(
                search: search,
                region: areaScope.SelectedRegion,
                province: areaScope.SelectedProvince,
                salesRep: effectiveRepParam,
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
            
            if (string.Equals(status, "outstanding", StringComparison.OrdinalIgnoreCase))
            {
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding || d.Status == DebtStatus.Installment || (int)d.Status == 100).ToList();
            }
            else if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
            {
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && d.Status != DebtStatus.Installment && (int)d.Status != 100 && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            }
            else if (string.Equals(status, "overdue120", StringComparison.OrdinalIgnoreCase))
            {
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).OrderByDescending(d => (today - d.BillDate.AddDays(d.Credit)).TotalDays).ThenBy(d => d.BillDate).ToList();
            }
            else if (!string.IsNullOrEmpty(status) && (status.Equals("Installment", StringComparison.OrdinalIgnoreCase) || status == "100"))
            {
                debts = debts.Where(d => d.Status == DebtStatus.Installment || (int)d.Status == 100).ToList();
            }
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<DebtStatus>(status, true, out var parsedStatus))
            {
                debts = debts.Where(d => d.Status == parsedStatus).ToList();
            }
            
            // เธเธฃเธญเธเธเธดเธฅเธ—เธตเนเธเธณเธฃเธฐเธเธฃเธเนเธฅเนเธงเนเธฅเธฐเน€เธฅเธข 1 เธงเธฑเธ (เธขเนเธฒเธขเนเธเธเธฃเธฐเธงเธฑเธ•เธด) เธขเธเน€เธงเนเธเธเธดเธฅเธเนเธญเธเธเธณเธฃเธฐ
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
                        return false; // เธเธฃเธญเธเธญเธญเธ เน€เธเธฃเธฒเธฐเน€เธเนเธฒเธเธงเนเธฒ 1 เธงเธฑเธ
                    }
                }
                return true;
            }).ToList();

            if (!string.IsNullOrEmpty(district))
            {
                var distVariants = RegionHelper.ExpandDistrictVariants(new[] { district });
                debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && (distVariants.Contains(d.District) || d.District.Contains(district) || distVariants.Any(v => d.District.Contains(v)))).ToList();
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
                creditOptions.Add("0_7"); // เน€เธโฌเน€เธยเน€เธเธ”เน€เธยเน€เธเธเน€เธโ€ / 7 เน€เธเธเน€เธเธ‘เน€เธย
            foreach (var c in rawCredits.Where(c => c != 0 && c != 7))
            {
                creditOptions.Add(c.ToString());
            }

            ViewBag.SearchTerm = search;
            ViewBag.PoSearch = poSearch;
            ViewBag.SelectedSalesRep = selectedRepForUi;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedRegion = areaScope.SelectedRegion;
            ViewBag.SelectedProvince = areaScope.SelectedProvince;
            ViewBag.SelectedDistrict = district;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCredit = credit;
            ViewBag.IsRestricted = isRestricted;
            ViewBag.AssignedSalesRep = currentUser?.SalesRepCode;
            ViewBag.IsLockedRegion = false;
            ViewBag.IsLockedProvince = false;
            ViewBag.IsLockedDistrict = !string.IsNullOrEmpty(userAllowedDistricts);
            ViewBag.Regions = areaScope.AvailableRegions;
            ViewBag.Provinces = areaScope.AvailableProvinces;
            ViewBag.HasRegionRestriction = areaScope.HasRegionRestriction;
            ViewBag.HasProvinceRestriction = areaScope.HasProvinceRestriction;
            ViewBag.RegionProvincesMap = areaScope.RegionProvincesMap;
            ViewBag.AllProvincesMap = RegionHelper.DisplayProvinces;
            ViewBag.ProvinceDistricts = ThailandDistrictData.All;




            var assignedReps = isRestricted && !string.IsNullOrWhiteSpace(currentUser?.SalesRepCode)
                ? SalesRepHelper.GetAssignedSalesReps(currentUser.SalesRepCode, allDbReps)
                : new List<string>();

            if (isRestricted && assignedReps.Count > 0)
            {
                ViewBag.SalesReps = assignedReps;
                ViewBag.IsLockedSalesRep = assignedReps.Count <= 1;
                ViewBag.IsMultiAssignedRep = assignedReps.Count > 1;
            }
            else
            {
                ViewBag.SalesReps = allDbReps;
                ViewBag.IsLockedSalesRep = false;
                ViewBag.IsMultiAssignedRep = false;
            }

            string pos = (currentUser?.Position ?? "").Trim();
            bool isSalesRepPos = pos == "เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos == "เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos.Contains("เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธย") || pos.Contains("เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);

            ViewBag.CreditOptions = creditOptions;
            ViewBag.TotalAmount = debts.Sum(d => d.RemainingAmount);
            ViewBag.OverdueCount = debts.Count(d => d.DueDate < DateTime.Today && d.Status == DebtStatus.Outstanding);
            ViewBag.CanChangeDebtStatus = DebtStatusPermissionHelper.CanChangeDebtStatus(currentUser, User);
            ViewBag.CanManageReturnedBills = DebtStatusPermissionHelper.CanManageReturnedBills(currentUser, User);
            ViewBag.CanDeleteDebtor = currentUser != null && (currentUser.Role == "admin" || currentUser.CanDeleteDebtor);
            ViewBag.CanDownload = canDownload;

            int totalRecords = debts.Count;
            int effectivePageSize = pageSize > 0 ? pageSize : 50;
            int totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var pagedDebts = debts.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = effectivePageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalPages;

            return View(pagedDebts);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(
            string? search, 
            string? salesRep, 
            string? status = null, 
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
            bool isSalesRepPos = pos == "เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos == "เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos.Contains("เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธย") || pos.Contains("เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);
            if (!canDownload) return Forbid();

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            var rawDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var (filterReps, _, _) = SalesRepHelper.ResolveFilter(isRestricted, currentUser?.SalesRepCode, salesRep, rawDbReps);
            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;
            if (isRestricted && !string.IsNullOrEmpty(userAllowedRegion)) region = userAllowedRegion;

            var debts = await _svc.GetDebtorsAsync(
                search: search,
                region: region,
                province: province,
                salesRep: effectiveRepParam,
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

            if (string.Equals(status, "outstanding", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding).ToList();
            else if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            else if (string.Equals(status, "overdue120", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).OrderByDescending(d => (today - d.BillDate.AddDays(d.Credit)).TotalDays).ThenBy(d => d.BillDate).ToList();
            else if (!string.IsNullOrEmpty(status) && (status.Equals("Installment", StringComparison.OrdinalIgnoreCase) || status == "100"))
                debts = debts.Where(d => d.Status == DebtStatus.Installment || (int)d.Status == 100).ToList();
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<DebtStatus>(status, true, out var parsedStatus))
                debts = debts.Where(d => d.Status == parsedStatus).ToList();

            if (!string.IsNullOrEmpty(district))
            {
                var distVariants = RegionHelper.ExpandDistrictVariants(new[] { district });
                debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && (distVariants.Contains(d.District) || d.District.Contains(district) || distVariants.Any(v => d.District.Contains(v)))).ToList();
            }
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
            string? status = null, 
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
            bool isSalesRepPos = pos == "เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos == "เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos.Contains("เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธย") || pos.Contains("เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);
            if (!canDownload) return Forbid();

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            var rawDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var (filterReps, selectedRepForUi, _) = SalesRepHelper.ResolveFilter(isRestricted, currentUser?.SalesRepCode, salesRep, rawDbReps);
            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;
            var areaScope = RegionHelper.ResolveAreaFilter(
                isRestricted, userAllowedRegion, userAllowedProvinces, region, province);

            var debts = await _svc.GetDebtorsAsync(search, areaScope.SelectedRegion, areaScope.SelectedProvince, effectiveRepParam, null, credit, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);

            var today = DateTime.Today;
            if (string.Equals(status, "outstanding", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding).ToList();
            else if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            else if (string.Equals(status, "overdue120", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).OrderByDescending(d => (today - d.BillDate.AddDays(d.Credit)).TotalDays).ThenBy(d => d.BillDate).ToList();
            else if (!string.IsNullOrEmpty(status) && (status.Equals("Installment", StringComparison.OrdinalIgnoreCase) || status == "100"))
                debts = debts.Where(d => d.Status == DebtStatus.Installment || (int)d.Status == 100).ToList();
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<DebtStatus>(status, true, out var parsedStatus))
                debts = debts.Where(d => d.Status == parsedStatus).ToList();

            if (!string.IsNullOrEmpty(district))
            {
                var distVariants = RegionHelper.ExpandDistrictVariants(new[] { district });
                debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && (distVariants.Contains(d.District) || d.District.Contains(district) || distVariants.Any(v => d.District.Contains(v)))).ToList();
            }
            if (startDate.HasValue) debts = debts.Where(d => d.BillDate >= startDate.Value.Date).ToList();
            if (endDate.HasValue) debts = debts.Where(d => d.BillDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            if (!string.IsNullOrEmpty(poSearch)) debts = debts.Where(d => !string.IsNullOrEmpty(d.PoNumber) && d.PoNumber.Contains(poSearch)).ToList();

            var csv = new System.Text.StringBuilder();
            csv.Append('\uFEFF');
            csv.AppendLine("เน€เธโฌเน€เธเธ…เน€เธยเน€เธโ€”เน€เธเธ•เน€เธยเน€เธยเน€เธเธ”เน€เธเธ…,เน€เธเธเน€เธเธ‘เน€เธยเน€เธโ€”เน€เธเธ•เน€เธยเน€เธยเน€เธเธ”เน€เธเธ…,เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธเธ“เน€เธเธเน€เธยเน€เธโ€,เน€เธเธเน€เธเธเน€เธเธ‘เน€เธเธเน€เธเธ…เน€เธเธเน€เธยเน€เธยเน€เธยเน€เธเธ’,เน€เธยเน€เธเธ—เน€เธยเน€เธเธเน€เธเธ…เน€เธเธเน€เธยเน€เธยเน€เธยเน€เธเธ’,เน€เธเธเน€เธเธ“เน€เธโฌเน€เธย เน€เธเธ,เน€เธยเน€เธเธ‘เน€เธยเน€เธเธเน€เธเธเน€เธเธ‘เน€เธโ€,เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธยเน€เธยเน€เธเธ’เน€เธเธ,เน€เธเธเน€เธเธเน€เธโ€เน€เธโฌเน€เธโ€เน€เธเธ”เน€เธเธ,เน€เธเธเน€เธเธเน€เธโ€เน€เธยเน€เธยเน€เธยเน€เธยเน€เธเธ’เน€เธย,เน€เธเธเน€เธโ€“เน€เธเธ’เน€เธยเน€เธเธ,เน€เธโฌเน€เธเธ…เน€เธยเน€เธโ€”เน€เธเธ•เน€เธยเน€เธยเน€เธยเน€เธโฌเน€เธเธเน€เธเธเน€เธยเน€เธย,เน€เธเธเน€เธเธ‘เน€เธยเน€เธโ€”เน€เธเธ•เน€เธยเน€เธเธเน€เธเธ‘เน€เธยเน€เธโฌเน€เธยเน€เธเธ”เน€เธย");

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
            string? status = null, 
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
            bool isSalesRepPos = pos == "เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos == "เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" || pos.Contains("เน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธย") || pos.Contains("เน€เธยเน€เธยเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ’เน€เธยเน€เธยเน€เธเธ’เน€เธเธ");
            bool canDownload = !isSalesRepPos && (currentUser?.Role == "admin" || currentUser?.CanDownload == true);
            if (!canDownload)
            {
                return Forbid();
            }

            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            var allDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var (filterReps, selectedRepForUi, _) = SalesRepHelper.ResolveFilter(
                isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);

            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;

            var areaScope = RegionHelper.ResolveAreaFilter(
                isRestricted, userAllowedRegion, userAllowedProvinces, region, province);

            var debts = await _svc.GetDebtorsAsync(
                search: search,
                region: areaScope.SelectedRegion,
                province: areaScope.SelectedProvince,
                salesRep: effectiveRepParam,
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

            if (string.Equals(status, "outstanding", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding).ToList();
            else if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status != DebtStatus.Outstanding && (d.RemainingAmount <= 0 || d.FullyPaidDate.HasValue)).ToList();
            else if (string.Equals(status, "overdue120", StringComparison.OrdinalIgnoreCase))
                debts = debts.Where(d => d.Status == DebtStatus.Outstanding && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120).OrderByDescending(d => (today - d.BillDate.AddDays(d.Credit)).TotalDays).ThenBy(d => d.BillDate).ToList();
            else if (!string.IsNullOrEmpty(status) && (status.Equals("Installment", StringComparison.OrdinalIgnoreCase) || status == "100"))
                debts = debts.Where(d => d.Status == DebtStatus.Installment || (int)d.Status == 100).ToList();
            else if (!string.IsNullOrEmpty(status) && Enum.TryParse<DebtStatus>(status, true, out var parsedStatus))
                debts = debts.Where(d => d.Status == parsedStatus).ToList();

            if (!string.IsNullOrEmpty(district))
            {
                var distVariants = RegionHelper.ExpandDistrictVariants(new[] { district });
                debts = debts.Where(d => !string.IsNullOrEmpty(d.District) && (distVariants.Contains(d.District) || d.District.Contains(district) || distVariants.Any(v => d.District.Contains(v)))).ToList();
            }
            if (startDate.HasValue)
                debts = debts.Where(d => d.BillDate >= startDate.Value.Date).ToList();
            if (endDate.HasValue)
                debts = debts.Where(d => d.BillDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            if (!string.IsNullOrEmpty(poSearch))
                debts = debts.Where(d => !string.IsNullOrEmpty(d.PoNumber) && d.PoNumber.Contains(poSearch)).ToList();

            ViewBag.Search = search;
            ViewBag.SalesRep = selectedRepForUi;
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
            ViewBag.CanChangeDebtStatus = DebtStatusPermissionHelper.CanChangeDebtStatus(currentUser, User);
            ViewBag.CanManageReturnedBills = DebtStatusPermissionHelper.CanManageReturnedBills(currentUser, User);
            ViewBag.CanDeleteDebtor = currentUser != null && (currentUser.Role == "admin" || currentUser.CanDeleteDebtor);
            ViewBag.CanScreenCapture = currentUser != null && (currentUser.Role == "admin" || currentUser.CanScreenCapture);

            var items = await _db.SalesBillItems.AsNoTracking().Where(i => i.BillNo == debt.BillNo).ToListAsync();
            ViewBag.Items = items;
            var bill = await _db.SalesBills.AsNoTracking().FirstOrDefaultAsync(b => b.BillNo == debt.BillNo);
            ViewBag.SalesBill = bill;

            return View(debt);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [ActionName("DeleteDebt")]
        public async Task<IActionResult> DeleteDebtPost(string id) => await DeleteDebtor(id);

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDebtor(string id)
        {
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool canDelete = currentUser != null && (currentUser.Role == "admin" || currentUser.CanDeleteDebtor);
            if (!canDelete)
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์ในการลบการ์ดลูกหนี้";
                return RedirectToAction("Index");
            }

            int.TryParse(id, out int intId);
            var debt = await _db.OutstandingDebts
                .Include(d => d.PaymentRecords)
                .FirstOrDefaultAsync(d => (intId > 0 && d.Id == intId) || d.BillNo == id || EF.Functions.ILike(d.BillNo, id));
            if (debt == null) return NotFound();

            var billNo = debt.BillNo;
            var customerName = debt.CustomerName;
            var amount = debt.RemainingAmount;

            if (debt.PaymentRecords?.Any() == true)
            {
                _db.PaymentRecords.RemoveRange(debt.PaymentRecords);
            }
            var attachments = await _db.FileAttachments.Where(f => f.OutstandingDebtId == debt.Id).ToListAsync();
            if (attachments.Any())
            {
                _db.FileAttachments.RemoveRange(attachments);
            }

            _db.OutstandingDebts.Remove(debt);

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "",
                Action = "DELETE_DEBTOR_CARD",
                Detail = $"Deleted Debtor Card {billNo} (Customer: {customerName}, Amount: {amount:N2})",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            _cache.Remove("all_debtor_reps");
            _cache.Remove("all_debtor_credits");
            TempData["Success"] = $"ลบการ์ดลูกหนี้เลขที่บิล {billNo} เรียบร้อยแล้ว";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> History(string? search, string? salesRep, DateTime? fromDate = null, DateTime? toDate = null, string? dateType = null, int page = 1, int pageSize = 50)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            var allDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var (filterReps, selectedRepForUi, _) = SalesRepHelper.ResolveFilter(isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);
            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;

            var debts = await _svc.GetPaidHistoryAsync(search, effectiveRepParam, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);
            if (fromDate.HasValue) { if (dateType == "bill") debts = debts.Where(d => d.BillDate >= fromDate.Value.Date).ToList(); else debts = debts.Where(d => (d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate) >= fromDate.Value.Date).ToList(); }
            if (toDate.HasValue) { if (dateType == "bill") debts = debts.Where(d => d.BillDate <= toDate.Value.Date.AddDays(1).AddTicks(-1)).ToList(); else debts = debts.Where(d => (d.ReceiptDate ?? d.FullyPaidDate ?? d.PaidDate) <= toDate.Value.Date.AddDays(1).AddTicks(-1)).ToList(); }
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd"); ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd"); ViewBag.DateType = dateType;
            
            // Backfill missing or invalid Customer Code/Name for the same ReceiptNo
            var receiptGroups = debts.Where(d => !string.IsNullOrEmpty(d.ReceiptNo)).GroupBy(d => d.ReceiptNo).ToList();
            var neededBillNos = new List<string>();
            foreach (var g in receiptGroups)
            {
                var validName = g.FirstOrDefault(d => !string.IsNullOrEmpty(d.CustomerName) && !d.CustomerName.Contains("/") && !d.CustomerName.StartsWith("RD", StringComparison.OrdinalIgnoreCase))?.CustomerName;
                if (string.IsNullOrEmpty(validName))
                {
                    var firstBill = g.FirstOrDefault()?.BillNo;
                    if (!string.IsNullOrEmpty(firstBill))
                        neededBillNos.Add(firstBill);
                }
            }

            var billLookup = neededBillNos.Any()
                ? (await _db.SalesBills.AsNoTracking()
                    .Where(b => neededBillNos.Contains(b.BillNo))
                    .Select(b => new { b.BillNo, b.CustomerCode, b.CustomerName })
                    .ToListAsync())
                    .ToDictionary(b => b.BillNo, b => (CustomerCode: b.CustomerCode, CustomerName: b.CustomerName))
                : new Dictionary<string, (string CustomerCode, string CustomerName)>();

            foreach (var g in receiptGroups)
            {
                var validCode = g.FirstOrDefault(d => !string.IsNullOrEmpty(d.CustomerCode) && !d.CustomerCode.Contains("/"))?.CustomerCode;
                var validName = g.FirstOrDefault(d => !string.IsNullOrEmpty(d.CustomerName) && !d.CustomerName.Contains("/") && !d.CustomerName.StartsWith("RD", StringComparison.OrdinalIgnoreCase))?.CustomerName;
                
                // If still missing, fallback to batch-loaded SalesBills
                if (string.IsNullOrEmpty(validName))
                {
                    var firstBill = g.FirstOrDefault()?.BillNo;
                    if (!string.IsNullOrEmpty(firstBill) && billLookup.TryGetValue(firstBill, out var realBill))
                    {
                        validCode = realBill.CustomerCode;
                        validName = realBill.CustomerName;
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
            ViewBag.SalesRep = selectedRepForUi;
            ViewBag.TotalCount = debts.Count();
            ViewBag.TotalAmount = debts.Sum(d => d.OriginalAmount);
            var assignedReps = isRestricted && !string.IsNullOrWhiteSpace(currentUser?.SalesRepCode)
                ? SalesRepHelper.GetAssignedSalesReps(currentUser.SalesRepCode, allDbReps)
                : new List<string>();
            ViewBag.SalesReps = (isRestricted && assignedReps.Count > 0) ? assignedReps : allDbReps;
            ViewBag.IsRestricted = isRestricted;

            int totalRecords = debts.Count;
            int effectivePageSize = pageSize > 0 ? pageSize : 50;
            int totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var pagedDebts = debts.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = effectivePageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalPages;

            return View(pagedDebts);
        }

        public async Task<IActionResult> Cancelled(string? search, string? salesRep, int page = 1, int pageSize = 50)
        {
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isRestricted = currentUser != null && currentUser.Role != "admin";
            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : null;
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : null;
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : null;

            var allDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var (filterReps, selectedRepForUi, _) = SalesRepHelper.ResolveFilter(isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);
            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;

            var debts = await _svc.GetCancelledDebtsAsync(search, effectiveRepParam, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);
            var assignedReps = isRestricted && !string.IsNullOrWhiteSpace(currentUser?.SalesRepCode)
                ? SalesRepHelper.GetAssignedSalesReps(currentUser.SalesRepCode, allDbReps)
                : new List<string>();

            ViewBag.Search = search;
            ViewBag.SalesRep = selectedRepForUi;
            ViewBag.SelectedSalesRep = selectedRepForUi;
            ViewBag.TotalCount = debts.Count();
            ViewBag.TotalAmount = debts.Sum(d => d.OriginalAmount);
            ViewBag.SalesReps = (isRestricted && assignedReps.Count > 0) ? assignedReps : allDbReps;
            ViewBag.IsRestricted = isRestricted;

            int totalRecords = debts.Count;
            int effectivePageSize = pageSize > 0 ? pageSize : 50;
            int totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var pagedDebts = debts.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = effectivePageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalPages;

            return View(pagedDebts);
        }

        private bool IsSalesRepUser()
        {
            if (User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin")) return false;
            var pos = User.FindFirst("Position")?.Value ?? "";
            return pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย");
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
            IFormFile? statusFile)
        {
            if (IsSalesRepUser())
            {
                TempData["Error"] = "รหัสผู้แทนขายไม่มีสิทธิ์บันทึกหรือแก้ไขการรับเงิน";
                return RedirectToAction("Detail", new { id = billNo });
            }

            var debt = await _db.OutstandingDebts.Include(d => d.PaymentRecords).FirstOrDefaultAsync(d => d.BillNo == billNo);
            if (debt == null) return NotFound();

            if (amount <= 0 || amount > debt.RemainingAmount)
            {
                TempData["Error"] = "เน€เธเธเน€เธเธเน€เธโ€เน€เธยเน€เธเธ“เน€เธเธเน€เธเธเน€เธยเน€เธเธเน€เธยเน€เธโ€“เน€เธเธเน€เธยเน€เธโ€ขเน€เธยเน€เธเธเน€เธย (เน€เธโ€ขเน€เธยเน€เธเธเน€เธยเน€เธเธเน€เธเธ’เน€เธยเน€เธยเน€เธเธเน€เธยเน€เธเธ’ 0 เน€เธยเน€เธเธ…เน€เธเธเน€เธยเน€เธเธเน€เธยเน€เธโฌเน€เธยเน€เธเธ”เน€เธยเน€เธเธเน€เธเธเน€เธโ€เน€เธยเน€เธยเน€เธยเน€เธยเน€เธเธ’เน€เธย)";
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

            if (statusFile != null && statusFile.Length > 0)
            {
                var supabaseStorage = HttpContext.RequestServices.GetService<RoyalD.Web.Services.SupabaseStorageService>();
                if (supabaseStorage != null)
                {
                    string? uploadedUrl = await supabaseStorage.UploadFileAsync(statusFile, "uploads");
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        rec.Attachments.Add(new FileAttachment
                        {
                            FileName = statusFile.FileName,
                            FilePath = uploadedUrl,
                            UploadedBy = User.Identity?.Name ?? "system"
                        });
                    }
                }
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
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"เน€เธยเน€เธเธ‘เน€เธยเน€เธโ€”เน€เธเธ–เน€เธยเน€เธยเน€เธเธ’เน€เธเธเน€เธเธเน€เธเธ‘เน€เธยเน€เธยเน€เธเธ“เน€เธเธเน€เธเธเน€เธโฌเน€เธยเน€เธเธ”เน€เธย {amount:N2} เน€เธยเน€เธเธ’เน€เธโ€” เน€เธเธเน€เธเธ“เน€เธโฌเน€เธเธเน€เธยเน€เธยเน€เธยเน€เธเธ…เน€เธยเน€เธเธ";
            return RedirectToAction("Detail", new { id = billNo });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            string billNo, 
            DebtStatus status, 
            DateTime? postponedDate, 
            DateTime? deliveringDate, 
            DateTime? waitingGoodsDate, 
            DateTime? returnedToAccountDate,
            string? returnedToAccountReason,
            List<string>? waitingProductCodes,
            List<string>? allProductCodes,
            List<string>? allProductNames,
            decimal? badDebtAmount, 
            DateTime? badDebtDate, 
            decimal? returnAmount, 
            bool? isReturnCutFromBill, 
            string? returnType, 
            string? note, 
            IFormFile? statusFile)
        {
            if (Request.Form.ContainsKey("newStatus") && Enum.TryParse<DebtStatus>(Request.Form["newStatus"], true, out var formStatus))
            {
                status = formStatus;
            }

            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool canChangeStatus = DebtStatusPermissionHelper.CanChangeDebtStatus(currentUser, User);
            bool hasReturnedBills = DebtStatusPermissionHelper.CanManageReturnedBills(currentUser, User);
            bool canChangeThisStatus = (status == DebtStatus.ReturnedToAccount || status == DebtStatus.Cancelled) ? hasReturnedBills : canChangeStatus;
            if (!canChangeThisStatus)
            {
                if (status == DebtStatus.ReturnedToAccount)
                {
                    TempData["Error"] = "คุณไม่มีสิทธิ์เปลี่ยนสถานะเป็นบิลส่งกลับบัญชี (เฉพาะคุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร, คุณวนิดา เท่านั้น)";
                }
                else if (status == DebtStatus.Cancelled)
                {
                    TempData["Error"] = "คุณไม่มีสิทธิ์เปลี่ยนสถานะเป็นบิลยกเลิก (เฉพาะคุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร, คุณวนิดา เท่านั้น)";
                }
                else
                {
                    TempData["Error"] = "คุณไม่มีสิทธิ์เปลี่ยนสถานะหนี้ (เฉพาะคุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร เท่านั้น)";
                }
                return RedirectToAction("Detail", new { id = billNo });
            }
            var debt = await _db.OutstandingDebts.FirstOrDefaultAsync(d => d.BillNo == billNo);
            if (debt == null) return NotFound();

            var bill = await _db.SalesBills.FirstOrDefaultAsync(b => b.BillNo == billNo);

            // 1. ตรวจสอบการยืนยันรหัสผ่านกรณีเปลี่ยนเป็นบิลส่งคืนกลับมาบัญชี (ReturnedToAccount)
            if (status == DebtStatus.ReturnedToAccount)
            {
                string? returnPwd = Request.Form["returnedBillPassword"].FirstOrDefault() ?? Request.Form["adminPassword"].FirstOrDefault();
                var (isReturnOk, returnApprover) = await DebtStatusPermissionHelper.VerifyReturnedBillApproverPasswordAsync(_db, returnPwd);
                if (!isReturnOk)
                {
                    TempData["Error"] = "การเปลี่ยนสถานะเป็นบิลส่งกลับบัญชี ต้องใส่รหัสผ่านของผู้มีสิทธิ์ (คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร, คุณวนิดา) หรือรหัสผ่านสำรอง 029030445Rd* เท่านั้น";
                    return RedirectToAction("Detail", new { id = billNo });
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "CHANGE_STATUS_RETURNED_TO_ACCOUNT",
                    Detail = $"เปลี่ยนสถานะการ์ดลูกหนี้ {billNo} เป็นบิลส่งคืนกลับมาบัญชี (อนุมัติโดย: {returnApprover})",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. ตรวจสอบการยืนยันรหัสผ่านกรณีเปลี่ยนเป็นบิลยกเลิก (Cancelled)
            if (status == DebtStatus.Cancelled)
            {
                string? cancelPwd = Request.Form["cancelBillPassword"].FirstOrDefault() ?? Request.Form["returnedBillPassword"].FirstOrDefault() ?? Request.Form["adminPassword"].FirstOrDefault();
                var (isCancelOk, cancelApprover) = await DebtStatusPermissionHelper.VerifyReturnedBillApproverPasswordAsync(_db, cancelPwd);
                if (!isCancelOk)
                {
                    TempData["Error"] = "การเปลี่ยนสถานะเป็นบิลยกเลิก ต้องใส่รหัสผ่านของผู้มีสิทธิ์ (คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร, คุณวนิดา) หรือรหัสผ่านสำรอง 029030445Rd* เท่านั้น";
                    return RedirectToAction("Detail", new { id = billNo });
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "CHANGE_STATUS_CANCELLED",
                    Detail = $"เปลี่ยนสถานะการ์ดลูกหนี้ {billNo} เป็นบิลยกเลิก (อนุมัติโดย: {cancelApprover})",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 3. ตรวจสอบกรณีกู้คืนจากบิลยกเลิกกลับมาเป็นสถานะปกติ (Restoring from Cancelled)
            bool isRestoringFromCancelled = (debt != null && debt.Status == DebtStatus.Cancelled && status != DebtStatus.Cancelled);
            if (isRestoringFromCancelled)
            {
                string? restorePwd = Request.Form["adminPassword"].FirstOrDefault() ?? Request.Form["returnedBillPassword"].FirstOrDefault() ?? Request.Form["cancelBillPassword"].FirstOrDefault();
                var (isRestoreOk, restoreApprover) = await DebtStatusPermissionHelper.VerifyPaidBillApproverPasswordAsync(_db, restorePwd);
                if (!isRestoreOk)
                {
                    TempData["Error"] = "การกู้คืนจากบิลยกเลิกมาเป็นบิลค้างชำระปกติ ต้องใส่รหัสผ่านของผู้มีสิทธิ์ (คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร) หรือรหัสผ่านสำรอง 029030445Rd* เท่านั้น";
                    return RedirectToAction("Detail", new { id = billNo });
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "RESTORE_CANCELLED_BILL",
                    Detail = $"กู้คืนบิลยกเลิก {billNo} กลับเป็นสถานะ {status} (อนุมัติโดย: {restoreApprover})",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.UtcNow
                });
            }

            bool isPaid = DebtStatusPermissionHelper.IsBillPaid(bill, debt);
            if (isPaid && status != DebtStatus.ReturnedToAccount && status != DebtStatus.Cancelled && !isRestoringFromCancelled)
            {
                string? adminPassword = Request.Form["adminPassword"].FirstOrDefault();
                var (isApproverOk, approverName) = await DebtStatusPermissionHelper.VerifyPaidBillApproverPasswordAsync(_db, adminPassword);
                if (!isApproverOk)
                {
                    TempData["Error"] = "บิลนี้ชำระเงินครบถ้วนแล้ว การเปลี่ยนสถานะต้องใส่รหัสผ่านของผู้มีสิทธิ์ (คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร) หรือรหัสผ่านสำรอง 029030445Rd* เท่านั้น";
                    return RedirectToAction("Detail", new { id = billNo });
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "CHANGE_STATUS_PAID_DEBT",
                    Detail = $"เปลี่ยนสถานะการ์ดลูกหนี้บิลที่ชำระแล้ว {billNo} จาก {debt.Status} เป็น {status} (อนุมัติโดย: {approverName})",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var oldStatus = debt.Status;
            if (status == DebtStatus.Installment || (int)status == 100)
            {
                status = DebtStatus.Installment;
                debt.Status = DebtStatus.Installment;
                if (decimal.TryParse(Request.Form["installmentRemainingAmount"], out var customAmt) && customAmt > 0)
                {
                    debt.RemainingAmount = customAmt;
                }
                else
                {
                    var payments = await _db.PaymentRecords.Where(p => p.OutstandingDebtId == debt.Id).ToListAsync();
                    decimal paid = payments.Sum(p => p.PaidAmount);
                    if (debt.RemainingAmount <= 0)
                    {
                        debt.RemainingAmount = (debt.OriginalAmount > paid) ? (debt.OriginalAmount - paid) : debt.OriginalAmount;
                    }
                }
                debt.FullyPaidDate = null;
                if (bill != null) bill.IsFullyPaid = false;
            }
            else
            {
                debt.Status = status;
            }
            debt.Note = note ?? "";

            if (status == DebtStatus.ReturnedToAccount)
            {
                debt.Status = DebtStatus.ReturnedToAccount;
                debt.ReturnedToAccountDate = returnedToAccountDate ?? DateTime.Today;
                debt.ReturnedToAccountReason = !string.IsNullOrWhiteSpace(returnedToAccountReason) ? returnedToAccountReason.Trim() : (note ?? "");
                debt.Note = debt.ReturnedToAccountReason;
            }
            else if (status == DebtStatus.Postponed)
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

                // เธฅเธเธฃเธฒเธขเธเธฒเธฃเธชเธดเธเธเนเธฒเธเนเธฒเธเธชเนเธเน€เธ”เธดเธกเธเธญเธเธเธดเธฅเธเธตเนเธญเธญเธเธเนเธญเธเธเธฑเธเธ—เธถเธเนเธซเธกเน
                var oldPending = await _db.PendingProducts.Where(p => p.OutstandingDebtId == debt.Id || p.BillNo == debt.BillNo).ToListAsync();
                if (oldPending.Any())
                {
                    _db.PendingProducts.RemoveRange(oldPending);
                }

                // เธเธฑเธเธ—เธถเธเธฃเธฒเธขเธเธฒเธฃเธชเธดเธเธเนเธฒเธเนเธฒเธเธชเนเธเธ—เธตเนเน€เธฅเธทเธญเธเธเธฒเธเธเธดเธฅ
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

                // เธชเธฃเนเธฒเธเธเนเธญเธเธงเธฒเธกเธชเธฃเธธเธเธชเธดเธเธเนเธฒเธเนเธฒเธเธชเนเธเธฅเธเนเธ Note
                string pendingSummary = savedPendingList.Count > 0 ? $"เน€เธเธเน€เธเธเน€เธเธเน€เธเธ”เน€เธยเน€เธยเน€เธยเน€เธเธ’: {string.Join(", ", savedPendingList)}" : "";
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
                if (debt.IsReturnCutFromBill == true && returnAmount.HasValue && returnAmount.Value > 0)
                {
                    debt.RemainingAmount = Math.Max(0, debt.RemainingAmount - returnAmount.Value);
                }
            }
            else if (status == DebtStatus.Cancelled)
            {
                debt.CancelledDate = DateTime.Today;
            }

            // Always unmark fully paid for active/special statuses so they show up in lists
            if (status == DebtStatus.BadDebt || 
                status == DebtStatus.WaitingGoods || 
                status == DebtStatus.ReturnPending || 
                status == DebtStatus.ReturnIssued || 
                status == DebtStatus.Delivering || 
                status == DebtStatus.Postponed ||
                status == DebtStatus.ChangeProduct ||
                status == DebtStatus.Consignment)
                if (bill != null) bill.IsFullyPaid = false;

            // เน€เธโ€“เน€เธยเน€เธเธ’เน€เธโฌเน€เธยเน€เธเธ…เน€เธเธ•เน€เธยเน€เธเธเน€เธยเน€เธโฌเน€เธยเน€เธยเน€เธยเน€เธเธเน€เธโ€“เน€เธเธ’เน€เธยเน€เธเธเน€เธเธเน€เธเธ—เน€เธยเน€เธยเน€เธโ€”เน€เธเธ•เน€เธยเน€เธยเน€เธเธเน€เธยเน€เธยเน€เธยเน€เธย WaitingGoods เน€เธยเน€เธเธเน€เธยเน€เธโฌเน€เธยเน€เธเธ…เน€เธเธ•เน€เธเธเน€เธเธเน€เธย PendingProducts เน€เธยเน€เธเธ…เน€เธเธ WaitingGoodsDate
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

            if (statusFile != null && statusFile.Length > 0)
            {
                var supabaseStorage = HttpContext.RequestServices.GetService<RoyalD.Web.Services.SupabaseStorageService>();
                if (supabaseStorage != null)
                {
                    string? uploadedUrl = await supabaseStorage.UploadFileAsync(statusFile, "uploads");
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        _db.FileAttachments.Add(new FileAttachment
                        {
                            OutstandingDebtId = debt.Id,
                            FileName = statusFile.FileName,
                            FilePath = uploadedUrl,
                            UploadedBy = User.Identity?.Name ?? "system"
                        });
                    }
                }
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
            TempData["Success"] = $"เน€เธเธเน€เธเธ‘เน€เธยเน€เธโฌเน€เธโ€เน€เธโ€ขเน€เธเธเน€เธโ€“เน€เธเธ’เน€เธยเน€เธเธเน€เธโฌเน€เธยเน€เธยเน€เธย '{status.ToThaiString()}' เน€เธโฌเน€เธเธเน€เธเธ•เน€เธเธเน€เธยเน€เธเธเน€เธยเน€เธเธเน€เธเธเน€เธยเน€เธเธ…เน€เธยเน€เธเธ";
            return RedirectToAction("Detail", new { id = billNo });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReceipt(int id, string receiptNo, DateTime? receiptDate, string password)
        {
            var debt = await _db.OutstandingDebts.FindAsync(id);
            if (debt == null) return NotFound();

            if (IsSalesRepUser())
            {
                TempData["Error"] = "รหัสผู้แทนขายไม่มีสิทธิ์แก้ไขข้อมูลใบเสร็จรับเงิน";
                return RedirectToAction("Detail", new { id = debt.BillNo });
            }

            // เธฃเธซเธฑเธชเธœเนˆเธฒเธ™เธชเธณเธซเธฃเธฑเธšเธ เธฒเธฃเน เธ เน‰เน„เธ‚เนƒเธšเน€เธชเธฃเน‡เธˆ (เธ•เธฒเธกเธ—เธตเนˆเธฃเน‰เธญเธ‡เธ‚เธญ)
            if (password != "029030445Rd*")
            {
                TempData["Error"] = "เน€เธ˜เธƒเน€เธ˜เธ‹เน€เธ˜เธ‘เน€เธ˜เธŠเน€เธ˜ยœเน€เธ™ยˆเน€เธ˜เธ’เน€เธ˜ย™เน€เธ™ย„เน€เธ˜เธ เน€เธ™ยˆเน€เธ˜โ€“เน€เธ˜เธ™เน€เธ˜ย เน€เธ˜โ€ขเน€เธ™ย‰เน€เธ˜เธ เน€เธ˜ย‡ เน€เธ™ย„เน€เธ˜เธ เน€เธ™ยˆเน€เธ˜เธŠเน€เธ˜เธ’เน€เธ˜เธ เน€เธ˜เธ’เน€เธ˜เธƒเน€เธ˜โ€“เน€เธ™ย เน€เธ˜ย เน€เธ™ย‰เน€เธ™ย„เน€เธ˜ย‚เน€เธ˜ย‚เน€เธ™ย‰เน€เธ˜เธ เน€เธ˜เธ เน€เธ˜เธ™เน€เธ˜เธ…เน€เธ™ยƒเน€เธ˜ยšเน€เธ™โ‚ฌเน€เธ˜เธŠเน€เธ˜เธƒเน€เธ™ย‡เน€เธ˜ยˆเน€เธ™ย„เน€เธ˜โ€ เน€เธ™ย‰";
                return RedirectToAction("Detail", new { id = debt.BillNo });
            }

            string oldReceiptNo = debt.ReceiptNo;
            DateTime? oldReceiptDate = debt.ReceiptDate;

            debt.ReceiptNo = receiptNo ?? "";
            debt.ReceiptDate = receiptDate;

            // เธซเธฒเธเธกเธตเธเธฒเธฃเธฃเธฐเธเธธเน€เธฅเธเธ—เธตเนเนเธเน€เธชเธฃเนเธ เธ–เธทเธญเธงเนเธฒเธเธณเธฃเธฐเธเธฃเธ
            if (!string.IsNullOrEmpty(receiptNo))
            {
                debt.RemainingAmount = 0;
                debt.Status = DebtStatus.PaidTransfer;
                debt.FullyPaidDate = receiptDate ?? DateTime.Now;
                debt.PaidDate = receiptDate ?? DateTime.Now;
            }
            else
            {
                // เธซเธฒเธเธฅเธเน€เธฅเธเธ—เธตเนเนเธเน€เธชเธฃเนเธเธญเธญเธ (เธเธทเธเธชเธ–เธฒเธเธฐเธเนเธฒเธเธเธณเธฃเธฐ)
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
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "เน€เธเธเน€เธเธ‘เน€เธยเน€เธโฌเน€เธโ€เน€เธโ€ขเน€เธยเน€เธยเน€เธเธเน€เธเธเน€เธเธเน€เธเธ…เน€เธยเน€เธยเน€เธโฌเน€เธเธเน€เธเธเน€เธยเน€เธยเน€เธเธเน€เธเธ‘เน€เธยเน€เธโฌเน€เธยเน€เธเธ”เน€เธยเน€เธเธเน€เธเธ“เน€เธโฌเน€เธเธเน€เธยเน€เธย";
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

            var allDbReps = await _db.OutstandingDebts.AsNoTracking()
                .Where(d => d.Status == DebtStatus.Installment || d.PaymentRecords.Count > 1)
                .Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();

            var (filterReps, selectedRepForUi, hasMultipleAssigned) = SalesRepHelper.ResolveFilter(
                isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);

            var q = _db.OutstandingDebts
                .Include(d => d.PaymentRecords)
                .Where(d => d.Status == DebtStatus.Installment || d.PaymentRecords.Count > 1);

            if (!string.IsNullOrEmpty(userAllowedRegion)) q = q.Where(d => d.Province != null && d.Province.Contains(userAllowedRegion));
            if (filterReps != null && filterReps.Count > 0) q = q.Where(d => filterReps.Contains(d.SalesRep));
            if (!string.IsNullOrEmpty(search))
                q = q.Where(d => d.CustomerName.Contains(search) || d.BillNo.Contains(search) || d.CustomerCode.Contains(search));

            var debts = await q.OrderByDescending(d => d.BillDate).ToListAsync();
            var assignedReps = isRestricted && !string.IsNullOrWhiteSpace(currentUser?.SalesRepCode)
                ? SalesRepHelper.GetAssignedSalesReps(currentUser.SalesRepCode, allDbReps)
                : new List<string>();

            ViewBag.Search = search;
            ViewBag.SalesRep = selectedRepForUi;
            ViewBag.SalesReps = (isRestricted && assignedReps.Count > 0) ? assignedReps : allDbReps;
            ViewBag.IsRestricted = isRestricted;
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

            var allDbReps = await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            var (filterReps, selectedRepForUi, hasMultipleAssigned) = SalesRepHelper.ResolveFilter(
                isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);
            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;

            var debts = await _svc.GetDebtorsAsync(search, userAllowedRegion, userAllowedProvinces, effectiveRepParam, "BadDebt", null, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);
            var assignedReps = isRestricted && !string.IsNullOrWhiteSpace(currentUser?.SalesRepCode)
                ? SalesRepHelper.GetAssignedSalesReps(currentUser.SalesRepCode, allDbReps)
                : new List<string>();

            ViewBag.Search = search;
            ViewBag.SalesRep = selectedRepForUi;
            ViewBag.SalesReps = (isRestricted && assignedReps.Count > 0) ? assignedReps : allDbReps;
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
            var allDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var (filterReps, _, _) = SalesRepHelper.ResolveFilter(isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);
            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;

            var debts = await _svc.GetCancelledDebtsAsync(search, effectiveRepParam, 
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
            var allDbReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToListAsync();
            }) ?? new List<string>();

            var (filterReps, selectedRepForUi, _) = SalesRepHelper.ResolveFilter(isRestricted, currentUser?.SalesRepCode, salesRep, allDbReps);
            var effectiveRepParam = filterReps != null && filterReps.Count > 0 ? string.Join(",", filterReps) : null;

            var debts = await _svc.GetCancelledDebtsAsync(search, effectiveRepParam, 
                isRestricted ? currentUser?.AllowedRegion : null,
                isRestricted ? currentUser?.AllowedProvinces : null,
                isRestricted ? currentUser?.AllowedDistricts : null);
                
            ViewBag.Search = search; 
            ViewBag.SalesRep = selectedRepForUi; 
            ViewBag.Status = "cancelled"; 
            ViewBag.PrintedBy = currentUser?.FullName ?? User.Identity?.Name ?? "Admin"; 
            return View("PrintPdf", debts);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreCancelledBill(int debtId, string confirmPassword)
        {
            var (isPasswordOk, approverName) = await DebtStatusPermissionHelper.VerifyPaidBillApproverPasswordAsync(_db, confirmPassword);
            if (!isPasswordOk)
            {
                TempData["Error"] = "การกู้คืนจากบิลยกเลิกมาเป็นบิลค้างชำระปกติ ต้องใส่รหัสผ่านของผู้มีสิทธิ์ (คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร) หรือรหัสผ่านสำรอง 029030445Rd* เท่านั้น";
                return RedirectToAction("Cancelled");
            }

            var debt = await _db.OutstandingDebts.FirstOrDefaultAsync(d => d.Id == debtId);
            if (debt == null) return NotFound();

            if (debt.Status != DebtStatus.Cancelled)
            {
                TempData["Error"] = "บิลนี้ไม่ได้อยู่ในสถานะยกเลิก";
                return RedirectToAction("Cancelled");
            }

            debt.Status = DebtStatus.Outstanding;
            debt.CancelledDate = null;
            debt.CancelledBy = null;
            debt.CancelReason = null;
            debt.LastEditedDate = DateTime.Now;
            debt.LastEditedBy = approverName;

            _db.AuditLogs.Add(new AuditLog
            {
                Username = User.Identity?.Name ?? "system",
                Action = "RESTORE_CANCELLED_BILL",
                Detail = $"กู้คืนบิลยกเลิก {debt.BillNo} กลับเป็นบิลค้างชำระปกติ (อนุมัติโดย: {approverName})",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = $"กู้คืนบิล {debt.BillNo} กลับสู่ระบบเรียบร้อยแล้ว (อนุมัติโดย: {approverName})";
            return RedirectToAction("Cancelled");
        }
    }
}
