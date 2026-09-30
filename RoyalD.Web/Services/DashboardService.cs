using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using RoyalD.Web.Controllers;

namespace RoyalD.Web.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // MODERN TRADE EXCLUSION LIST
        // ==========================================
        public static readonly HashSet<string> ModernTradeCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "101901", "10501", "103166", "110472", "110473",
            "102003", "102719",
            "1020379", "103373", "103374", "103375", "103376", "110239", "110547", "110571",
            "102445", "103468",
            "1020441", "1020453", "103281", "103282", "103283", "120349", "200335", "200336",
            "400182", "410135", "500224", "830063", "830064", "900186",
            "102540", "101298",
            "102275", "102276", "102293", "102309", "102556", "102648", "102716", "102736",
            "102751", "102767", "103085", "103424", "240089", "600089", "760064", "770033",
            "102307", "102265", "100783", "103329", "700084",
            "1020359", "1020363", "102660", "102661", "102662", "102663", "102664", "102665",
            "102666", "102667", "102668", "102669", "102670", "102671", "102672", "102673",
            "102674", "102740", "103058", "103073", "200263", "200265", "300320", "730168", "740156",
            "102955", "102956", "103065", "103205", "103290", "110463", "110499", "110540", "730185",
            "102677", "1020327", "102410", "103456", "102244", "200305", "730045"
        };

        public static readonly string[] ModernTradeNameKeywords = new[]
        {
            "ซีพี แอ็กซ์ตร้า", "ซีพี แอ๊กซ์ตร้า", "สยามแม็คโคร", "สยาม แม็คโคร", "แม็คโคร",
            "ซีพี ออลล์", "ซีพีออลล์",
            "เซ็นทรัล ฟู้ด",
            "บิ๊กซี", "big c",
            "สุวรรณชาด",
            "ปตท. บริหารธุรกิจค้าปลีก",
            "ปิโตรเลียมไทยคอร์ปอเรชั่น",
            "พี แอนด์ เอฟ อินทิเกรท",
            "อิออน (ไทยแลนด์)",
            "ซี.เจ.เอ็กซ์เพรส", "ซี.เจ. เอ็กซ์เพรส",
            "ฟู้ดแลนด์",
            "ฮาร์เบอร์ แลนด์", "ฮาร์เบอร์แลนด์",
            "บู๊ทส์ รีเทล",
            "เดอะมอลล์ กรุ๊ป",
            "วิลล่า มาร์เก็ท",
            "สห ลอว์สัน",
            "เซ็นทรัล วัตสัน",
            "ซูรูฮะ (ประเทศไทย)",
            "ฟาร์มาฮอฟ", "ฟาร์มมาฮอฟ", "pharmahof",
            "ไทยฟู้ดส์", "ฟาสซิโน", "ใบเมี่ยง"
        };

        public static bool IsModernTrade(string? customerCode, string? customerName = null)
        {
            if (!string.IsNullOrEmpty(customerCode) && ModernTradeCodes.Contains(customerCode.Trim()))
                return true;

            if (!string.IsNullOrEmpty(customerName))
            {
                var name = customerName.Trim();
                if (name.Contains("สาลี่สุพรรณ") || name.Contains("คุณเอกชัย") || name.Contains("ยากาแร็ต") ||
                    name.Contains("คลองสี่วา") || name.Contains("เอกชัยกอล์ฟ") || name.Contains("เพียวเคมม์") ||
                    name.Contains("ปรีชา  วิลล่า") || name.Contains("มหาชัยวิลล่า") || name.Contains("ท็อป ไฮเทค") ||
                    name.Contains("ท็อป พาร์ทเนอร์"))
                {
                    return false;
                }

                foreach (var kw in ModernTradeNameKeywords)
                {
                    if (name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        public async Task<(DashboardSummary summary, List<DashboardBillItem> drilldownBills, ComparisonBoardViewModel comparisonBoard)> GetDashboardDataAsync()
        {
            var today = DateTime.Today;
            var cutoffDate = new DateTime(2026, 9, 14);

            var allDebts = await _db.OutstandingDebts
                .AsNoTracking()
                .Where(d => d.Status != DebtStatus.Cancelled)
                .OrderByDescending(d => d.BillDate)
                .ToListAsync();

            var paidSalesBills = await _db.SalesBills
                .AsNoTracking()
                .Where(b => b.IsFullyPaid || (!string.IsNullOrEmpty(b.ReceiptNo) && b.ReceiptDate != null))
                .OrderByDescending(b => b.ReceiptDate ?? b.BillDate)
                .ToListAsync();

            var summary = new DashboardSummary
            {
                TotalDebtors = allDebts.Where(d => d.RemainingAmount > 0 && !string.IsNullOrEmpty(d.CustomerCode)).Select(d => d.CustomerCode).Distinct().Count(),
                TotalSalesAmount = allDebts.Sum(d => d.OriginalAmount),
                TotalAmount = allDebts.Sum(d => d.OriginalAmount)
            };

            var drilldownBills = new List<DashboardBillItem>();
            var comparisonBoard = new ComparisonBoardViewModel();
            var seenPaidBillNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var d in allDebts)
            {
                bool isPaid = d.RemainingAmount <= 0 || 
                              d.Status == DebtStatus.PaidCash || 
                              d.Status == DebtStatus.PaidTransfer || 
                              d.Status == DebtStatus.PaidCheck || 
                              d.FullyPaidDate.HasValue || 
                              (d.ReceiptDate.HasValue && !string.IsNullOrEmpty(d.ReceiptNo));

                var dueByCredit = d.BillDate.AddDays(d.Credit);
                int aging = (int)(today - dueByCredit).TotalDays;
                bool isBkkArea = RegionHelper.IsBkkAndVicinity(d.Province, d.District);

                string cat = "";
                string catName = "";
                string groupCode = "3";

                var bDateNorm = d.BillDate.Year > 2500 ? d.BillDate.AddYears(-543) : d.BillDate;

                if (isBkkArea)
                {
                    if (bDateNorm.Date >= cutoffDate.Date && d.Credit <= 7)
                    {
                        groupCode = "1";
                    }
                    else
                    {
                        groupCode = "2";
                    }
                }
                else
                {
                    groupCode = "3";
                }

                if (isPaid)
                {
                    cat = "collected";
                    catName = "ยอดเก็บเงินสำเร็จ";
                    decimal paidAmt = (d.OriginalAmount - d.RemainingAmount > 0) ? (d.OriginalAmount - d.RemainingAmount) : d.OriginalAmount;
                    if (paidAmt <= 0) paidAmt = d.OriginalAmount;

                    summary.Collected.BillCount++;
                    summary.Collected.TotalAmount += paidAmt;
                    seenPaidBillNos.Add(d.BillNo);
                }
                else
                {
                    summary.TotalOutstandingAmount += d.RemainingAmount;

                    if (groupCode == "1")
                    {
                        cat = "cash7";
                        catName = "เก็บสด 7 วัน (กทม.&ปริมณฑล)";
                        summary.Cash7Days.BillCount++;
                        summary.Cash7Days.TotalAmount += d.RemainingAmount;
                    }
                    else if (groupCode == "2")
                    {
                        cat = "cash10";
                        catName = "เงินสดรวมสายเวลา (กทม.&ปริมณฑล)";
                        summary.Cash10Days.BillCount++;
                        summary.Cash10Days.TotalAmount += d.RemainingAmount;
                    }
                    else
                    {
                        cat = "upcountry";
                        catName = "ต่างจังหวัดทั้งหมด";
                        summary.UpcountryDebts.BillCount++;
                        summary.UpcountryDebts.TotalAmount += d.RemainingAmount;
                    }

                    if (aging > 120 && !IsModernTrade(d.CustomerCode, d.CustomerName))
                    {
                        summary.Overdue120Days.BillCount++;
                        summary.Overdue120Days.TotalAmount += d.RemainingAmount;
                    }
                }

                var region = isBkkArea ? summary.Bangkok : summary.Upcountry;
                region.BillCount++;
                region.TotalAmount += d.OriginalAmount;

                if (isPaid)
                {
                    decimal paidAmt = (d.OriginalAmount - d.RemainingAmount > 0) ? (d.OriginalAmount - d.RemainingAmount) : d.OriginalAmount;
                    if (paidAmt <= 0) paidAmt = d.OriginalAmount;
                    region.Paid.BillCount++;
                    region.Paid.TotalAmount += paidAmt;
                }
                else
                {
                    region.OutstandingTotal.BillCount++;
                    region.OutstandingTotal.TotalAmount += d.RemainingAmount;
                    if (aging > 120 && !IsModernTrade(d.CustomerCode, d.CustomerName))
                    {
                        region.Over120Days.BillCount++;
                        region.Over120Days.TotalAmount += d.RemainingAmount;
                    }
                    else if (aging <= 120)
                    {
                        region.LessThan120Days.BillCount++;
                        region.LessThan120Days.TotalAmount += d.RemainingAmount;
                    }
                }

                var billItem = new DashboardBillItem
                {
                    BillNo = d.BillNo,
                    BillDate = d.BillDate,
                    SalesRep = d.SalesRep ?? "",
                    CustomerCode = d.CustomerCode ?? "",
                    CustomerName = d.CustomerName ?? "",
                    Province = d.Province ?? "",
                    District = d.District ?? "",
                    Amount = isPaid ? d.OriginalAmount : d.RemainingAmount,
                    Category = cat,
                    CategoryName = catName,
                    GroupCode = groupCode,
                    Credit = d.Credit,
                    DueDate = d.DueDate,
                    AgingDays = aging,
                    StatusName = isPaid ? "เก็บเงินสำเร็จ" : (aging > 0 ? $"เกินกำหนด {aging} วัน" : "ยังไม่ถึงกำหนด"),
                    IsPaid = isPaid,
                    IsBkk = isBkkArea,
                    IsModernTrade = IsModernTrade(d.CustomerCode, d.CustomerName)
                };

                drilldownBills.Add(billItem);
            }

            foreach (var sb in paidSalesBills)
            {
                if (!seenPaidBillNos.Contains(sb.BillNo))
                {
                    bool isBkkArea = RegionHelper.IsBkkAndVicinity(sb.Province, sb.District);
                    string groupCode = isBkkArea ? ((sb.BillDate.Date >= cutoffDate.Date && sb.Credit <= 7) ? "1" : "2") : "3";

                    summary.Collected.BillCount++;
                    summary.Collected.TotalAmount += sb.TotalAmount;
                    seenPaidBillNos.Add(sb.BillNo);

                    var region = isBkkArea ? summary.Bangkok : summary.Upcountry;
                    region.BillCount++;
                    region.TotalAmount += sb.TotalAmount;
                    region.Paid.BillCount++;
                    region.Paid.TotalAmount += sb.TotalAmount;

                    var item = new DashboardBillItem
                    {
                        BillNo = sb.BillNo,
                        BillDate = sb.BillDate,
                        SalesRep = sb.SalesRep ?? "",
                        CustomerCode = sb.CustomerCode ?? "",
                        CustomerName = sb.CustomerName ?? "",
                        Province = sb.Province ?? "",
                        District = sb.District ?? "",
                        Amount = sb.TotalAmount,
                        Category = "collected",
                        CategoryName = "ยอดเก็บเงินสำเร็จ",
                        GroupCode = groupCode,
                        Credit = sb.Credit,
                        DueDate = sb.BillDate.AddDays(sb.Credit),
                        AgingDays = 0,
                        StatusName = "เก็บเงินสำเร็จ",
                        IsPaid = true,
                        IsBkk = isBkkArea,
                        IsModernTrade = IsModernTrade(sb.CustomerCode, sb.CustomerName)
                    };
                    drilldownBills.Add(item);
                }
            }

            summary.TotalCollectedAmount = summary.Collected.TotalAmount;
            summary.AllDrilldownBills = drilldownBills;
            comparisonBoard.Summary = summary;
            comparisonBoard.AllBills = drilldownBills;

            return (summary, drilldownBills, comparisonBoard);
        }

        public async Task<DashboardController.DashboardPageData> LoadDashboardPageDataAsync()
        {
            var today = DateTime.Today;
            var cancelledCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Cancelled);
            int totalBills = (await _db.SalesBills.CountAsync()) - cancelledCount;

            var (summary, drilldownBills, comparisonBoard) = await GetDashboardDataAsync();

            int installmentCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Installment);
            int postponedCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Postponed);
            int badDebtCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.BadDebt);

            int currentYear = today.Year;

            var monthlySales = await _db.SalesBills
                .Where(b => b.BillDate.Year == currentYear)
                .GroupBy(b => b.BillDate.Month)
                .Select(g => new { M = g.Key, V = g.Sum(x => x.TotalAmount) })
                .ToDictionaryAsync(x => x.M, x => x.V);

            var monthlyPaid = await _db.PaymentRecords
                .Where(p => p.PaidDate.Year == currentYear)
                .GroupBy(p => p.PaidDate.Month)
                .Select(g => new { M = g.Key, V = g.Sum(x => x.PaidAmount) })
                .ToDictionaryAsync(x => x.M, x => x.V);

            var monthlyOverdue = await _db.OutstandingDebts
                .Where(d => d.BillDate.Year == currentYear && d.RemainingAmount > 0)
                .GroupBy(d => d.BillDate.Month)
                .Select(g => new { M = g.Key, V = g.Sum(x => x.RemainingAmount) })
                .ToDictionaryAsync(x => x.M, x => x.V);

            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();
            var paidValues = new List<decimal>();
            var overdueValues = new List<decimal>();

            for (int i = 1; i <= 12; i++)
            {
                chartLabels.Add(new DateTime(currentYear, i, 1).ToString("MMM yy", new System.Globalization.CultureInfo("th-TH")));
                chartValues.Add(monthlySales.ContainsKey(i) ? monthlySales[i] : 0);
                paidValues.Add(monthlyPaid.ContainsKey(i) ? monthlyPaid[i] : 0);
                overdueValues.Add(monthlyOverdue.ContainsKey(i) ? monthlyOverdue[i] : 0);
            }

            var recentDebts = await _db.OutstandingDebts
                .Where(d => d.Status == DebtStatus.Outstanding)
                .OrderBy(d => d.DueDate)
                .Take(10)
                .ToListAsync();

            var latestBillDate = await _db.SalesBills.OrderByDescending(b => b.BillDate).Select(b => (DateTime?)b.BillDate).FirstOrDefaultAsync();
            var latestDebtorDate = await _db.OutstandingDebts.OrderByDescending(d => d.BillDate).Select(d => (DateTime?)d.BillDate).FirstOrDefaultAsync();
            var latestReceiptDate = await _db.SalesBills.Where(b => b.ReceiptDate != null).OrderByDescending(b => b.ReceiptDate).Select(b => (DateTime?)b.ReceiptDate).FirstOrDefaultAsync()
                                  ?? await _db.OutstandingDebts.Where(d => d.ReceiptDate != null).OrderByDescending(d => d.ReceiptDate).Select(d => (DateTime?)d.ReceiptDate).FirstOrDefaultAsync();

            return new DashboardController.DashboardPageData
            {
                TotalBills = totalBills,
                Summary = summary,
                DrilldownBills = drilldownBills,
                ComparisonBoard = comparisonBoard,
                InstallmentCount = installmentCount,
                PostponedCount = postponedCount,
                BadDebtCount = badDebtCount,
                ChartLabels = chartLabels,
                ChartValues = chartValues,
                PaidValues = paidValues,
                OverdueValues = overdueValues,
                RecentDebts = recentDebts,
                LatestBillDate = latestBillDate,
                LatestDebtorDate = latestDebtorDate,
                LatestReceiptDate = latestReceiptDate
            };
        }
    }
}
