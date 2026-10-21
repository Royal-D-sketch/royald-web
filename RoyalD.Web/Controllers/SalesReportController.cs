using System.Drawing;

using RoyalD.Web.Models;

using OfficeOpenXml.Style;

using OfficeOpenXml;

using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Caching.Memory;

using RoyalD.Web.Services;



namespace RoyalD.Web.Controllers

{

    [Authorize]

    public class SalesReportController : Controller

    {

        private readonly ReportService _svc;

        private readonly IMemoryCache _cache;



        public SalesReportController(ReportService svc, IMemoryCache cache)

        {

            _svc = svc;

            _cache = cache;

        }



        // Screen 1: Pivot Matrix Summary & Interactive Rep Tabs (Default)

        private bool CheckPerm(string p) 

        { 

            if (User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin")) return true; 

            var a = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(x => x.Trim().ToLower()) ?? Array.Empty<string>(); 

            if (a.Contains(p.ToLower())) return true; 



            if (p.Equals("customerproduct", StringComparison.OrdinalIgnoreCase))

            {

                var pos = User.FindFirst("Position")?.Value ?? "";

                if (pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย") || a.Contains("salesbill"))

                {

                    return true;

                }

            }

            return false; 

        } 



        private Task<AnnualPerformanceViewModel> GetCachedAnnualPerformanceAsync()

        {

            return _cache.GetOrCreateAsync("annual_performance_cache", async entry =>

            {

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);

                return await _svc.GetAnnualPerformanceAsync();

            })!;

        }



        public async Task<IActionResult> Index() { 

            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");

            var data = await GetCachedAnnualPerformanceAsync();

            return View("Summary", data);

        }



        public async Task<IActionResult> Summary() { 

            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");

            var data = await GetCachedAnnualPerformanceAsync();

            return View(data);

        }



        // Screen 2: Annual Performance Charts

        public async Task<IActionResult> Charts() { 

            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");

            var data = await GetCachedAnnualPerformanceAsync();

            return View(data);

        }



        [HttpGet]

        public async Task<IActionResult> GetChartCustomRange(DateTime startDate, DateTime endDate)

        {

            if (!CheckPerm("salesreport")) return Forbid();

            var items = await _svc.GetCustomRangeReportAsync(startDate, endDate);

            return Json(new {

                startDate = startDate.ToString("yyyy-MM-dd"),

                endDate = endDate.ToString("yyyy-MM-dd"),

                items

            });

        }



        // Screen 3: Product Movement Details

        public async Task<IActionResult> ProductDetails(string? salesRep = null, DateTime? startDate = null, DateTime? endDate = null, int? credit = null, string? month = null) 

        { 

            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");

            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin");

            var pos = User.FindFirst("Position")?.Value ?? "";

            bool isSalesRepRole = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย") || pos.Equals("sale", StringComparison.OrdinalIgnoreCase) || pos.Equals("Supervisor", StringComparison.OrdinalIgnoreCase));

            string? userRepCode = User.FindFirst("SalesRepCode")?.Value;

            string? userFullName = User.FindFirst("FullName")?.Value;

            string? username = User.Identity?.Name;

            bool isRestricted = isSalesRepRole || !string.IsNullOrWhiteSpace(userRepCode);



            var cacheKey = $"prod_details_{salesRep}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{credit}_{month}_{userRepCode}_{username}_{isRestricted}";

            var data = await _cache.GetOrCreateAsync(cacheKey, async entry =>

            {

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);

                return await _svc.GetProductDetailsReportAsync(salesRep, startDate, endDate, credit, userRepCode, userFullName, username, isRestricted, month);

            });

            return View(data);

        }



        public async Task<IActionResult> ProductDetailsAmount(string? salesRep = null, DateTime? startDate = null, DateTime? endDate = null, int? credit = null, string? month = null) 

        { 

            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");

            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin");

            var pos = User.FindFirst("Position")?.Value ?? "";

            bool isSalesRepRole = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย") || pos.Equals("sale", StringComparison.OrdinalIgnoreCase) || pos.Equals("Supervisor", StringComparison.OrdinalIgnoreCase));

            string? userRepCode = User.FindFirst("SalesRepCode")?.Value;

            string? userFullName = User.FindFirst("FullName")?.Value;

            string? username = User.Identity?.Name;

            bool isRestricted = isSalesRepRole || !string.IsNullOrWhiteSpace(userRepCode);



            var cacheKey = $"prod_details_{salesRep}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{credit}_{month}_{userRepCode}_{username}_{isRestricted}";

            var data = await _cache.GetOrCreateAsync(cacheKey, async entry =>

            {

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);

                return await _svc.GetProductDetailsReportAsync(salesRep, startDate, endDate, credit, userRepCode, userFullName, username, isRestricted, month);

            });

