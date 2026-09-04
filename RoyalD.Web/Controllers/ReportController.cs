using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ReportService _svc;

        public ReportController(ReportService svc) => _svc = svc;

        public async Task<IActionResult> Sales()
        {
            var data = await _svc.GetAnnualPerformanceAsync();
            return View(data);
        }
        public async Task<IActionResult> ExportExcel(DateTime? from, DateTime? to)
        {
            var data = await _svc.GetSalesSummaryAsync(from, to);
            var bytes = await _svc.ExportToExcelAsync(data, from, to);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> WaitingGoods([FromServices] AppDbContext db)
        {
            var data = await db.PendingProducts
                .Include(p => p.OutstandingDebt)
                .Select(p => new {
                    BillNo = p.OutstandingDebt.BillNo,
                    BillDate = p.OutstandingDebt.BillDate,
                    CustomerCode = p.OutstandingDebt.CustomerCode,
                    CustomerName = p.OutstandingDebt.CustomerName,
                    SalesRep = p.OutstandingDebt.SalesRep,
                    UpdatedAt = p.OutstandingDebt.WaitingGoodsDate ?? p.OutstandingDebt.BillDate,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    Quantity = p.Quantity,
                    Note = p.OutstandingDebt.Note
                }).ToListAsync();
            return View(data);
        }
        
                public async Task<IActionResult> PaidHistory([FromServices] AppDbContext db, string? search)
        {
            var dateLimit = DateTime.Now.AddDays(-120);
            var q = db.SalesBills.AsNoTracking().Where(b => b.IsFullyPaid && b.ReceiptDate >= dateLimit);
            
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(b => b.BillNo.Contains(search) 
                              || b.CustomerName.Contains(search) 
                              || b.CustomerCode.Contains(search) 
                              || (b.ReceiptNo != null && b.ReceiptNo.Contains(search)));
            }
            
            var data = await q.OrderByDescending(b => b.ReceiptDate).ToListAsync();
            ViewBag.SearchTerm = search;
            return View(data);
        }

        public async Task<IActionResult> CancelledBills([FromServices] AppDbContext db)
        {
            var dateLimit = DateTime.Now.AddDays(-30);
            var data = await db.OutstandingDebts.AsNoTracking()
                .Where(d => d.Status == DebtStatus.Cancelled && d.CancelledDate >= dateLimit)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> ReturnNotes([FromServices] AppDbContext db)
        {
            var data = await db.OutstandingDebts.AsNoTracking()
                .Include(d => d.Attachments)
                .Where(d => d.Status == DebtStatus.ReturnIssued || d.Status == DebtStatus.ReturnPending)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> InstallmentDebtors([FromServices] AppDbContext db)
        {
            var data = await db.OutstandingDebts
                                .AsNoTracking()
                                .Where(d => (int)d.Status == 100)
                                .OrderBy(d => d.DueDate)
                                .ToListAsync();
            return View(data);
        }
    }
}

