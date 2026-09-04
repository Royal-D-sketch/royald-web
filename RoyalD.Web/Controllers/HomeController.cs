using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

namespace RoyalD.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            var today = DateTime.Today;

            // 1. Calculate AR Summary
            var outstandingDebts = await _db.OutstandingDebts.ToListAsync();
            var totalOutstanding = outstandingDebts
                .Where(d => d.Status == DebtStatus.Outstanding || d.Status == DebtStatus.Installment)
                .Sum(d => d.RemainingAmount);

            var overdue120Amount = outstandingDebts
                .Where(d => (d.Status == DebtStatus.Outstanding || d.Status == DebtStatus.Installment) 
                         && (today - d.BillDate.AddDays(d.Credit)).TotalDays > 120)
                .Sum(d => d.RemainingAmount);

            var waitingGoodsCount = outstandingDebts
                .Count(d => d.Status == DebtStatus.WaitingGoods);

            ViewBag.TotalOutstanding = totalOutstanding;
            ViewBag.Overdue120Amount = overdue120Amount;
            ViewBag.WaitingGoodsCount = waitingGoodsCount;

            // 2. Sales Rep Stats
            var cancelledBillNos = outstandingDebts.Where(d => d.Status == DebtStatus.Cancelled).Select(d => d.BillNo).ToHashSet();
            var salesBills = (await _db.SalesBills.ToListAsync()).Where(s => !cancelledBillNos.Contains(s.BillNo)).ToList();
            var paymentRecords = await _db.PaymentRecords.Include(p => p.OutstandingDebt).ToListAsync();

            var repNames = salesBills.Select(s => s.SalesRep).Where(r => !string.IsNullOrEmpty(r)).Distinct().ToList();
            var repStats = new List<SalesRepStatsViewModel>();

            foreach (var rep in repNames)
            {
                var repBills = salesBills.Where(s => s.SalesRep == rep).ToList();
                decimal totalSales = repBills.Sum(s => s.TotalAmount);

                // TotalCollected (Sum of PaidAmount from PaymentRecords OR TotalAmount of fully paid bills)
                var fullyPaidBillAmount = repBills.Where(b => b.IsFullyPaid).Sum(b => b.TotalAmount);
                var paidFromRecords = paymentRecords
                    .Where(p => p.OutstandingDebt != null && p.OutstandingDebt.SalesRep == rep && !repBills.Any(b => b.BillNo == p.OutstandingDebt.BillNo && b.IsFullyPaid))
                    .Sum(p => p.PaidAmount);

                decimal totalCollected = fullyPaidBillAmount + paidFromRecords;
                decimal outstanding = totalSales - totalCollected;
                if (outstanding < 0) outstanding = 0;

                repStats.Add(new SalesRepStatsViewModel
                {
                    SalesRepName = rep,
                    TotalSales = totalSales,
                    TotalCollected = totalCollected,
                    Outstanding = outstanding
                });
            }

            ViewBag.RepStats = repStats;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
