using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class SalesReportController : Controller
    {
        private readonly ReportService _svc;

        public SalesReportController(ReportService svc) => _svc = svc;

        // Screen 1: Pivot Matrix Summary & Interactive Rep Tabs (Default)
        public async Task<IActionResult> Index()
        {
            var data = await _svc.GetAnnualPerformanceAsync();
            return View("Summary", data);
        }

        public async Task<IActionResult> Summary()
        {
            var data = await _svc.GetAnnualPerformanceAsync();
            return View(data);
        }

        // Screen 2: Annual Performance Charts
        public async Task<IActionResult> Charts()
        {
            var data = await _svc.GetAnnualPerformanceAsync();
            return View(data);
        }

        // Screen 3: Product Movement Details
        public async Task<IActionResult> ProductDetails(string? salesRep = null, string? month = null)
        {
            var data = await _svc.GetProductDetailsReportAsync(salesRep, month);
            return View(data);
        }

        // Screen 4: Customer Product Details
        public async Task<IActionResult> CustomerProduct(string? rep = null, string? month = null, DateTime? date = null)
        {
            var vm = await _svc.GetCustomerProductReportAsync(rep, month, date);
            return View(vm);
        }

        // Export Actions
        public async Task<IActionResult> ExportSummaryExcel()
        {
            var data = await _svc.GetSalesSummaryMatrixAsync();
            var bytes = await _svc.ExportMatrixExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_Matrix_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportProductDetailsExcel(string? salesRep = null, string? month = null)
        {
            var data = await _svc.GetProductDetailsReportAsync(salesRep, month);
            var bytes = await _svc.ExportProductDetailsExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_Products_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}