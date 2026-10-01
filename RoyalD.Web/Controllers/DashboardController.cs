using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;

        public DashboardController(AppDbContext db, IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _db = db;
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        // ==========================================
        // MODERN TRADE EXCLUSION LIST (Corporate Legal Entities & Customer Codes)
        // กรองร้านค้า Modern Trade (ห้าง/ร้านสะดวกซื้อ) ออกจากหนี้วิกฤต > 120 วัน
        // รองรับทั้งรหัสลูกค้า และชื่อบริษัทนิติบุคคลที่เปิดบิลจริง (เช่น ซีพี แอ็กซ์ตร้า, ซีพี ออลล์, เซ็นทรัล ฟู้ด)
        // ==========================================
        private static readonly HashSet<string> ModernTradeCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            // 1. 7-Eleven (บริษัท ซีพี ออลล์ จำกัด (มหาชน) / เครือข่ายเซเว่น)
            "101901", "10501", "103166", "110472", "110473",

            // 2. Big C / Pure / Big C Mini (บริษัท บิ๊กซี ซูเปอร์เซ็นเตอร์ จำกัด (มหาชน))
            "102003",

            // 3. Jiffy (บริษัท ปตท. บริหารธุรกิจค้าปลีก จำกัด)
            "102719",

            // 4. Tops / Tops Daily / Tops Care (บริษัท เซ็นทรัล ฟู้ด รีเทล จำกัด)
            "1020379", "103373", "103374", "103375", "103376", "110239", "110547", "110571",

            // 5. Makro (บริษัท ซีพี แอ็กซ์ตร้า จำกัด (มหาชน) / สยามแม็คโคร)
            "102445", "103468",

            // 6. Go Wholesale (บริษัท เซ็นทรัล ฟู้ด โฮลเซลล์ จำกัด)
            "1020441", "1020453", "103281", "103282", "103283", "120349", "200335", "200336",
            "400182", "410135", "500224", "830063", "830064", "900186",

            // 7. UCare (บริษัท ยูแคร์ จำกัด)
            "102540",

            // 8. Watsons (บริษัท เซ็นทรัล วัตสัน จำกัด)
            "101298",

            // 9. Golden Place (บริษัท สุวรรณชาด จำกัด)
            "102275", "102276", "102293", "102309", "102556", "102648", "102716", "102736",
            "102751", "102767", "103085", "103424", "240089", "600089", "760064", "770033",

            // 10. Tsuruha (บริษัท ซูรูฮะ (ประเทศไทย) จำกัด)
            "102307",

            // 11. PT.max (บริษัท ปิโตรเลียมไทยคอร์ปอเรชั่น จำกัด)
            "102265",

            // 12. P&F (บริษัท พี แอนด์ เอฟ อินทิเกรท จำกัด)
            "100783",

            // 13. MaxValu (บริษัท อิออน (ไทยแลนด์) จำกัด)
            "103329",

            // 14. CJ More / CJ Express (บริษัท ซี.เจ.เอ็กซ์เพรส กรุ๊ป จำกัด)
            "700084",

            // 15. Foodland (บริษัท ฟู้ดแลนด์ซุปเปอร์มาร์เก็ต จำกัด - ทุกสาขา)
            "1020359", "1020363", "102660", "102661", "102662", "102663", "102664", "102665",
            "102666", "102667", "102668", "102669", "102670", "102671", "102672", "102673",
            "102674", "102740", "103058", "103073", "200263", "200265", "300320", "730168", "740156",

            // 16. Harborland (บริษัท ฮาร์เบอร์ แลนด์ จำกัด)
            "102955", "102956", "103065", "103205", "103290", "110463", "110499", "110540", "730185",

            // 17. Boots (บริษัท บู๊ทส์ รีเทล (ประเทศไทย) จำกัด)
            "102677",

            // 18. Gourmet Market (บริษัท เดอะมอลล์ กรุ๊ป จำกัด)
            "1020327",

            // 19. Lawson 108 (บริษัท สห ลอว์สัน จำกัด)
            "102410",

            // 20. Villa Market (บริษัท วิลล่า มาร์เก็ท เจพี จำกัด)
            "103456",

            // 21. Fascino (บริษัท ฟาร์มาฮอฟ จำกัด)
            "102244", "200305", "730045"
        };

        // คีย์เวิร์ดชื่อนิติบุคคล / บริษัทที่เปิดบิลจริง สำหรับตรวจสอบแบบ dynamic ป้องกันชื่อร้านไม่ตรง
        private static readonly string[] ModernTradeNameKeywords = new[]
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

        /// <summary>คืนค่า true ถ้ารหัสลูกค้าหรือชื่อบริษัทเป็นกลุ่ม Modern Trade ที่ต้องกรองออกจาก Over120</summary>
        public static bool IsModernTrade(string? customerCode, string? customerName = null)
        {
            if (!string.IsNullOrEmpty(customerCode) && ModernTradeCodes.Contains(customerCode.Trim()))
                return true;

            if (!string.IsNullOrEmpty(customerName))
            {
                var name = customerName.Trim();
                // ข้อยกเว้น: บุคคลหรือร้านท้องถิ่นที่มีคำคล้ายแต่ไม่ใช่ห้าง Modern Trade
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



        private async Task<(DashboardSummary summary, List<DashboardBillItem> drilldownBills, ComparisonBoardViewModel comparisonBoard)> GetDashboardDataAsync()
        {
            var today = DateTime.Today;
            var cutoffDate = new DateTime(2026, 9, 14);

            // Fetch all non-cancelled debts
            var allDebts = await _db.OutstandingDebts
                .AsNoTracking()
                .Where(d => d.Status != DebtStatus.Cancelled)
                .OrderByDescending(d => d.BillDate)
                .ToListAsync();

            // Fetch any paid sales bills to include in collected
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
                string groupCode = "3"; // default upcountry

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

                // Region comparison breakdown
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
                    District = d.District ?? "",
                    Province = d.Province ?? "",
                    Amount = isPaid ? ((d.OriginalAmount - d.RemainingAmount > 0) ? d.OriginalAmount - d.RemainingAmount : d.OriginalAmount) : d.RemainingAmount,
                    Category = cat,
                    CategoryName = catName,
                    GroupCode = groupCode,
                    Credit = d.Credit,
                    DueDate = dueByCredit,
                    AgingDays = aging,
                    StatusName = isPaid ? "เก็บเงินสำเร็จ" : (aging > 120 ? "เกิน 120 วัน" : (aging > 0 ? $"เกินกำหนด {aging} วัน" : "ยังไม่ถึงกำหนด")),
                    IsPaid = isPaid,
                    IsBkk = isBkkArea,
                    IsModernTrade = IsModernTrade(d.CustomerCode, d.CustomerName)
                };

                drilldownBills.Add(billItem);

                if (!isPaid)
                {
                    if (groupCode == "1") comparisonBoard.Table1_Cash7Bkk.Add(billItem);
                    else if (groupCode == "2") comparisonBoard.Table2_CashTimelineBkk.Add(billItem);
                    else if (groupCode == "3") comparisonBoard.Table3_Upcountry.Add(billItem);
                }
            }

            // Include any additional paid sales bills from SalesBills table
            foreach (var sb in paidSalesBills)
            {
                if (!seenPaidBillNos.Contains(sb.BillNo))
                {
                    bool isBkkArea = RegionHelper.IsBkkAndVicinity(sb.Province, sb.District);
                    var sbDateNorm = sb.BillDate.Year > 2500 ? sb.BillDate.AddYears(-543) : sb.BillDate;
                    string groupCode = "3";
                    if (isBkkArea)
                    {
                        if (sbDateNorm.Date >= cutoffDate.Date && sb.Credit <= 7) groupCode = "1";
                        else groupCode = "2";
                    }

                    summary.Collected.BillCount++;
                    summary.Collected.TotalAmount += sb.TotalAmount;
                    summary.TotalSalesAmount += sb.TotalAmount;
                    summary.TotalAmount += sb.TotalAmount;
                    seenPaidBillNos.Add(sb.BillNo);

                    var item = new DashboardBillItem
                    {
                        BillNo = sb.BillNo,
                        BillDate = sb.BillDate,
                        SalesRep = sb.SalesRep ?? "",
                        CustomerCode = sb.CustomerCode ?? "",
                        CustomerName = sb.CustomerName ?? "",
                        District = sb.District ?? "",
                        Province = sb.Province ?? "",
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

        public class DashboardPageData
        {
            public int TotalBills { get; set; }
            public DashboardSummary Summary { get; set; } = null!;
            public List<DashboardBillItem> DrilldownBills { get; set; } = new();
            public ComparisonBoardViewModel ComparisonBoard { get; set; } = null!;
            public int InstallmentCount { get; set; }
            public int PostponedCount { get; set; }
            public int BadDebtCount { get; set; }
            public List<string> ChartLabels { get; set; } = new();
            public List<decimal> ChartValues { get; set; } = new();
            public List<decimal> PaidValues { get; set; } = new();
            public List<decimal> OverdueValues { get; set; } = new();
            public List<OutstandingDebt> RecentDebts { get; set; } = new();
            public DateTime? LatestBillDate { get; set; }
            public DateTime? LatestDebtorDate { get; set; }
            public DateTime? LatestReceiptDate { get; set; }
        }

        private async Task<DashboardPageData> LoadDashboardPageDataAsync()
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

            return new DashboardPageData
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

        // ==========================================
        // PAGE 1: AR EXECUTIVE SUMMARY & DASHBOARD
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var allowedPages = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
            if (!User.IsInRole("admin") && !allowedPages.Contains("dashboard"))
                return RedirectToAction("Index", "SalesBill");

            var data = await _cache.GetOrCreateAsync("dashboard_page_data_cache", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                return await LoadDashboardPageDataAsync();
            }) ?? new DashboardPageData();

            ViewBag.TotalBills = data.TotalBills;
            ViewBag.Summary = data.Summary;
            ViewBag.DrilldownBills = data.DrilldownBills;

            ViewBag.TotalOutstanding = data.Summary?.TotalOutstandingAmount ?? 0;
            ViewBag.TotalDebtors = data.Summary?.TotalDebtors ?? 0;

            ViewBag.OverdueCount = data.Summary?.Overdue120Days?.BillCount ?? 0;
            ViewBag.InstallmentCount = data.InstallmentCount;
            ViewBag.PostponedCount = data.PostponedCount;
            ViewBag.BadDebtCount = data.BadDebtCount;

            ViewBag.ChartLabels = data.ChartLabels;
            ViewBag.ChartValues = data.ChartValues;
            ViewBag.PaidValues = data.PaidValues;
            ViewBag.OverdueValues = data.OverdueValues;

            ViewBag.LatestBillDate = data.LatestBillDate;
            ViewBag.LatestDebtorDate = data.LatestDebtorDate;
            ViewBag.LatestReceiptDate = data.LatestReceiptDate;

            return View(data.RecentDebts);
        }

        // ==========================================
        // API: DRILLDOWN BILLS PAGINATION (PAGE 1)
        // แบ่งหน้าละ 50 รายการ โหลดเร็วติดทันที
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetDrilldownBills(
            string? category = "all",
            int page = 1,
            int pageSize = 50,
            string? searchBill = null,
            string? searchCustomer = null,
            string? searchSalesRep = null)
        {
            // ---- FAST PATH: ถ้า cache มีข้อมูลอยู่แล้ว ตอบกลับทันทีโดยไม่ต้องรอ DB ----
            if (!_cache.TryGetValue("dashboard_page_data_cache", out DashboardPageData? cachedData) || cachedData == null)
            {
                // Cache miss: warmup ใน background scope ใหม่ (ป้องกัน DbContext disposed)
                // ตอบกลับ warming=true ทันที → JS จะ retry อัตโนมัติใน 2 วินาที
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dashSvc = scope.ServiceProvider.GetRequiredService<DashboardService>();
                        var freshData = await dashSvc.LoadDashboardPageDataAsync();
                        _cache.Set("dashboard_page_data_cache", freshData, TimeSpan.FromMinutes(90));
                    }
                    catch { }
                });
                return Json(new { warming = true, message = "กำลังเตรียมข้อมูล..." });
            }

            var data = cachedData;


            var query = data.DrilldownBills.AsEnumerable();

            var cat = category?.Trim().ToLower() ?? "all";
            if (cat == "cash7")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "1");
            }
            else if (cat == "cash10")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "2");
            }
            else if (cat == "upcountry")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "3");
            }
            else if (cat == "over120")
            {
                query = query.Where(b => !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade);
            }
            else if (cat == "collected")
            {
                query = query.Where(b => b.IsPaid);
            }

            if (!string.IsNullOrWhiteSpace(searchBill))
            {
                var sBill = searchBill.Trim();
                query = query.Where(b => b.BillNo.Contains(sBill, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchCustomer))
            {
                var sCust = searchCustomer.Trim();
                query = query.Where(b => (b.CustomerName != null && b.CustomerName.Contains(sCust, StringComparison.OrdinalIgnoreCase)) ||
                                         (b.CustomerCode != null && b.CustomerCode.Contains(sCust, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(searchSalesRep))
            {
                var sRep = searchSalesRep.Trim();
                query = query.Where(b => b.SalesRep != null && b.SalesRep.Contains(sRep, StringComparison.OrdinalIgnoreCase));
            }

            // Ordering
            if (cat == "over120")
            {
                query = query.OrderByDescending(b => b.AgingDays).ThenByDescending(b => b.Amount);
            }
            else
            {
                query = query.OrderByDescending(b => b.BillDate).ThenBy(b => b.BillNo);
            }

            var totalItems = query.Count();
            var totalAmount = query.Sum(b => b.Amount);

            if (pageSize <= 0) pageSize = 50;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select((b, idx) => new
                {
                    index = (page - 1) * pageSize + idx + 1,
                    billNo = b.BillNo,
                    billDate = b.BillDate.ToString("dd/MM/yyyy"),
                    customerCode = b.CustomerCode,
                    customerName = b.CustomerName,
                    district = b.District ?? "",
                    province = b.Province ?? "",
                    credit = b.Credit > 0 ? b.Credit.ToString() : "สด",
                    agingDays = b.AgingDays,
                    amount = b.Amount,
                    amountFormatted = b.Amount.ToString("N2"),
                    salesRep = b.SalesRep ?? "",
                    isPaid = b.IsPaid,
                    statusName = b.IsPaid ? "ชำระแล้ว" : (b.AgingDays > 120 ? $"เกิน {b.AgingDays} วัน" : (b.AgingDays > 0 ? $"{b.AgingDays} วัน" : "ยังไม่ถึงกำหนด"))
                })
                .ToList();

            return Json(new
            {
                success = true,
                category = cat,
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                totalAmount,
                totalAmountFormatted = totalAmount.ToString("N2"),
                items
            });
        }

        // ==========================================
        // API: COMPARISON BILLS PAGINATION
        // แบ่งหน้าละ 50 รายการ โหลดเร็วติดทันที
        // ==========================================
        [HttpGet]
        public IActionResult GetComparisonBills(
            string? group = "all",
            string? subFilter = "all",
            int page = 1,
            int pageSize = 50,
            string? searchBill = null,
            string? searchCustomer = null,
            string? searchSalesRep = null)
        {
            if (!_cache.TryGetValue("dashboard_page_data_cache", out DashboardPageData? cachedData) || cachedData == null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dashSvc = scope.ServiceProvider.GetRequiredService<DashboardService>();
                        var freshData = await dashSvc.LoadDashboardPageDataAsync();
                        _cache.Set("dashboard_page_data_cache", freshData, TimeSpan.FromMinutes(90));
                    }
                    catch { }
                });
                return Json(new { warming = true, message = "กำลังเตรียมข้อมูล..." });
            }

            var allBills = cachedData.DrilldownBills.AsEnumerable();

            var grp = group?.Trim() ?? "all";
            var sub = subFilter?.Trim().ToLower() ?? "all";

            // กรองตาม Group
            if (grp == "1") allBills = allBills.Where(b => b.GroupCode == "1");
            else if (grp == "2") allBills = allBills.Where(b => b.GroupCode == "2");
            else if (grp == "3") allBills = allBills.Where(b => b.GroupCode == "3");

            // กรองตาม SubFilter
            if (sub == "outstanding") allBills = allBills.Where(b => !b.IsPaid);
            else if (sub == "under120") allBills = allBills.Where(b => !b.IsPaid && b.AgingDays <= 120);
            else if (sub == "over120") allBills = allBills.Where(b => !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade);
            else if (sub == "collected") allBills = allBills.Where(b => b.IsPaid);

            // กรองตาม Search
            if (!string.IsNullOrWhiteSpace(searchBill))
                allBills = allBills.Where(b => b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(searchCustomer))
                allBills = allBills.Where(b => (b.CustomerName != null && b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)) || (b.CustomerCode != null && b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)));
            if (!string.IsNullOrWhiteSpace(searchSalesRep))
                allBills = allBills.Where(b => b.SalesRep != null && b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase));

            // Sort
            var query = (sub == "over120")
                ? allBills.OrderByDescending(b => b.AgingDays).ThenByDescending(b => b.Amount)
                : allBills.OrderByDescending(b => b.BillDate).ThenBy(b => b.BillNo);

            var totalItems = query.Count();
            var totalAmount = query.Sum(b => b.Amount);
            if (pageSize <= 0) pageSize = 50;
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / pageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select((b, idx) => new
                {
                    index = (page - 1) * pageSize + idx + 1,
                    billNo = b.BillNo,
                    billDate = b.BillDate.ToString("dd/MM/yyyy"),
                    customerCode = b.CustomerCode,
                    customerName = b.CustomerName,
                    district = b.District ?? "",
                    province = b.Province ?? "",
                    credit = b.Credit > 0 ? b.Credit.ToString() : "สด",
                    agingDays = b.AgingDays,
                    amount = b.Amount,
                    amountFormatted = b.Amount.ToString("N2"),
                    salesRep = b.SalesRep ?? "",
                    isPaid = b.IsPaid,
                    groupCode = b.GroupCode,
                    isModernTrade = b.IsModernTrade
                }).ToList();

            // คำนวณ summary metrics สำหรับ 3 Big Cards (จาก filtered data ก่อน pagination)
            var allFiltered = cachedData.DrilldownBills.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchBill))
                allFiltered = allFiltered.Where(b => b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(searchCustomer))
                allFiltered = allFiltered.Where(b => (b.CustomerName != null && b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)) || (b.CustomerCode != null && b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)));
            if (!string.IsNullOrWhiteSpace(searchSalesRep))
                allFiltered = allFiltered.Where(b => b.SalesRep != null && b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase));

            var filteredList = allFiltered.ToList();
            var metrics = new
            {
                g1 = new { outAmt = filteredList.Where(b => b.GroupCode == "1" && !b.IsPaid).Sum(b => b.Amount), outCnt = filteredList.Count(b => b.GroupCode == "1" && !b.IsPaid), under120Amt = filteredList.Where(b => b.GroupCode == "1" && !b.IsPaid && b.AgingDays <= 120).Sum(b => b.Amount), under120Cnt = filteredList.Count(b => b.GroupCode == "1" && !b.IsPaid && b.AgingDays <= 120), over120Amt = filteredList.Where(b => b.GroupCode == "1" && !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade).Sum(b => b.Amount), over120Cnt = filteredList.Count(b => b.GroupCode == "1" && !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade), colAmt = filteredList.Where(b => b.GroupCode == "1" && b.IsPaid).Sum(b => b.Amount), colCnt = filteredList.Count(b => b.GroupCode == "1" && b.IsPaid) },
                g2 = new { outAmt = filteredList.Where(b => b.GroupCode == "2" && !b.IsPaid).Sum(b => b.Amount), outCnt = filteredList.Count(b => b.GroupCode == "2" && !b.IsPaid), under120Amt = filteredList.Where(b => b.GroupCode == "2" && !b.IsPaid && b.AgingDays <= 120).Sum(b => b.Amount), under120Cnt = filteredList.Count(b => b.GroupCode == "2" && !b.IsPaid && b.AgingDays <= 120), over120Amt = filteredList.Where(b => b.GroupCode == "2" && !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade).Sum(b => b.Amount), over120Cnt = filteredList.Count(b => b.GroupCode == "2" && !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade), colAmt = filteredList.Where(b => b.GroupCode == "2" && b.IsPaid).Sum(b => b.Amount), colCnt = filteredList.Count(b => b.GroupCode == "2" && b.IsPaid) },
                g3 = new { outAmt = filteredList.Where(b => b.GroupCode == "3" && !b.IsPaid).Sum(b => b.Amount), outCnt = filteredList.Count(b => b.GroupCode == "3" && !b.IsPaid), under120Amt = filteredList.Where(b => b.GroupCode == "3" && !b.IsPaid && b.AgingDays <= 120).Sum(b => b.Amount), under120Cnt = filteredList.Count(b => b.GroupCode == "3" && !b.IsPaid && b.AgingDays <= 120), over120Amt = filteredList.Where(b => b.GroupCode == "3" && !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade).Sum(b => b.Amount), over120Cnt = filteredList.Count(b => b.GroupCode == "3" && !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade), colAmt = filteredList.Where(b => b.GroupCode == "3" && b.IsPaid).Sum(b => b.Amount), colCnt = filteredList.Count(b => b.GroupCode == "3" && b.IsPaid) }
            };

            return Json(new { success = true, totalItems, totalPages, currentPage = page, pageSize, totalAmount, totalAmountFormatted = totalAmount.ToString("N2"), items, metrics });
        }

        // ==========================================
        // PAGE 2: AR DETAILED COMPARISON BOARD
        // ==========================================
        public async Task<IActionResult> Comparison()
        {
            var allowedPages = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
            if (!User.IsInRole("admin") && !allowedPages.Contains("dashboard"))
                return RedirectToAction("Index", "SalesBill");

            var data = await _cache.GetOrCreateAsync("dashboard_page_data_cache", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                return await LoadDashboardPageDataAsync();
            });

            return View(data?.ComparisonBoard ?? (await GetDashboardDataAsync()).comparisonBoard);
        }

        // ==========================================
        // EXPORT EXCEL ENDPOINTS (PAGE 1)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? category, string? searchBill, string? searchCustomer, string? searchSalesRep)
        {
            var (summary, bills, _) = await GetDashboardDataAsync();
            var query = bills.AsEnumerable();

            var cat = category?.Trim().ToLower() ?? "all";
            if (cat == "cash7")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "1");
            }
            else if (cat == "cash10")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "2");
            }
            else if (cat == "upcountry")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "3");
            }
            else if (cat == "over120")
            {
                query = query.Where(b => !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade);
            }
            else if (cat == "collected")
            {
                query = query.Where(b => b.IsPaid);
            }

            if (!string.IsNullOrEmpty(searchBill))
                query = query.Where(b => b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchCustomer))
                query = query.Where(b => (b.CustomerName != null && b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)) || (b.CustomerCode != null && b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(searchSalesRep))
                query = query.Where(b => b.SalesRep != null && b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase));

            var filteredList = (cat == "over120")
                ? query.OrderByDescending(b => b.AgingDays).ThenByDescending(b => b.Amount).ToList()
                : query.OrderByDescending(b => b.BillDate).ThenBy(b => b.BillNo).ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Executive_Summary");

            string groupTitle = category?.ToLower() switch
            {
                "cash7" => "เก็บสด 7 วัน (กทม.&ปริมณฑล)",
                "cash10" => "เงินสดรวมสายเวลา (กทม.&ปริมณฑล)",
                "upcountry" => "บิลค้างชำระต่างจังหวัดทั้งหมด",
                "over120" => "ค้างชำระ > 120 วัน",
                "collected" => "ยอดเก็บเงินสำเร็จ",
                _ => "รายงานภาพรวมทุกกลุ่ม"
            };

            ws.Cells["A1:I1"].Merge = true;
            ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:I2"].Merge = true;
            ws.Cells["A2"].Value = $"รายงานรายละเอียดบิล: {groupTitle} (พิมพ์ ณ วันที่ {DateTime.Now:dd/MM/yyyy HH:mm} น.)";
            ws.Cells["A2"].Style.Font.Size = 12;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            string[] headers = new[] { "#", "เลขที่บิล", "วันที่บิล", "ผู้แทนขาย", "รหัสลูกค้า", "ชื่อลูกค้า", "อำเภอ", "จังหวัด", "จำนวนเงิน (บาท)" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[4, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = (i == 0 || i == 1 || i == 2 || i == 4) ? ExcelHorizontalAlignment.Center : (i == 8 ? ExcelHorizontalAlignment.Right : ExcelHorizontalAlignment.Left);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int r = 5;
            for (int idx = 0; idx < filteredList.Count; idx++)
            {
                var item = filteredList[idx];
                ws.Cells[r, 1].Value = idx + 1;
                ws.Cells[r, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[r, 2].Value = item.BillNo;
                ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[r, 3].Value = item.BillDate.ToString("dd/MM/yyyy");
                ws.Cells[r, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[r, 4].Value = item.SalesRep;

                ws.Cells[r, 5].Value = item.CustomerCode;
                ws.Cells[r, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[r, 6].Value = item.CustomerName;
                ws.Cells[r, 7].Value = item.District;
                ws.Cells[r, 8].Value = item.Province;

                ws.Cells[r, 9].Value = item.Amount;
                ws.Cells[r, 9].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[r, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                for (int c = 1; c <= 9; c++)
                    ws.Cells[r, c].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.LightGray);

                r++;
            }

            ws.Cells[r, 1, r, 8].Merge = true;
            ws.Cells[r, 1].Value = $"ยอดรวมทั้งสิ้น ({filteredList.Count:N0} รายการ):";
            ws.Cells[r, 1].Style.Font.Bold = true;
            ws.Cells[r, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[r, 9].Value = filteredList.Sum(x => x.Amount);
            ws.Cells[r, 9].Style.Font.Bold = true;
            ws.Cells[r, 9].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[r, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            for (int c = 1; c <= 9; c++)
            {
                ws.Cells[r, c].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws.Cells[r, c].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                ws.Cells[r, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[r, c].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 253, 250));
            }

            ws.Cells.AutoFitColumns();
            ws.Column(1).Width = 5;
            ws.Column(6).Width = Math.Max(ws.Column(6).Width, 35);
            ws.Column(9).Width = 18;

            var fileBytes = package.GetAsByteArray();
            string fileName = $"AR_Drilldown_{(category ?? "All")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==========================================
        // EXPORT PDF ENDPOINTS (PAGE 1)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportPdf(string? category, string? searchBill, string? searchCustomer, string? searchSalesRep)
        {
            var (summary, bills, _) = await GetDashboardDataAsync();
            var query = bills.AsEnumerable();

            var cat = category?.Trim().ToLower() ?? "all";
            if (cat == "cash7")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "1");
            }
            else if (cat == "cash10")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "2");
            }
            else if (cat == "upcountry")
            {
                query = query.Where(b => !b.IsPaid && b.GroupCode == "3");
            }
            else if (cat == "over120")
            {
                query = query.Where(b => !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade);
            }
            else if (cat == "collected")
            {
                query = query.Where(b => b.IsPaid);
            }

            if (!string.IsNullOrEmpty(searchBill))
                query = query.Where(b => b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchCustomer))
                query = query.Where(b => (b.CustomerName != null && b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)) || (b.CustomerCode != null && b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(searchSalesRep))
                query = query.Where(b => b.SalesRep != null && b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase));

            var filteredList = (cat == "over120")
                ? query.OrderByDescending(b => b.AgingDays).ThenByDescending(b => b.Amount).ToList()
                : query.OrderByDescending(b => b.BillDate).ThenBy(b => b.BillNo).ToList();

            ViewBag.Category = category;
            ViewBag.CategoryTitle = category?.ToLower() switch
            {
                "cash7" => "เก็บสด 7 วัน (กทม.&ปริมณฑล)",
                "cash10" => "เงินสดรวมสายเวลา (กทม.&ปริมณฑล)",
                "upcountry" => "บิลค้างชำระต่างจังหวัดทั้งหมด",
                "over120" => "ค้างชำระ > 120 วัน",
                "collected" => "ยอดเก็บเงินสำเร็จ",
                _ => "รายงานภาพรวมทุกกลุ่ม"
            };
            ViewBag.SearchBill = searchBill;
            ViewBag.SearchCustomer = searchCustomer;
            ViewBag.SearchSalesRep = searchSalesRep;
            ViewBag.PrintedBy = User.Identity?.Name ?? "Admin";

            return View("PrintPdf", filteredList);
        }

        // ==========================================
        // EXPORT EXCEL ENDPOINT (PAGE 2: COMPARISON)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportComparisonExcel(string? section, string? filterType, string? searchBill, string? searchCustomer, string? searchSalesRep)
        {
            var (_, _, comp) = await GetDashboardDataAsync();

            Func<DashboardBillItem, bool> filterFunc = b =>
            {
                if (!string.IsNullOrEmpty(searchBill) && !b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.IsNullOrEmpty(searchCustomer) && !b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase) && !b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.IsNullOrEmpty(searchSalesRep) && !b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase)) return false;

                if (!string.IsNullOrEmpty(filterType) && filterType.ToLower() != "all")
                {
                    if (filterType == "outstanding" && b.IsPaid) return false;
                    if (filterType == "under120" && (b.IsPaid || b.AgingDays > 120)) return false;
                    if (filterType == "over120" && (b.IsPaid || b.AgingDays <= 120 || b.IsModernTrade)) return false;
                    if (filterType == "collected" && !b.IsPaid) return false;
                }

                return true;
            };

            var list1 = comp.AllBills.Where(b => b.GroupCode == "1").Where(filterFunc).OrderBy(b => b.BillDate).ToList();
            var list2 = comp.AllBills.Where(b => b.GroupCode == "2").Where(filterFunc).OrderBy(b => b.BillDate).ToList();
            var list3 = comp.AllBills.Where(b => b.GroupCode == "3").Where(filterFunc).OrderBy(b => b.BillDate).ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

            Action<ExcelWorksheet, string, List<DashboardBillItem>, Color> buildSheet = (ws, title, list, headerBg) =>
            {
                ws.Cells["A1:J1"].Merge = true;
                ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";
                ws.Cells["A1"].Style.Font.Size = 15;
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells["A2:J2"].Merge = true;
                ws.Cells["A2"].Value = $"กระดานเปรียบเทียบ: {title} (พิมพ์ ณ {DateTime.Now:dd/MM/yyyy HH:mm} น.)";
                ws.Cells["A2"].Style.Font.Size = 11;
                ws.Cells["A2"].Style.Font.Bold = true;
                ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                string[] cols = new[] { "#", "เลขที่บิล", "วันที่บิล", "รหัสลูกค้า", "ชื่อลูกค้า", "อำเภอ", "จังหวัด", "เครดิต", "อายุหนี้", "จำนวนเงิน (บาท)", "ผู้แทนขาย" };
                for (int c = 0; c < cols.Length; c++)
                {
                    var cell = ws.Cells[4, c + 1];
                    cell.Value = cols[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(headerBg);
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = (c == 0 || c == 1 || c == 2 || c == 3 || c == 7 || c == 8) ? ExcelHorizontalAlignment.Center : (c == 9 ? ExcelHorizontalAlignment.Right : ExcelHorizontalAlignment.Left);
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                int row = 5;
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    ws.Cells[row, 1].Value = i + 1;
                    ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 2].Value = item.BillNo;
                    ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 3].Value = item.BillDate.ToString("dd/MM/yyyy");
                    ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 4].Value = item.CustomerCode;
                    ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 5].Value = item.CustomerName;
                    ws.Cells[row, 6].Value = item.District;
                    ws.Cells[row, 7].Value = item.Province;
                    ws.Cells[row, 8].Value = item.Credit;
                    ws.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 9].Value = item.IsPaid ? "ชำระแล้ว" : (item.AgingDays > 0 ? $"{item.AgingDays} วัน" : "ยังไม่ถึงกำหนด");
                    ws.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 10].Value = item.Amount;
                    ws.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";
                    ws.Cells[row, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    ws.Cells[row, 11].Value = item.SalesRep;

                    for (int c = 1; c <= 11; c++)
                        ws.Cells[row, c].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.LightGray);

                    row++;
                }

                // Summary Row
                ws.Cells[row, 1, row, 9].Merge = true;
                ws.Cells[row, 1].Value = $"ยอดรวมทั้งสิ้น ({list.Count:N0} รายการ):";
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                ws.Cells[row, 10].Value = list.Sum(x => x.Amount);
                ws.Cells[row, 10].Style.Font.Bold = true;
                ws.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                for (int c = 1; c <= 11; c++)
                {
                    ws.Cells[row, c].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    ws.Cells[row, c].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));
                }

                ws.Cells.AutoFitColumns();
                ws.Column(1).Width = 5;
                ws.Column(5).Width = Math.Max(ws.Column(5).Width, 35);
                ws.Column(10).Width = 18;
            };

            if (section == "1")
            {
                var ws1 = package.Workbook.Worksheets.Add("7 วัน กทม.");
                buildSheet(ws1, "1. บิลเก็บสด 7 วัน (กทม.&ปริมณฑล)", list1, Color.FromArgb(2, 132, 199));
            }
            else if (section == "2")
            {
                var ws2 = package.Workbook.Worksheets.Add("รวมสายเวลา กทม.");
                buildSheet(ws2, "2. บิลเงินสดนโยบายรวมสายเวลา (กทม.&ปริมณฑล)", list2, Color.FromArgb(217, 119, 6));
            }
            else if (section == "3")
            {
                var ws3 = package.Workbook.Worksheets.Add("ต่างจังหวัด");
                buildSheet(ws3, "3. บิลค้างชำระ ต่างจังหวัด (ทุกภาค)", list3, Color.FromArgb(124, 58, 237));
            }
            else
            {
                var ws1 = package.Workbook.Worksheets.Add("1. 7 วัน กทม.");
                buildSheet(ws1, "1. บิลเก็บสด 7 วัน (กทม.&ปริมณฑล)", list1, Color.FromArgb(2, 132, 199));

                var ws2 = package.Workbook.Worksheets.Add("2. รวมสายเวลา กทม.");
                buildSheet(ws2, "2. บิลเงินสดนโยบายรวมสายเวลา (กทม.&ปริมณฑล)", list2, Color.FromArgb(217, 119, 6));

                var ws3 = package.Workbook.Worksheets.Add("3. ต่างจังหวัด");
                buildSheet(ws3, "3. บิลค้างชำระ ต่างจังหวัด (ทุกภาค)", list3, Color.FromArgb(124, 58, 237));
            }

            var fileBytes = package.GetAsByteArray();
            string fileName = $"AR_Comparison_Board_{(section ?? "All")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==========================================
        // EXPORT PDF ENDPOINT (PAGE 2: COMPARISON)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportComparisonPdf(string? section, string? filterType, string? searchBill, string? searchCustomer, string? searchSalesRep)
        {
            var (_, _, comp) = await GetDashboardDataAsync();

            Func<DashboardBillItem, bool> filterFunc = b =>
            {
                if (!string.IsNullOrEmpty(searchBill) && !b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.IsNullOrEmpty(searchCustomer) && !b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase) && !b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.IsNullOrEmpty(searchSalesRep) && !b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase)) return false;

                if (!string.IsNullOrEmpty(filterType) && filterType.ToLower() != "all")
                {
                    if (filterType == "outstanding" && b.IsPaid) return false;
                    if (filterType == "under120" && (b.IsPaid || b.AgingDays > 120)) return false;
                    if (filterType == "over120" && (b.IsPaid || b.AgingDays <= 120 || b.IsModernTrade)) return false;
                    if (filterType == "collected" && !b.IsPaid) return false;
                }

                return true;
            };

            comp.Table1_Cash7Bkk = comp.AllBills.Where(b => b.GroupCode == "1").Where(filterFunc).OrderBy(b => b.BillDate).ToList();
            comp.Table2_CashTimelineBkk = comp.AllBills.Where(b => b.GroupCode == "2").Where(filterFunc).OrderBy(b => b.BillDate).ToList();
            comp.Table3_Upcountry = comp.AllBills.Where(b => b.GroupCode == "3").Where(filterFunc).OrderBy(b => b.BillDate).ToList();

            ViewBag.Section = section ?? "all";
            ViewBag.FilterType = filterType ?? "all";
            ViewBag.PrintedBy = User.Identity?.Name ?? "Admin";

            return View("PrintComparisonPdf", comp);
        }
    }
}
