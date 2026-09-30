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

        // Screen 3: Product Movement Details
        public async Task<IActionResult> ProductDetails(string? salesRep = null, string? month = null) 
        { 
            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");
            var cacheKey = $"prod_details_{salesRep}_{month}";
            var data = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _svc.GetProductDetailsReportAsync(salesRep, month);
            });
            return View(data);
        }

        public async Task<IActionResult> ProductDetailsAmount(string? salesRep = null, string? month = null) 
        { 
            if (!CheckPerm("salesreport")) return RedirectToAction("Index", "SalesBill");
            var cacheKey = $"prod_details_{salesRep}_{month}";
            var data = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _svc.GetProductDetailsReportAsync(salesRep, month);
            });
            return View(data);
        }

        // Screen 4: Customer Product Details
        public async Task<IActionResult> CustomerProduct(string? rep = null, string? month = null, DateTime? date = null, string? q = null, int page = 1, int pageSize = 100) 
        { 
            if (!CheckPerm("customerproduct")) return RedirectToAction("Index", "SalesBill");

            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin");
            var pos = User.FindFirst("Position")?.Value ?? "";
            bool isSalesRep = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย"));

            string? userRepCode = null;
            string? userFullName = null;
            string? username = null;
            if (isSalesRep)
            {
                userRepCode = User.FindFirst("SalesRepCode")?.Value;
                userFullName = User.FindFirst("FullName")?.Value;
                username = User.Identity?.Name;
            }

            var cacheKey = $"cust_prod_{rep}_{month}_{date?.ToString("yyyyMMdd")}_{q}_{userRepCode}_{username}";
            var vm = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _svc.GetCustomerProductReportAsync(rep, month, date, q, userRepCode, userFullName, username);
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
            ViewBag.SelectedRep = rep;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");
            ViewBag.SearchQ = q;

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
            string? month = null,
            string? searchCustomerCode = null,
            string? searchCustomerName = null,
            string? searchProductCode = null,
            int page = 1,
            int pageSize = 100) 
        { 
            if (!CheckPerm("customerpurchasesummary")) return RedirectToAction("Index", "SalesBill");

            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin");
            var pos = User.FindFirst("Position")?.Value ?? "";
            bool isSalesRepRole = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย"));

            string? userRepCode = null;
            string? userFullName = null;
            string? username = null;
            if (isSalesRepRole)
            {
                userRepCode = User.FindFirst("SalesRepCode")?.Value;
                userFullName = User.FindFirst("FullName")?.Value;
                username = User.Identity?.Name;
            }

            var cacheKey = $"cust_purch_{salesRep}_{month}_{userRepCode}_{username}_{searchCustomerCode}_{searchCustomerName}_{searchProductCode}";
            var vm = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45);
                return await _svc.GetCustomerPurchaseSummaryAsync(salesRep, month, userRepCode, userFullName, username, searchCustomerCode, searchCustomerName, searchProductCode);
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
            ViewBag.SearchCustomerCode = searchCustomerCode;
            ViewBag.SearchCustomerName = searchCustomerName;
            ViewBag.SearchProductCode = searchProductCode;

            return View(vm);
        }

        private bool CanDownload()
        {
            if (User.IsInRole("admin")) return true;
            var pos = User.FindFirst("Position")?.Value ?? "";
            bool isSalesRep = pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย");
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

                public async Task<IActionResult> ExportCustomerProductExcel(string? rep = null, string? month = null, DateTime? date = null, string? q = null)
        {
            if (!CanDownload()) return Forbid();
            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin");
            var pos = User.FindFirst("Position")?.Value ?? "";
            bool isSalesRep = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย"));
            string? userRepCode = isSalesRep ? User.FindFirst("SalesRepCode")?.Value : null;
            string? userFullName = isSalesRep ? User.FindFirst("FullName")?.Value : null;
            string? username = isSalesRep ? User.Identity?.Name : null;

            var data = await _svc.GetCustomerProductReportAsync(rep, month, date, q, userRepCode, userFullName, username);
            var bytes = await _svc.ExportCustomerProductExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CustomerProduct_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportProductDetailsExcel(string? salesRep = null, string? month = null)
        {
            if (!CanDownload()) return Forbid();
            var data = await _svc.GetProductDetailsReportAsync(salesRep, month);
            var bytes = await _svc.ExportProductDetailsExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_Products_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportCustomerProductCsv(string? rep = null, string? month = null, DateTime? date = null, string? q = null)
        {
            if (!CanDownload()) return Forbid();
            bool isAdmin = User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin");
            var pos = User.FindFirst("Position")?.Value ?? "";
            bool isSalesRep = !isAdmin && (pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย"));
            string? userRepCode = isSalesRep ? User.FindFirst("SalesRepCode")?.Value : null;
            string? userFullName = isSalesRep ? User.FindFirst("FullName")?.Value : null;
            string? username = isSalesRep ? User.Identity?.Name : null;

            var data = await _svc.GetCustomerProductReportAsync(rep, month, date, q, userRepCode, userFullName, username);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ลำดับ,เดือน,รหัสลูกค้า,ชื่อลูกค้า,รหัสสินค้า,ชื่อสินค้า,หน่วยสินค้า,ราคาต่อหน่วย,เครดิต(วัน),ชื่อผู้แทนขาย");
            int idx = 1;
            foreach (var item in data.Items)
            {
                var custName = (item.CustomerName ?? "").Replace("\"", "\"\"");
                var prodName = (item.ProductName ?? "").Replace("\"", "\"\"");
                var unit = (item.Unit ?? "").Replace("\"", "\"\"");
                sb.AppendLine($"{idx++},{item.Month},{item.CustomerCode},\"{custName}\",{item.ProductCode},\"{prodName}\",\"{unit}\",{item.Price},{item.Credit},{item.SalesRep}");
            }
            // Use UTF8 with BOM so Excel reads Thai correctly
            var utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[utf8Bom.Length + bytes.Length];
            System.Buffer.BlockCopy(utf8Bom, 0, result, 0, utf8Bom.Length);
            System.Buffer.BlockCopy(bytes, 0, result, utf8Bom.Length, bytes.Length);

            return File(result, "text/csv", $"Customer_Sales_Report_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}




