using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public DashboardController(AppDbContext db) => _db = db;

        // ==========================================
        // MODERN TRADE EXCLUSION LIST (by Customer Code)
        // กรองร้านค้า Modern Trade ออกจากหนี้วิกฤต > 120 วัน
        // อ้างอิงจากรหัสลูกค้าจริงในฐานข้อมูล (ไม่ใช้ชื่อร้าน)
        // ==========================================
        private static readonly HashSet<string> ModernTradeCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            // BigC
            "102003",
            // Watson / Central Watson
            "101298",
            // UCare
            "102540",
            // Lawson108 / สห ลอว์สัน
            "102410",
            // Boots รีเทล
            "102677",
            // Villa Market
            "103456", "740207",
            // Foodland (ทุกสาขา)
            "102660", "102661", "102662", "102664", "102665",
            "102666", "102667", "102668", "102669", "102670",
            "102671", "102672", "102673", "102674", "102740",
            "103058", "103073", "200263", "200265",
            "300320", "730168", "1020359", "1020363",
            // Winning Seven (7-Eleven network)
            "103166",
            // Seven Pharma chains
            "110472", "110473",
        };

        /// <summary>คืนค่า true ถ้ารหัสลูกค้าเป็น Modern Trade ที่ต้องกรองออกจาก Over120</summary>
        private static bool IsModernTrade(string? customerCode)
            => !string.IsNullOrEmpty(customerCode) && ModernTradeCodes.Contains(customerCode.Trim());



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

                    if (aging > 120 && !IsModernTrade(d.CustomerCode))
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
                    IsModernTrade = IsModernTrade(d.CustomerCode)
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
                        IsBkk = isBkkArea
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

        // ==========================================
        // PAGE 1: AR EXECUTIVE SUMMARY & DASHBOARD
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var allowedPages = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
            if (!User.IsInRole("admin") && !allowedPages.Contains("dashboard"))
                return RedirectToAction("Index", "SalesBill");

            var today = DateTime.Today;

            var cancelledCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Cancelled);
            ViewBag.TotalBills = (await _db.SalesBills.CountAsync()) - cancelledCount;

            var (summary, drilldownBills, comparisonBoard) = await GetDashboardDataAsync();

            ViewBag.Summary = summary;
            ViewBag.DrilldownBills = drilldownBills;

            ViewBag.TotalOutstanding = summary.TotalOutstandingAmount;
            ViewBag.TotalDebtors = summary.TotalDebtors;

            ViewBag.OverdueCount = summary.Overdue120Days.BillCount;
            ViewBag.InstallmentCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Installment);
            ViewBag.PostponedCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.Postponed);
            ViewBag.BadDebtCount = await _db.OutstandingDebts.CountAsync(d => d.Status == DebtStatus.BadDebt);

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

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartValues = chartValues;
            ViewBag.PaidValues = paidValues;
            ViewBag.OverdueValues = overdueValues;

            var recentDebts = await _db.OutstandingDebts
                .Where(d => d.Status == DebtStatus.Outstanding)
                .OrderBy(d => d.DueDate)
                .Take(10)
                .ToListAsync();

            ViewBag.LatestBillDate = await _db.SalesBills.OrderByDescending(b => b.BillDate).Select(b => (DateTime?)b.BillDate).FirstOrDefaultAsync();
            ViewBag.LatestDebtorDate = await _db.OutstandingDebts.OrderByDescending(d => d.BillDate).Select(d => (DateTime?)d.BillDate).FirstOrDefaultAsync();
            ViewBag.LatestReceiptDate = await _db.SalesBills.Where(b => b.ReceiptDate != null).OrderByDescending(b => b.ReceiptDate).Select(b => (DateTime?)b.ReceiptDate).FirstOrDefaultAsync()
                                      ?? await _db.OutstandingDebts.Where(d => d.ReceiptDate != null).OrderByDescending(d => d.ReceiptDate).Select(d => (DateTime?)d.ReceiptDate).FirstOrDefaultAsync();

            return View(recentDebts);
        }

        // ==========================================
        // PAGE 2: AR DETAILED COMPARISON BOARD
        // ==========================================
        public async Task<IActionResult> Comparison()
        {
            var allowedPages = User.FindFirst("AllowedPages")?.Value?.Split(',').Select(p => p.Trim().ToLower()) ?? Array.Empty<string>();
            if (!User.IsInRole("admin") && !allowedPages.Contains("dashboard"))
                return RedirectToAction("Index", "SalesBill");

            var (summary, drilldownBills, comparisonBoard) = await GetDashboardDataAsync();
            return View(comparisonBoard);
        }

        // ==========================================
        // EXPORT EXCEL ENDPOINTS (PAGE 1)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? category, string? searchBill, string? searchCustomer, string? searchSalesRep)
        {
            var (summary, bills, _) = await GetDashboardDataAsync();
            var query = bills.AsEnumerable();

            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
                query = query.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchBill))
                query = query.Where(b => b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchCustomer))
                query = query.Where(b => b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase) || b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchSalesRep))
                query = query.Where(b => b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase));

            // When exporting over120, apply over120 filter + exclude Modern Trade
            if (category?.ToLower() == "over120")
                query = query.Where(b => !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade);

            var filteredList = (category?.ToLower() == "over120")
                ? query.OrderByDescending(b => b.AgingDays).ThenBy(b => b.BillDate).ToList()
                : query.OrderBy(b => b.BillDate).ToList();

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

            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
                query = query.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchBill))
                query = query.Where(b => b.BillNo.Contains(searchBill, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchCustomer))
                query = query.Where(b => b.CustomerName.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase) || b.CustomerCode.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(searchSalesRep))
                query = query.Where(b => b.SalesRep.Contains(searchSalesRep, StringComparison.OrdinalIgnoreCase));

            // When exporting over120, apply over120 filter + exclude Modern Trade
            if (category?.ToLower() == "over120")
                query = query.Where(b => !b.IsPaid && b.AgingDays > 120 && !b.IsModernTrade);

            var filteredList = (category?.ToLower() == "over120")
                ? query.OrderByDescending(b => b.AgingDays).ThenBy(b => b.BillDate).ToList()
                : query.OrderBy(b => b.BillDate).ToList();

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
                    if (filterType == "over120" && (b.IsPaid || b.AgingDays <= 120)) return false;
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
                    if (filterType == "over120" && (b.IsPaid || b.AgingDays <= 120)) return false;
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
