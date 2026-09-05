using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var allowedPages = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
            if (!User.IsInRole("admin") && !allowedPages.Contains("dashboard"))
                return RedirectToAction("Index", "SalesBill");

            var today = DateTime.Today;

            var cancelledCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Cancelled);
            ViewBag.TotalBills = (await _db.SalesBills.CountAsync()) - cancelledCount;

            // ดึงข้อมูลลูกหนี้ทั้งหมด (พร้อมซ่อมแซมข้อมูลจังหวัดและเครดิตที่หายไป)
            var nullProvinces = await _db.OutstandingDebts.Where(d => string.IsNullOrEmpty(d.Province) || string.IsNullOrEmpty(d.District)).ToListAsync();
            bool needsSave = false;
            foreach(var d in nullProvinces) {
                var sb = await _db.SalesBills.FirstOrDefaultAsync(s => s.BillNo == d.BillNo);
                if (sb != null) {
                    if (string.IsNullOrEmpty(d.Province)) d.Province = sb.Province;
                    if (string.IsNullOrEmpty(d.District)) d.District = sb.District;
                    if (d.Credit == 0 && sb.Credit > 0) d.Credit = sb.Credit;
                    needsSave = true;
                }
            }
            if (needsSave) await _db.SaveChangesAsync();

            // แทรกบิลหนี้สูญ 2 ใบที่ตกหล่น (R100150, R108995) แบบอัตโนมัติหากยังไม่มี
            bool addedBadDebt = false;
            var missingBills = new[] { "R100150", "R108995" };
            foreach (var b in missingBills)
            {
                if (!await _db.OutstandingDebts.AnyAsync(d => d.BillNo == b))
                {
                    _db.SalesBills.Add(new SalesBill
                    {
                        BillNo = b,
                        CustomerCode = "370018",
                        CustomerName = "ศรีอำนวย",
                        Province = "อำนาจเจริญ",
                        District = "เมือง",
                        SalesRep = "วีรนุช",
                        BillDate = b == "R100150" ? new DateTime(2024, 11, 4) : new DateTime(2025, 3, 20),
                        Credit = 45,
                        TotalAmount = 11340,
                        SourceMonth = b == "R100150" ? "2024-11" : "2025-03",
                        IsFullyPaid = false
                    });
                    
                    _db.OutstandingDebts.Add(new OutstandingDebt
                    {
                        BillNo = b,
                        CustomerCode = "370018",
                        CustomerName = "ศรีอำนวย",
                        Province = "อำนาจเจริญ",
                        District = "เมือง",
                        SalesRep = "วีรนุช",
                        BillDate = b == "R100150" ? new DateTime(2024, 11, 4) : new DateTime(2025, 3, 20),
                        DueDate = b == "R100150" ? new DateTime(2024, 12, 19) : new DateTime(2025, 5, 4),
                        Credit = 45,
                        OriginalAmount = 11340,
                        RemainingAmount = 11340,
                        Status = DebtStatus.Outstanding
                    });
                    addedBadDebt = true;
                }
            }
            if (addedBadDebt) await _db.SaveChangesAsync();

            var allDebts = await _db.OutstandingDebts
                .Where(d => d.Status != DebtStatus.Cancelled)
                .Select(d => new { d.Province, d.District, d.BillDate, d.DueDate, d.Credit, d.OriginalAmount, d.RemainingAmount, d.CustomerCode, d.Status })
                .ToListAsync();

            var summary = new DashboardSummary
            {
                TotalDebtors = allDebts.Where(d => d.RemainingAmount > 0).Select(d => d.CustomerCode).Distinct().Count(),
                TotalAmount = allDebts.Sum(d => d.OriginalAmount)
            };

            decimal totalAllOutstanding = 0;
            decimal lessThan120AmountAll = 0;
            int lessThan120CountAll = 0;
            decimal over120AmountAll = 0;
            int over120CountAll = 0;

            foreach (var d in allDebts)
            {
                string prov = (d.Province ?? "") + " " + (d.District ?? "");
                bool isBkk = prov.Contains("กรุงเทพ") || prov.Contains("กทม") || prov.ToLower().Contains("bangkok");

                // คำนวณภาพรวมทุกจังหวัดทั่วประเทศ
                if (d.RemainingAmount > 0 && d.Status != DebtStatus.PaidCash && d.Status != DebtStatus.PaidTransfer && d.Status != DebtStatus.PaidCheck)
                {
                    totalAllOutstanding += d.RemainingAmount;
                    var dueByCredit = d.BillDate.AddDays(d.Credit);
                    int aging = (int)(today - dueByCredit).TotalDays;

                    if (aging > 120)
                    {
                        over120AmountAll += d.RemainingAmount;
                        over120CountAll++;
                    }
                    else
                    {
                        lessThan120AmountAll += d.RemainingAmount;
                        lessThan120CountAll++;
                    }
                }

                // สำหรับกรุงเทพฯ: คิดเฉพาะบิลที่เป็นเครดิตเงินสดหรือ 7 วัน
                if (isBkk)
                {
                    if (d.Credit != 0 && d.Credit != 7)
                        continue;

                    var region = summary.Bangkok;
                    region.BillCount++;
                    region.TotalAmount += d.OriginalAmount;

                    decimal paidAmount = d.OriginalAmount - d.RemainingAmount;
                    if (paidAmount > 0 || d.Status == DebtStatus.PaidCash || d.Status == DebtStatus.PaidTransfer || d.Status == DebtStatus.PaidCheck)
                    {
                        if (paidAmount <= 0 && d.RemainingAmount == 0) paidAmount = d.OriginalAmount;
                        if (paidAmount <= 0) paidAmount = d.OriginalAmount;
                        region.Paid.BillCount++;
                        region.Paid.TotalAmount += paidAmount;
                    }

                    if (d.RemainingAmount > 0 && d.Status != DebtStatus.PaidCash && d.Status != DebtStatus.PaidTransfer && d.Status != DebtStatus.PaidCheck)
                    {
                        var dueByCredit = d.BillDate.AddDays(d.Credit);
                        int aging = (int)(today - dueByCredit).TotalDays;

                        region.OutstandingTotal.BillCount++;
                        region.OutstandingTotal.TotalAmount += d.RemainingAmount;

                        if (aging > 120)
                        {
                            region.Over120Days.BillCount++;
                            region.Over120Days.TotalAmount += d.RemainingAmount;
                        }
                        else
                        {
                            region.LessThan120Days.BillCount++;
                            region.LessThan120Days.TotalAmount += d.RemainingAmount;
                        }
                    }
                }
                else
                {
                    var region = summary.Upcountry;
                    region.BillCount++;
                    region.TotalAmount += d.OriginalAmount;

                    decimal paidAmount = d.OriginalAmount - d.RemainingAmount;
                    if (paidAmount > 0 || d.Status == DebtStatus.PaidCash || d.Status == DebtStatus.PaidTransfer || d.Status == DebtStatus.PaidCheck)
                    {
                        if (paidAmount <= 0 && d.RemainingAmount == 0) paidAmount = d.OriginalAmount;
                        if (paidAmount <= 0) paidAmount = d.OriginalAmount;
                        region.Paid.BillCount++;
                        region.Paid.TotalAmount += paidAmount;
                    }

                    if (d.RemainingAmount > 0 && d.Status != DebtStatus.PaidCash && d.Status != DebtStatus.PaidTransfer && d.Status != DebtStatus.PaidCheck)
                    {
                        var dueByCredit = d.BillDate.AddDays(d.Credit);
                        int aging = (int)(today - dueByCredit).TotalDays;

                        region.OutstandingTotal.BillCount++;
                        region.OutstandingTotal.TotalAmount += d.RemainingAmount;

                        if (aging > 120)
                        {
                            region.Over120Days.BillCount++;
                            region.Over120Days.TotalAmount += d.RemainingAmount;
                        }
                        else
                        {
                            region.LessThan120Days.BillCount++;
                            region.LessThan120Days.TotalAmount += d.RemainingAmount;
                        }
                    }
                }
            }

            ViewBag.Summary = summary;
            ViewBag.TotalOutstanding = totalAllOutstanding;
            ViewBag.OutstandingLessThan120Amount = lessThan120AmountAll;
            ViewBag.OutstandingLessThan120Count = lessThan120CountAll;
            ViewBag.OutstandingOver120Amount = over120AmountAll;
            ViewBag.OutstandingOver120Count = over120CountAll;
            ViewBag.TotalDebtors = summary.TotalDebtors;

            // Status breakdown for donut chart
            ViewBag.OverdueCount = allDebts.Count(d => d.BillDate.AddDays(d.Credit) < today && d.RemainingAmount > 0);
            ViewBag.InstallmentCount = allDebts.Count(d => d.Status == DebtStatus.Installment);
            ViewBag.PostponedCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Postponed);
            ViewBag.BadDebtCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.BadDebt);

            int currentYear = today.Year;

            // Monthly Sales
            var monthlySales = await _db.SalesBills
                .Where(b => b.BillDate.Year == currentYear)
                .GroupBy(b => b.BillDate.Month)
                .Select(g => new { M = g.Key, V = g.Sum(x => x.TotalAmount) })
                .ToDictionaryAsync(x => x.M, x => x.V);

            // Monthly Collections
            var monthlyPaid = await _db.PaymentRecords
                .Where(p => p.PaidDate.Year == currentYear)
                .GroupBy(p => p.PaidDate.Month)
                .Select(g => new { M = g.Key, V = g.Sum(x => x.PaidAmount) })
                .ToDictionaryAsync(x => x.M, x => x.V);

            // Monthly Overdue
            var monthlyOverdue = await _db.OutstandingDebts
                .Where(d => d.BillDate.Year == currentYear && d.RemainingAmount > 0)
                .GroupBy(d => d.BillDate.Month)
                .Select(g => new { M = g.Key, V = g.Sum(x => x.RemainingAmount) })
                .ToDictionaryAsync(x => x.M, x => x.V);

            var chartLabels = new List<string>();
            var chartValues = new List<decimal>(); // Sales
            var paidValues = new List<decimal>(); // Collections
            var overdueValues = new List<decimal>(); // Overdue

            for (int i = 1; i <= 12; i++)
            {
                chartLabels.Add(new DateTime(currentYear, i, 1).ToString("MMM yy", new System.Globalization.CultureInfo("th-TH")));
                chartValues.Add(monthlySales.ContainsKey(i) ? monthlySales[i] : 0);
                paidValues.Add(monthlyPaid.ContainsKey(i) ? monthlyPaid[i] : 0);
                overdueValues.Add(monthlyOverdue.ContainsKey(i) ? monthlyOverdue[i] : 0);
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartValues = chartValues;
            ViewBag.PaidValues = paidValues;
            ViewBag.OverdueValues = overdueValues;

            // Recent debts — เรียงตาม due date ที่ใกล้หรือเกินกำหนด
            var recentDebts = await _db.OutstandingDebts
                .Where(d => d.Status == DebtStatus.Outstanding)
                .OrderBy(d => d.DueDate)
                .Take(10)
                .ToListAsync();

            return View(recentDebts);
        }
    }
}
