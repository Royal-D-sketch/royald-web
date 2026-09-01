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
        public async Task<IActionResult> CustomerProduct(string? rep = null, string? month = null, DateTime? date = null, string? q = null)
        {
            var vm = await _svc.GetCustomerProductReportAsync(rep, month, date, q);
            return View(vm);
        }

        // Screen 5: Compare Sales Reps
        public async Task<IActionResult> Compare()
        {
            var data = await _svc.GetAnnualPerformanceAsync();
            return View(data);
        }
        // Export Actions
        public async Task<IActionResult> ExportSummaryExcel()
        {
            var data = await _svc.GetSalesSummaryMatrixAsync();
            var bytes = await _svc.ExportMatrixExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_Matrix_{DateTime.Now:yyyyMMdd}.xlsx");
        }

                public async Task<IActionResult> ExportCustomerProductExcel(string? rep = null, string? month = null, DateTime? date = null, string? q = null)
        {
            var data = await _svc.GetCustomerProductReportAsync(rep, month, date, q);
            var bytes = await _svc.ExportCustomerProductExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CustomerProduct_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportProductDetailsExcel(string? salesRep = null, string? month = null)
        {
            var data = await _svc.GetProductDetailsReportAsync(salesRep, month);
            var bytes = await _svc.ExportProductDetailsExcelAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_Products_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportCustomerProductCsv(string? rep = null, string? month = null, DateTime? date = null, string? q = null)
        {
            var data = await _svc.GetCustomerProductReportAsync(rep, month, date, q);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("เธฅเธณเธ”เธฑเธ,เน€เธ”เธทเธญเธ,เธฃเธซเธฑเธชเธฅเธนเธเธเนเธฒ,เธเธทเนเธญเธฅเธนเธเธเนเธฒ,เธฃเธซเธฑเธชเธชเธดเธเธเนเธฒ,เธเธทเนเธญเธชเธดเธเธเนเธฒ,เธฃเธฒเธเธฒเธ•เนเธญเธซเธเนเธงเธข,เธเธทเนเธญเธเธนเนเนเธ—เธเธเธฒเธข");
            int idx = 1;
            foreach (var item in data.Items)
            {
                var custName = (item.CustomerName ?? "").Replace("\"", "\"\"");
                var prodName = (item.ProductName ?? "").Replace("\"", "\"\"");
                sb.AppendLine($"{idx++},{item.Month},{item.CustomerCode},\"{custName}\",{item.ProductCode},\"{prodName}\",{item.Price},{item.Credit},{item.SalesRep}");
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