            return View(data);

        }



        // Screen 4: Customer Product Details
        public async Task<IActionResult> CustomerProduct(
            string? rep = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            int? credit = null, 
            string? q = null, 
            string? month = null, 
            DateTime? date = null, 
            string? region = null,
            string? province = null,
            string? district = null,
            int page = 1, 
            int pageSize = 100) 
        { 
            if (!CheckPerm("customerproduct")) return RedirectToAction("Index", "SalesBill");

            var currentUser = await _svc.Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin") || (User.Identity?.Name?.ToLower() == "art");
            bool isRestricted = !isAdmin;
            string? userRepCode = isRestricted ? (currentUser?.SalesRepCode ?? User.FindFirst("SalesRepCode")?.Value) : null;
            string? userFullName = currentUser?.FullName ?? User.FindFirst("FullName")?.Value;
            string? username = User.Identity?.Name;

            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : (isRestricted ? User.FindFirst("AllowedRegion")?.Value : null);
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : (isRestricted ? User.FindFirst("AllowedProvinces")?.Value : null);
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : (isRestricted ? User.FindFirst("AllowedDistricts")?.Value : null);

            var cacheKey = $"cust_prod_{rep}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{credit}_{month}_{date:yyyyMMdd}_{q}_{userRepCode}_{username}_{region}_{province}_{district}_{userAllowedRegion}_{userAllowedProvinces}_{userAllowedDistricts}";
            var vm = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _svc.GetCustomerProductReportAsync(
                    rep, startDate, endDate, credit, q, userRepCode, userFullName, username, month, date,
                    region, province, district, userAllowedRegion, userAllowedProvinces, userAllowedDistricts, isRestricted);
            }) ?? new CustomerProductViewModel();

            int totalRecords = vm.Items?.Count ?? 0;
            int effectivePageSize = pageSize > 0 ? pageSize : 100;
            int totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var pagedItems = vm.Items != null
                ? vm.Items.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList()
                : new List<CustomerProductItem>();

            ViewBag.Page = page;
            ViewBag.PageSize = effectivePageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalPages;
            ViewBag.PagedItems = pagedItems;

            ViewBag.SelectedRep = vm.SelectedRep;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Credit = credit;
            ViewBag.SearchQ = q;
            ViewBag.SelectedRegion = vm.SelectedRegion;
            ViewBag.SelectedProvince = vm.SelectedProvince;
            ViewBag.SelectedDistrict = vm.SelectedDistrict;

            return View(vm);
        }



        // Screen 5: Compare Sales Reps

        public async Task<IActionResult> Compare() 

        { 

            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");

            var data = await GetCachedAnnualPerformanceAsync();

            return View(data);

        }



        public async Task<IActionResult> CustomerPurchaseSummary(

            string? salesRep = null, 

            DateTime? startDate = null,

            DateTime? endDate = null,

            int? credit = null,

            string? searchCustomerCode = null,

            string? searchCustomerName = null,

            string? searchProductCode = null,

            string? month = null,

            int page = 1,

            int pageSize = 100) 

        { 

            if (!CheckPerm("customerpurchasesummary")) return RedirectToAction("Index", "SalesBill");



            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin");

            var pos = User.FindFirst("Position")?.Value ?? "";

            bool isSalesRepRole = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย") || pos.Equals("sale", StringComparison.OrdinalIgnoreCase) || pos.Equals("Supervisor", StringComparison.OrdinalIgnoreCase));



            string? userRepCode = null;

            string? userFullName = null;

            string? username = null;

            if (isSalesRepRole)

            {

                userRepCode = User.FindFirst("SalesRepCode")?.Value;

                userFullName = User.FindFirst("FullName")?.Value;

                username = User.Identity?.Name;

            }



            var cacheKey = $"cust_purch_{salesRep}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{credit}_{month}_{userRepCode}_{username}_{searchCustomerCode}_{searchCustomerName}_{searchProductCode}";

            var vm = await _cache.GetOrCreateAsync(cacheKey, async entry =>

            {

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);

                return await _svc.GetCustomerPurchaseSummaryAsync(salesRep, startDate, endDate, credit, userRepCode, userFullName, username, searchCustomerCode, searchCustomerName, searchProductCode, month);

            }) ?? new CustomerPurchaseSummaryViewModel();



            int totalRecords = vm.Rows?.Count ?? 0;

            int effectivePageSize = pageSize > 0 ? pageSize : 100;

            int totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);

            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));



            var pagedRows = vm.Rows != null

                ? vm.Rows.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList()

                : new List<CustomerPurchaseSummaryRow>();



            ViewBag.Page = page;

            ViewBag.PageSize = effectivePageSize;

            ViewBag.TotalRecords = totalRecords;

            ViewBag.TotalPages = totalPages;

            ViewBag.PagedRows = pagedRows;

            ViewBag.SelectedSalesRep = salesRep;

            ViewBag.SelectedMonth = month;

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");

            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            ViewBag.Credit = credit;

            ViewBag.SearchCustomerCode = searchCustomerCode;

            ViewBag.SearchCustomerName = searchCustomerName;

            ViewBag.SearchProductCode = searchProductCode;



            return View(vm);

        }



        private bool CanDownload()
        {
            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin") || (User.Identity?.Name?.ToLower() == "art");
            if (isAdmin) return true;
            var pos = User.FindFirst("Position")?.Value ?? "";
            var repCode = User.FindFirst("SalesRepCode")?.Value;
            bool isSalesRep = pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย") || !string.IsNullOrWhiteSpace(repCode);
            if (isSalesRep) return false;
            return User.FindFirst("CanDownload")?.Value == "true";
        }



        // Export Actions

        public async Task<IActionResult> ExportSummaryExcel()

        {

            if (!CanDownload()) return Forbid();

            var data = await _svc.GetSalesSummaryMatrixAsync();

            var bytes = await _svc.ExportMatrixExcelAsync(data);

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                $"SalesReport_Matrix_{DateTime.Now:yyyyMMdd}.xlsx");

        }



        public async Task<IActionResult> ExportCustomerProductExcel(
            string? rep = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            int? credit = null, 
            string? q = null, 
            string? month = null, 
            DateTime? date = null,
            string? region = null,
            string? province = null,
            string? district = null)
        {
            if (!CheckPerm("customerproduct")) return Forbid();
            if (!CanDownload()) return Forbid();

            var currentUser = await _svc.Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin") || (User.Identity?.Name?.ToLower() == "art");
            bool isRestricted = !isAdmin;
            string? userRepCode = isRestricted ? (currentUser?.SalesRepCode ?? User.FindFirst("SalesRepCode")?.Value) : null;
            string? userFullName = currentUser?.FullName ?? User.FindFirst("FullName")?.Value;
            string? username = User.Identity?.Name;

            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : (isRestricted ? User.FindFirst("AllowedRegion")?.Value : null);
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : (isRestricted ? User.FindFirst("AllowedProvinces")?.Value : null);
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : (isRestricted ? User.FindFirst("AllowedDistricts")?.Value : null);

            var data = await _svc.GetCustomerProductReportAsync(
                rep, startDate, endDate, credit, q, userRepCode, userFullName, username, month, date,
                region, province, district, userAllowedRegion, userAllowedProvinces, userAllowedDistricts, isRestricted);

            var bytes = await _svc.ExportCustomerProductExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CustomerProduct_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportCustomerProductCsv(
            string? rep = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            int? credit = null, 
            string? q = null, 
            string? month = null, 
            DateTime? date = null,
            string? region = null,
            string? province = null,
            string? district = null)
        {
            if (!CheckPerm("customerproduct")) return Forbid();
            if (!CanDownload()) return Forbid();

            var currentUser = await _svc.Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin") || (User.Identity?.Name?.ToLower() == "art");
            bool isRestricted = !isAdmin;
            string? userRepCode = isRestricted ? (currentUser?.SalesRepCode ?? User.FindFirst("SalesRepCode")?.Value) : null;
            string? userFullName = currentUser?.FullName ?? User.FindFirst("FullName")?.Value;
            string? username = User.Identity?.Name;

            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : (isRestricted ? User.FindFirst("AllowedRegion")?.Value : null);
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : (isRestricted ? User.FindFirst("AllowedProvinces")?.Value : null);
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : (isRestricted ? User.FindFirst("AllowedDistricts")?.Value : null);

            var data = await _svc.GetCustomerProductReportAsync(
                rep, startDate, endDate, credit, q, userRepCode, userFullName, username, month, date,
                region, province, district, userAllowedRegion, userAllowedProvinces, userAllowedDistricts, isRestricted);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ลำดับ,เดือน,รหัสลูกค้า,ชื่อลูกค้า,รหัสสินค้า : ชื่อสินค้า,หน่วยสินค้า,ราคาต่อหน่วย,เครดิต(วัน),ชื่อผู้แทนขาย");
            int idx = 1;
            foreach (var item in data.Items)
            {
                var row = $"{idx++},{item.Month},\"{item.CustomerCode}\",\"{item.CustomerName}\",\"{item.ProductCode} : {item.ProductName}\",\"{item.Unit}\",{item.Price},\"{((item.Credit == 0 || item.Credit == 7) ? "เงินสด" : item.Credit.ToString())}\",\"{item.SalesRep}\"";
                sb.AppendLine(row);
            }

            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var csvBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fullBytes = bom.Concat(csvBytes).ToArray();

            return File(fullBytes, "text/csv; charset=utf-8",
                $"CustomerProduct_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet]

        public async Task<IActionResult> ProductGroupSales(
            [FromServices] AppDbContext db,
            [FromServices] ProductGroupService groupService,
            string? salesRep = null,
            string? month25 = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? region = null,
            string? province = null,
            string? district = null)
        {
            var vm = await BuildProductGroupSalesVmAsync(db, groupService, salesRep, month25, startDate, endDate, region, province, district);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> PrintProductGroupSalesPdf(
            [FromServices] AppDbContext db,
            [FromServices] ProductGroupService groupService,
            string? salesRep = null,
            string? month25 = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? region = null,
            string? province = null,
            string? district = null)
        {
            if (!CanDownload()) return Forbid();
            var vm = await BuildProductGroupSalesVmAsync(db, groupService, salesRep, month25, startDate, endDate, region, province, district);
            ViewBag.PrintedBy = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";
            return View("PrintProductGroupSalesPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportProductGroupSalesExcel(
            [FromServices] AppDbContext db,
            [FromServices] ProductGroupService groupService,
            string? salesRep = null,
            string? month25 = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? region = null,
            string? province = null,
            string? district = null)
        {
            if (!CanDownload()) return Forbid();
            var vm = await BuildProductGroupSalesVmAsync(db, groupService, salesRep, month25, startDate, endDate, region, province, district);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();



            // -------------------------------------------------------------

            // Sheet 1: สรุปยอดขายและลดหนี้แยกกลุ่มสินค้า

            // -------------------------------------------------------------

            var ws1 = package.Workbook.Worksheets.Add("สรุปกลุ่มสินค้า");

            ws1.Cells.Style.Font.Name = "Sarabun";

            ws1.Cells.Style.Font.Size = 11;



            // Header info

            ws1.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";

            ws1.Cells["A1"].Style.Font.Size = 16;

            ws1.Cells["A1"].Style.Font.Bold = true;

            ws1.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(15, 118, 110));



            ws1.Cells["A2"].Value = $"รายงานผู้แทนขายแยกตามกลุ่มสินค้าและใบลดหนี้ (ผู้แทน: {(string.IsNullOrEmpty(vm.SelectedSalesRep) ? "ทุกคน" : vm.SelectedSalesRep)} | ช่วงวันที่: {vm.StartDate:dd/MM/yyyy} - {vm.EndDate:dd/MM/yyyy})";

            ws1.Cells["A2"].Style.Font.Size = 12;

            ws1.Cells["A2"].Style.Font.Bold = true;



            int row1 = 4;

            string[] headers1 = { "ลำดับ", "รหัสกลุ่ม", "ชื่อกลุ่มสินค้า", "จำนวนขาย", "ยอดเงินขาย (บาท)", "จำนวนคืน", "ยอดลดหนี้ (บาท)", "ยอดขายสุทธิ (บาท)" };

            for (int col = 0; col < headers1.Length; col++)

            {

                var cell = ws1.Cells[row1, col + 1];

                cell.Value = headers1[col];

                cell.Style.Font.Bold = true;

                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(13, 148, 136));

                cell.Style.Font.Color.SetColor(Color.White);

                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(15, 118, 110));

            }



            int seq1 = 1;

            foreach (var g in vm.Groups)

            {

                row1++;

                ws1.Cells[row1, 1].Value = seq1++;

                ws1.Cells[row1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[row1, 2].Value = g.GroupCode;

                ws1.Cells[row1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[row1, 3].Value = g.GroupName;

                ws1.Cells[row1, 4].Value = g.GroupSalesQty;

                ws1.Cells[row1, 4].Style.Numberformat.Format = "#,##0.00";

                ws1.Cells[row1, 5].Value = g.GroupSalesAmount;

                ws1.Cells[row1, 5].Style.Numberformat.Format = "#,##0.00";

                ws1.Cells[row1, 6].Value = g.GroupReturnQty;

                ws1.Cells[row1, 6].Style.Numberformat.Format = "#,##0.00";

                ws1.Cells[row1, 7].Value = g.GroupCreditAmount;

                ws1.Cells[row1, 7].Style.Numberformat.Format = "#,##0.00";

                ws1.Cells[row1, 8].Value = g.GroupNetSalesAmount;

                ws1.Cells[row1, 8].Style.Numberformat.Format = "#,##0.00";

                ws1.Cells[row1, 8].Style.Font.Bold = true;

            }



            // Summary Total Row

            row1++;

            ws1.Cells[row1, 1, row1, 3].Merge = true;

            ws1.Cells[row1, 1].Value = "รวมทั้งสิ้น";

            ws1.Cells[row1, 1].Style.Font.Bold = true;

            ws1.Cells[row1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws1.Cells[row1, 4].Value = vm.TotalSalesQty;

            ws1.Cells[row1, 4].Style.Numberformat.Format = "#,##0.00";

            ws1.Cells[row1, 5].Value = vm.TotalGrossSales;

            ws1.Cells[row1, 5].Style.Numberformat.Format = "#,##0.00";

            ws1.Cells[row1, 6].Value = vm.TotalReturnQty;

            ws1.Cells[row1, 6].Style.Numberformat.Format = "#,##0.00";

            ws1.Cells[row1, 7].Value = vm.TotalCreditNotes;

            ws1.Cells[row1, 7].Style.Numberformat.Format = "#,##0.00";

            ws1.Cells[row1, 8].Value = vm.TotalNetSales;

            ws1.Cells[row1, 8].Style.Numberformat.Format = "#,##0.00";

            ws1.Cells[row1, 1, row1, 8].Style.Font.Bold = true;

            ws1.Cells[row1, 1, row1, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;

            ws1.Cells[row1, 1, row1, 8].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 251, 241));



            ws1.Cells[4, 1, row1, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;

            ws1.Cells[4, 1, row1, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            ws1.Cells[4, 1, row1, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;

            ws1.Cells[4, 1, row1, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            ws1.Cells.AutoFitColumns();



            // -------------------------------------------------------------

            // Sheet 2: รายละเอียดสินค้าทุกรายการ

            // -------------------------------------------------------------

            var ws2 = package.Workbook.Worksheets.Add("รายละเอียดสินค้า");

            ws2.Cells.Style.Font.Name = "Sarabun";

            ws2.Cells.Style.Font.Size = 10;



            ws2.Cells["A1"].Value = "รายละเอียดสินค้าและกระทบยอดใบลดหนี้รายตัว";

            ws2.Cells["A1"].Style.Font.Size = 14;

            ws2.Cells["A1"].Style.Font.Bold = true;



            int row2 = 3;

            string[] headers2 = { "ลำดับ", "กลุ่มสินค้า", "รหัสสินค้า", "ชื่อสินค้า", "หน่วย", "จำนวนขาย", "ยอดเงินขาย (บาท)", "เลขที่ใบลดหนี้", "จำนวนคืน", "ยอดลดหนี้ (บาท)", "ยอดขายสุทธิ (บาท)" };

            for (int col = 0; col < headers2.Length; col++)

            {

                var cell = ws2.Cells[row2, col + 1];

                cell.Value = headers2[col];

                cell.Style.Font.Bold = true;

                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110));

                cell.Style.Font.Color.SetColor(Color.White);

                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            }



            int seq2 = 1;

            foreach (var g in vm.Groups)

            {

                foreach (var item in g.Items)

                {

                    row2++;

                    ws2.Cells[row2, 1].Value = seq2++;

                    ws2.Cells[row2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws2.Cells[row2, 2].Value = g.GroupName;

                    ws2.Cells[row2, 3].Value = item.ProductCode;

                    ws2.Cells[row2, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws2.Cells[row2, 4].Value = item.ProductName;

                    ws2.Cells[row2, 5].Value = item.Unit;

                    ws2.Cells[row2, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws2.Cells[row2, 6].Value = item.SalesQty;

                    ws2.Cells[row2, 6].Style.Numberformat.Format = "#,##0.00";

                    ws2.Cells[row2, 7].Value = item.SalesAmount;

                    ws2.Cells[row2, 7].Style.Numberformat.Format = "#,##0.00";

                    ws2.Cells[row2, 8].Value = item.CreditNoteNos.Any() ? string.Join(", ", item.CreditNoteNos) : "-";

                    ws2.Cells[row2, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws2.Cells[row2, 9].Value = item.ReturnQty;

                    ws2.Cells[row2, 9].Style.Numberformat.Format = "#,##0.00";

                    ws2.Cells[row2, 10].Value = item.CreditAmount;

                    ws2.Cells[row2, 10].Style.Numberformat.Format = "#,##0.00";

                    ws2.Cells[row2, 11].Value = item.NetSalesAmount;

                    ws2.Cells[row2, 11].Style.Numberformat.Format = "#,##0.00";

                    ws2.Cells[row2, 11].Style.Font.Bold = true;



                    if (item.HasCreditNote)

                    {

                        ws2.Cells[row2, 8, row2, 10].Style.Font.Color.SetColor(Color.FromArgb(185, 28, 28));

                        ws2.Cells[row2, 8].Style.Font.Bold = true;

                    }

                }

            }



            ws2.Cells[3, 1, row2, 11].Style.Border.Top.Style = ExcelBorderStyle.Thin;

            ws2.Cells[3, 1, row2, 11].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            ws2.Cells[3, 1, row2, 11].Style.Border.Left.Style = ExcelBorderStyle.Thin;

            ws2.Cells[3, 1, row2, 11].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            ws2.Cells.AutoFitColumns();



            var bytes = package.GetAsByteArray();

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                $"ProductGroupSales_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

        }



        private async Task<ProductGroupSalesReportViewModel> BuildProductGroupSalesVmAsync(
            AppDbContext db,
            ProductGroupService groupService,
            string? salesRep,
            string? month25,
            DateTime? startDate,
            DateTime? endDate,
            string? region = null,
            string? province = null,
            string? district = null)
        {
            var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin") || (User.Identity?.Name?.ToLower() == "art");
            var pos = currentUser?.Position ?? User.FindFirst("Position")?.Value ?? "";
            bool isSalesRepRole = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย") || pos.Equals("sale", StringComparison.OrdinalIgnoreCase) || pos.Equals("Supervisor", StringComparison.OrdinalIgnoreCase));
            string? userRepCode = currentUser?.SalesRepCode ?? User.FindFirst("SalesRepCode")?.Value;
            bool isRestricted = !isAdmin;

            string? userAllowedRegion = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedRegion) ? currentUser.AllowedRegion : (isRestricted ? User.FindFirst("AllowedRegion")?.Value : null);
            string? userAllowedProvinces = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedProvinces) ? currentUser.AllowedProvinces : (isRestricted ? User.FindFirst("AllowedProvinces")?.Value : null);
            string? userAllowedDistricts = isRestricted && !string.IsNullOrEmpty(currentUser?.AllowedDistricts) ? currentUser.AllowedDistricts : (isRestricted ? User.FindFirst("AllowedDistricts")?.Value : null);

            var areaScope = RegionHelper.ResolveAreaFilter(
                isRestricted, userAllowedRegion, userAllowedProvinces, region, province);

            var allReps = await _cache.GetOrCreateAsync("all_sales_reps_list", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await SalesRepHelper.GetAllKnownSalesRepsAsync(db, _cache);
            }) ?? new List<string>();

            var (filterReps, selectedRepForUi, isRestrictedRep) = SalesRepHelper.ResolveFilter(isRestricted, userRepCode, salesRep, allReps);

            // Generate Available 25th Months options
            string[] thaiMonthAbbr = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            var availableMonths25 = new List<Month25Option>();
            var today = DateTime.Today;
            for (int i = 0; i < 14; i++)
            {
                var refDate = today.AddMonths(-i);
                int y = refDate.Year;
                int m = refDate.Month;

                DateTime s25 = m == 1 ? new DateTime(y - 1, 12, 26) : new DateTime(y, m - 1, 26);
                DateTime e25 = new DateTime(y, m, 25);
                string key = $"{y}-{m:D2}";
                int thaiYearShort = (y > 2500 ? y : y + 543) % 100;
                string label = $"รอบตัด 25 {thaiMonthAbbr[m]} {thaiYearShort:D2} ({s25:dd/MM/yy} - {e25:dd/MM/yy})";

                availableMonths25.Add(new Month25Option
                {
                    Key = key,
                    Label = label,
                    StartDate = s25,
                    EndDate = e25
                });
            }

            DateTime finalStart;
            DateTime finalEnd;
            string selectedM25 = month25 ?? "";

            if (!string.IsNullOrEmpty(month25) && availableMonths25.Any(x => x.Key == month25))
            {
                var opt = availableMonths25.First(x => x.Key == month25);
                finalStart = opt.StartDate;
                finalEnd = opt.EndDate.AddDays(1).AddTicks(-1);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                finalStart = startDate.Value.Date;
                finalEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }
            else
            {
                var currentCycle = availableMonths25.First();
                selectedM25 = currentCycle.Key;
                finalStart = currentCycle.StartDate;
                finalEnd = currentCycle.EndDate.AddDays(1).AddTicks(-1);
            }

            // 1. Query Sales Bills
            var billsQuery = db.SalesBills.AsNoTracking()
                .Where(b => !b.IsCancelled && b.BillDate >= finalStart && b.BillDate <= finalEnd);

            if (areaScope.EffectiveProvinceVariants != null && areaScope.EffectiveProvinceVariants.Count > 0)
            {
                billsQuery = billsQuery.Where(b => areaScope.EffectiveProvinceVariants.Contains(b.Province));
            }

            if (!string.IsNullOrEmpty(userAllowedDistricts))
            {
                var rawDist = userAllowedDistricts.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim());
                var allowedDistList = RegionHelper.ExpandDistrictVariants(rawDist);
                if (allowedDistList != null && allowedDistList.Count > 0)
                {
                    billsQuery = billsQuery.Where(b => allowedDistList.Contains(b.District));
                }
            }

            if (!string.IsNullOrEmpty(district))
            {
                var distVariants = RegionHelper.ExpandDistrictVariants(new[] { district });
                billsQuery = billsQuery.Where(b => distVariants.Contains(b.District) || b.District.Contains(district));
            }

            if (filterReps != null && filterReps.Any())
            {
                billsQuery = billsQuery.Where(b => filterReps.Contains(b.SalesRep));
            }

            var billList = await billsQuery.Select(b => new { b.BillNo, b.SalesRep, b.BillDate }).ToListAsync();
            var billNos = billList.Select(b => b.BillNo).ToList();

            var itemsList = await db.SalesBillItems.AsNoTracking()
                .Where(i => billNos.Contains(i.BillNo))
                .Select(i => new { i.BillNo, i.ProductCode, i.ProductName, i.Unit, i.Qty, i.Price, i.Amount, i.Discount })
                .ToListAsync();

            // 2. Query Credit Notes
            var cnQuery = db.CreditNotes.AsNoTracking()
                .Include(c => c.Items)
                .Where(c => c.Status != "Cancelled" && c.CreditNoteDate >= finalStart && c.CreditNoteDate <= finalEnd);

            if (filterReps != null && filterReps.Any())
            {
                cnQuery = cnQuery.Where(c => filterReps.Contains(c.SalesRep) || filterReps.Contains(c.OriginalSalesRep));
            }
            if (areaScope.EffectiveProvinceVariants != null && areaScope.EffectiveProvinceVariants.Count > 0)
            {
                cnQuery = cnQuery.Where(c => billNos.Contains(c.OriginalBillNo) || (filterReps != null && (filterReps.Contains(c.SalesRep) || filterReps.Contains(c.OriginalSalesRep))));
            }

            var creditNoteList = await cnQuery.ToListAsync();

            var vm = new ProductGroupSalesReportViewModel
            {
                SalesReps = (isRestricted && !string.IsNullOrWhiteSpace(userRepCode)) ? SalesRepHelper.GetAssignedSalesReps(userRepCode, allReps) : allReps,
                SelectedSalesRep = selectedRepForUi ?? "",
                IsRestrictedRep = isRestricted,
                SelectedMonth25 = selectedM25,
                StartDate = finalStart.Date,
                EndDate = finalEnd.Date,
                AvailableMonths25 = availableMonths25,
                TotalCreditNoteCount = creditNoteList.Count,
                SelectedRegion = areaScope.SelectedRegion,
                SelectedProvince = areaScope.SelectedProvince,
                SelectedDistrict = district ?? "",
                AvailableRegions = areaScope.AvailableRegions,
                AvailableProvinces = areaScope.AvailableProvinces,
                HasRegionRestriction = areaScope.HasRegionRestriction,
                HasProvinceRestriction = areaScope.HasProvinceRestriction,
                RegionProvincesMap = areaScope.RegionProvincesMap,
                ProvinceDistricts = ThailandDistrictData.All
            };

            var allMasterGroups = groupService.GetAllGroups();

            var groupDict = new Dictionary<string, ProductGroupSummaryItem>();



            foreach (var g in allMasterGroups)

            {

                groupDict[g.Code] = new ProductGroupSummaryItem

                {

                    GroupCode = g.Code,

                    GroupName = g.Name

                };

            }





            var itemMap = new Dictionary<string, Dictionary<string, ProductItemReconcileRow>>();

            foreach (var gCode in groupDict.Keys)

            {

                itemMap[gCode] = new Dictionary<string, ProductItemReconcileRow>(StringComparer.OrdinalIgnoreCase);

            }



            // Sales items (ข้ามสินค้าที่เป็น Tester ไม่นำมาจัดกลุ่ม)

            foreach (var item in itemsList)

            {

                



                var gInfo = groupService.GetProductGroup(item.ProductCode, item.ProductName, item.Unit);

                if (gInfo == null || string.IsNullOrWhiteSpace(gInfo.GroupCode) || !groupDict.ContainsKey(gInfo.GroupCode))

                {

                    // ข้ามสินค้าที่ไม่มีกลุ่มสินค้า เอาเฉพาะรายการที่มีกลุ่มสินค้าเท่านั้น

                    continue;

                }



                



                string gCode = gInfo.GroupCode;



                var gItems = itemMap[gCode];

                string pCode = string.IsNullOrWhiteSpace(item.ProductCode) ? "-" : item.ProductCode.Trim();



                if (!gItems.TryGetValue(pCode, out var row))

                {

                    row = new ProductItemReconcileRow

                    {

                        ProductCode = pCode,

                        ProductName = !string.IsNullOrEmpty(gInfo.ProductName) ? gInfo.ProductName : item.ProductName,

                        Unit = !string.IsNullOrEmpty(gInfo.Unit) ? gInfo.Unit : (!string.IsNullOrEmpty(item.Unit) ? item.Unit : "ชิ้น")

                    };

                    gItems[pCode] = row;

                }



                // Tester ไม่ต้องใส่ราคา แต่ใส่แค่จำนวน, และหักส่วนลดการค้า (Discount %) ออกจากยอดเงิน
                bool isTester = groupService.IsTester(item.ProductCode, item.ProductName);
                decimal effectiveAmount = 0m;
                if (!isTester)
                {
                    decimal baseAmt = item.Amount > 0 ? item.Amount : Math.Round(item.Qty * item.Price, 2);
                    decimal discPct = item.Discount;
                    if (discPct > 0)
                    {
                        effectiveAmount = Math.Round(baseAmt * (1m - (discPct / 100m)), 2);
                    }
                    else
                    {
                        effectiveAmount = baseAmt;
                    }
                }

                row.SalesQty += item.Qty;
                row.SalesAmount += effectiveAmount;

            }



            // Credit Notes items (ข้ามสินค้าที่เป็น Tester ไม่นำมาจัดกลุ่ม)

            foreach (var cn in creditNoteList)

            {

                foreach (var cnItem in cn.Items)

                {

                    



                    var gInfo = groupService.GetProductGroup(cnItem.ProductCode, cnItem.ProductName, cnItem.Unit);

                    if (gInfo == null || string.IsNullOrWhiteSpace(gInfo.GroupCode) || !groupDict.ContainsKey(gInfo.GroupCode))

                    {

                        // ข้ามสินค้าที่ไม่มีกลุ่มสินค้า เอาเฉพาะรายการที่มีกลุ่มสินค้าเท่านั้น

                        continue;

                    }



                    



                    string gCode = gInfo.GroupCode;



                    var gItems = itemMap[gCode];

                    string pCode = string.IsNullOrWhiteSpace(cnItem.ProductCode) ? "-" : cnItem.ProductCode.Trim();



                    if (!gItems.TryGetValue(pCode, out var row))

                    {

                        row = new ProductItemReconcileRow

                        {

                            ProductCode = pCode,

                            ProductName = !string.IsNullOrEmpty(gInfo.ProductName) ? gInfo.ProductName : cnItem.ProductName,

                            Unit = !string.IsNullOrEmpty(gInfo.Unit) ? gInfo.Unit : (!string.IsNullOrEmpty(cnItem.Unit) ? cnItem.Unit : "ชิ้น")

                        };

                        gItems[pCode] = row;

                    }



                    bool isCnTester = groupService.IsTester(cnItem.ProductCode, cnItem.ProductName);
                    decimal cnEffectiveAmount = 0m;
                    if (!isCnTester)
                    {
                        // รายงาน CD เป็นยอดก่อน VAT
                        cnEffectiveAmount = cnItem.AmountBeforeVat > 0 ? cnItem.AmountBeforeVat : cnItem.TotalAmount;
                    }

                    row.ReturnQty += cnItem.TotalReturnQty;
                    row.CreditAmount += cnEffectiveAmount;

                    if (!string.IsNullOrEmpty(cn.CreditNoteNo) && !row.CreditNoteNos.Contains(cn.CreditNoteNo))

                    {

                        row.CreditNoteNos.Add(cn.CreditNoteNo);

                    }

                }

            }



            foreach (var kvp in groupDict.OrderBy(x => x.Key))

            {

                var gItem = kvp.Value;

                var gRows = itemMap[kvp.Key].Values

                    .Where(r => r.SalesQty != 0 || r.SalesAmount != 0 || r.ReturnQty != 0 || r.CreditAmount != 0)

                    .OrderByDescending(r => r.SalesAmount)

                    .ToList();



                if (gRows.Any())

                {

                    gItem.Items = gRows;

                    gItem.GroupSalesQty = gRows.Sum(r => r.SalesQty);

                    gItem.GroupSalesAmount = gRows.Sum(r => r.SalesAmount);

                    gItem.GroupReturnQty = gRows.Sum(r => r.ReturnQty);

                    gItem.GroupCreditAmount = gRows.Sum(r => r.CreditAmount);



                    vm.Groups.Add(gItem);

                }

            }



            vm.TotalGrossSales = vm.Groups.Sum(g => g.GroupSalesAmount);

            vm.TotalCreditNotes = vm.Groups.Sum(g => g.GroupCreditAmount);

            vm.TotalSalesQty = vm.Groups.Sum(g => g.GroupSalesQty);

            vm.TotalReturnQty = vm.Groups.Sum(g => g.GroupReturnQty);



            return vm;

        }



}

}









