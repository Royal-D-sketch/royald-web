using RoyalD.Web.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace RoyalD.Web.Services
{
    public class SalesReportData
    {
        public string SalesRep { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class MonthlySummaryItem
    {
        public string MonthKey { get; set; } = string.Empty; // e.g. "2026-01"
        public string MonthNameThai { get; set; } = string.Empty; // e.g. "ม.ค. 69"
        public decimal TotalSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal Outstanding { get; set; }
        public int Overdue120Count { get; set; }
        public decimal CollectionRate => TotalSales > 0 ? Math.Round(TotalCollected / TotalSales * 100m, 1) : 0m;
    }

    public class SalesRepPivotRow
    {
        public string SalesRep { get; set; } = string.Empty;
        public List<MonthlySummaryItem> MonthlyData { get; set; } = new();
        public decimal TotalSales => MonthlyData.Sum(m => m.TotalSales);
        public decimal TotalCollected => MonthlyData.Sum(m => m.TotalCollected);
        public decimal TotalOutstanding => MonthlyData.Sum(m => m.Outstanding);
        public decimal OverallCollectionRate => TotalSales > 0 ? Math.Round(TotalCollected / TotalSales * 100m, 1) : 0m;
        public int TotalOverdue120 => MonthlyData.Sum(m => m.Overdue120Count);
    }

    public class SalesSummaryMatrixViewModel
    {
        public List<string> Months { get; set; } = new();
        public List<string> MonthKeys { get; set; } = new();
        public List<SalesRepPivotRow> RepRows { get; set; } = new();
        public SalesRepPivotRow TotalCompany { get; set; } = new();
        public SalesRepMonthlyReport OverallCompany { get; set; } = new();
    }

    public class SalesRepMonthlyReport
    {
        public string SalesRep { get; set; } = string.Empty;
        public List<MonthlySummaryItem> MonthlyData { get; set; } = new();
        public decimal TotalSales => MonthlyData.Sum(m => m.TotalSales);
        public decimal TotalCollected => MonthlyData.Sum(m => m.TotalCollected);
        public decimal TotalOutstanding => MonthlyData.Sum(m => m.Outstanding);
        public decimal OverallCollectionRate => TotalSales > 0 ? Math.Round(TotalCollected / TotalSales * 100m, 1) : 0m;
        public int TotalOverdue120 => MonthlyData.Sum(m => m.Overdue120Count);
    }

    public class AnnualPerformanceViewModel
    {
        public List<string> Months { get; set; } = new();
        public List<string> MonthKeys { get; set; } = new();
        public SalesRepMonthlyReport OverallCompany { get; set; } = new();
        public List<SalesRepMonthlyReport> RepReports { get; set; } = new();
    }

    public class ProductMovementGroup
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal AvgPrice => TotalQty > 0 ? Math.Round(TotalAmount / TotalQty, 2) : 0m;
        public decimal TotalAmount { get; set; }
        public decimal DeliveringQty { get; set; }
        public decimal DeliveringAmount { get; set; }
        public decimal NetQty => TotalQty - DeliveringQty;
        public decimal NetAmount => TotalAmount - DeliveringAmount;
        public bool HasDelivering => DeliveringQty > 0;
        public bool IsAllDelivering => TotalQty > 0 && DeliveringQty >= TotalQty;
    }

    public class RepMonthlyProductMovement
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthNameThai { get; set; } = string.Empty;
        public List<ProductMovementGroup> Products { get; set; } = new();
        public decimal MonthGrossAmount => Products.Sum(p => p.TotalAmount);
        public decimal MonthDeliveringDeduction => Products.Sum(p => p.DeliveringAmount);
        public decimal MonthNetAmount => Products.Sum(p => p.NetAmount);
        public decimal MonthNetQty => Products.Sum(p => p.NetQty);
    }

    public class SalesRepProductReport
    {
        public string SalesRep { get; set; } = string.Empty;
        public List<RepMonthlyProductMovement> MonthlyMovements { get; set; } = new();
        public decimal TotalGrossSales => MonthlyMovements.Sum(m => m.MonthGrossAmount);
        public decimal TotalDeliveringDeduction => MonthlyMovements.Sum(m => m.MonthDeliveringDeduction);
        public decimal TotalNetSales => MonthlyMovements.Sum(m => m.MonthNetAmount);
        public decimal TotalNetQty => MonthlyMovements.Sum(m => m.MonthNetQty);
    }

    public class ProductDetailsReportViewModel
    {
        public List<string> Months { get; set; } = new();
        public List<string> MonthKeys { get; set; } = new();
        public List<string> SalesReps { get; set; } = new();
        public string? SelectedSalesRep { get; set; }
        public string? SelectedMonth { get; set; }
        public List<SalesRepProductReport> RepReports { get; set; } = new();
    }

    public class ReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db) => _db = db;

        private static readonly Dictionary<string, string> StandardMonthsMap = new()
        {
            ["2026-01"] = "ม.ค. 69",
            ["2026-02"] = "ก.พ. 69",
            ["2026-03"] = "มี.ค. 69",
            ["2026-04"] = "เม.ย. 69",
            ["2026-05"] = "พ.ค. 69",
            ["2026-06"] = "มิ.ย. 69",
            ["2026-07"] = "ก.ค. 69",
            ["2026-08"] = "ส.ค. 69 (1-20 ส.ค.)"
        };

        // 1. Pivot Matrix Report (Summary)
        public async Task<SalesSummaryMatrixViewModel> GetSalesSummaryMatrixAsync()
        {
            var monthKeys = StandardMonthsMap.Keys.ToList();
            var monthLabels = StandardMonthsMap.Values.ToList();

            var bills = await _db.SalesBills
                .Select(b => new {
                    b.BillNo,
                    b.BillDate,
                    b.SourceMonth,
                    SalesRep = string.IsNullOrEmpty(b.SalesRep) ? "ไม่ระบุ" : b.SalesRep,
                    b.TotalAmount
                }).ToListAsync();

            var today = DateTime.Today;
            var debts = await _db.OutstandingDebts.Select(d => new { d.Id, d.BillNo, d.OriginalAmount, d.RemainingAmount, d.DueDate, d.Status, d.ReceiptDate, SalesRep = string.IsNullOrEmpty(d.SalesRep) ? "ไม่ระบุ" : d.SalesRep }).ToListAsync();
            var payments = await _db.PaymentRecords.Select(p => new { p.OutstandingDebtId, p.PaidDate, p.PaidAmount }).ToListAsync();

            var debtDict = debts.GroupBy(d => d.BillNo).ToDictionary(g => g.Key, g => g.First());

            var allReps = bills.Select(b => b.SalesRep)
                .Where(r => !string.IsNullOrEmpty(r) && r != "ไม่ระบุ")
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            MonthlySummaryItem CalcMonth(List<dynamic> mBills, string mk, string repName, bool isOverall)
            {
                decimal mSales = mBills.Sum(b => (decimal)b.TotalAmount);
                decimal mOutstanding = 0m;
                int mOverdue120 = 0;
                foreach (var b in mBills)
                {
                    if (debtDict.TryGetValue((string)b.BillNo, out var d))
                    {
                        decimal billOutstanding = d.RemainingAmount;
                        if (d.OriginalAmount > d.RemainingAmount && d.ReceiptDate.HasValue)
                        {
                            string receiptMonth = d.ReceiptDate.Value.ToString("yyyy-MM");
                            if (string.Compare(receiptMonth, mk) > 0)
                            {
                                billOutstanding = d.OriginalAmount;
                            }
                        }
                        mOutstanding += billOutstanding;
                        if (d.RemainingAmount > 0 && (today - d.DueDate).TotalDays > 120)
                            mOverdue120++;
                    }
                }
                int mkYear = int.Parse(mk.Substring(0, 4));
                int mkMonth = int.Parse(mk.Substring(5, 2));

                decimal mCollected = 0m;
                var repDebts = isOverall ? debts : debts.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                foreach (var d in repDebts)
                {
                    var dPayments = payments.Where(p => p.OutstandingDebtId == d.Id && 
                        (p.PaidDate.Year > 2500 ? p.PaidDate.Year - 543 : p.PaidDate.Year) == mkYear && p.PaidDate.Month == mkMonth).ToList();
                    
                    if (d.ReceiptDate.HasValue && 
                        (d.ReceiptDate.Value.Year > 2500 ? d.ReceiptDate.Value.Year - 543 : d.ReceiptDate.Value.Year) == mkYear && 
                        d.ReceiptDate.Value.Month == mkMonth)
                    {
                        mCollected += (d.OriginalAmount - d.RemainingAmount);
                    }
                }

                return new MonthlySummaryItem
                {
                    MonthKey = mk,
                    MonthNameThai = StandardMonthsMap[mk],
                    TotalSales = mSales,
                    TotalCollected = mCollected,
                    Outstanding = mOutstanding,
                    Overdue120Count = mOverdue120
                };
            }

            var vm = new SalesSummaryMatrixViewModel
            {
                MonthKeys = monthKeys,
                Months = monthLabels
            };

            foreach (var rep in allReps)
            {
                var row = new SalesRepPivotRow { SalesRep = rep };
                foreach (var mk in monthKeys)
                {
                    var mBills = bills.Where(b => (b.SalesRep != null && b.SalesRep.Contains(rep.Trim())) && (b.SourceMonth == mk || b.BillDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture) == mk)).Cast<dynamic>().ToList();
                    row.MonthlyData.Add(CalcMonth(mBills, mk, rep, false));
                }
                vm.RepRows.Add(row);
            }

            // Total Company row
            var totalRow = new SalesRepPivotRow { SalesRep = "รวมทั้งบริษัท (Total)" };
            var overallRep = new SalesRepMonthlyReport { SalesRep = "ภาพรวมผู้แทนทุกคน" };

            foreach (var mk in monthKeys)
            {
                var mBills = bills.Where(b => (b.SourceMonth == mk || b.BillDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture) == mk)).Cast<dynamic>().ToList();
                var cMonth = CalcMonth(mBills, mk, "", true);
                totalRow.MonthlyData.Add(cMonth);
                overallRep.MonthlyData.Add(cMonth);
            }
            vm.TotalCompany = totalRow;
            vm.OverallCompany = overallRep;

            return vm;
        }

        // 2. Annual Performance Charts
        public async Task<AnnualPerformanceViewModel> GetAnnualPerformanceAsync()
        {
            var monthKeys = StandardMonthsMap.Keys.ToList();
            var monthLabels = StandardMonthsMap.Values.ToList();

            var bills = await _db.SalesBills
                .Select(b => new { 
                    b.BillNo, 
                    b.BillDate, 
                    b.SourceMonth,
                    SalesRep = string.IsNullOrEmpty(b.SalesRep) ? "ไม่ระบุ" : b.SalesRep, 
                    b.TotalAmount 
                }).ToListAsync();

            var today = DateTime.Today;
            var debts = await _db.OutstandingDebts.Select(d => new { d.Id, d.BillNo, d.OriginalAmount, d.RemainingAmount, d.DueDate, d.Status, d.ReceiptDate, SalesRep = string.IsNullOrEmpty(d.SalesRep) ? "ไม่ระบุ" : d.SalesRep }).ToListAsync();
            var payments = await _db.PaymentRecords.Select(p => new { p.OutstandingDebtId, p.PaidDate, p.PaidAmount }).ToListAsync();

            var debtDict = debts.GroupBy(d => d.BillNo).ToDictionary(g => g.Key, g => g.First());

            var allReps = bills.Select(b => b.SalesRep)
                .Where(r => !string.IsNullOrEmpty(r) && r != "ไม่ระบุ")
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            SalesRepMonthlyReport BuildReport(string repName, bool isOverall)
            {
                var rep = new SalesRepMonthlyReport { SalesRep = repName };
                foreach (var mk in monthKeys)
                {
                    var mBills = isOverall 
                        ? bills.Where(b => (b.SourceMonth == mk || b.BillDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture) == mk)).ToList()
                        : bills.Where(b => (b.SalesRep != null && b.SalesRep.Contains(repName.Trim())) && (b.SourceMonth == mk || b.BillDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture) == mk)).ToList();

                    decimal mSales = mBills.Sum(b => b.TotalAmount);
                    decimal mOutstanding = 0m;
                    int mOverdue120 = 0;

                    foreach (var b in mBills)
                    {
                        if (debtDict.TryGetValue(b.BillNo, out var d))
                        {
                            decimal billOutstanding = d.RemainingAmount;
                            if (d.OriginalAmount > d.RemainingAmount && d.ReceiptDate.HasValue)
                            {
                                string receiptMonth = d.ReceiptDate.Value.ToString("yyyy-MM");
                                if (string.Compare(receiptMonth, mk) > 0)
                                {
                                    billOutstanding = d.OriginalAmount;
                                }
                            }
                            mOutstanding += billOutstanding;
                            if (d.RemainingAmount > 0 && (today - d.DueDate).TotalDays > 120)
                                mOverdue120++;
                        }
                    }

                    int mkYear = int.Parse(mk.Substring(0, 4));
                    int mkMonth = int.Parse(mk.Substring(5, 2));

                    decimal mCollected = 0m;
                    var repDebts = isOverall ? debts : debts.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                    foreach (var d in repDebts)
                    {
                        var dPayments = payments.Where(p => p.OutstandingDebtId == d.Id && 
                            (p.PaidDate.Year > 2500 ? p.PaidDate.Year - 543 : p.PaidDate.Year) == mkYear && p.PaidDate.Month == mkMonth).ToList();
                        
                        if (d.ReceiptDate.HasValue && 
                            (d.ReceiptDate.Value.Year > 2500 ? d.ReceiptDate.Value.Year - 543 : d.ReceiptDate.Value.Year) == mkYear && 
                            d.ReceiptDate.Value.Month == mkMonth)
                        {
                            mCollected += (d.OriginalAmount - d.RemainingAmount);
                        }
                    }

                    rep.MonthlyData.Add(new MonthlySummaryItem
                    {
                        MonthKey = mk,
                        MonthNameThai = StandardMonthsMap[mk],
                        TotalSales = mSales,
                        TotalCollected = mCollected,
                        Outstanding = mOutstanding,
                        Overdue120Count = mOverdue120
                    });
                }
                return rep;
            }

            var vm = new AnnualPerformanceViewModel
            {
                MonthKeys = monthKeys,
                Months = monthLabels,
                OverallCompany = BuildReport("ภาพรวมผู้แทนทุกคน", true)
            };

            foreach (var r in allReps)
            {
                vm.RepReports.Add(BuildReport(r, false));
            }

            return vm;
        }

        // 3. Product Movement Details with Delivering Deduction Logic
        public async Task<ProductDetailsReportViewModel> GetProductDetailsReportAsync(string? selectedRep = null, string? selectedMonth = null)
        {
            var monthKeys = StandardMonthsMap.Keys.ToList();
            var monthLabels = StandardMonthsMap.Values.ToList();

            var billQuery = _db.SalesBills.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(selectedRep)) { var sRepTrim = selectedRep.Trim(); billQuery = billQuery.Where(b => b.SalesRep != null && b.SalesRep.Contains(sRepTrim)); }
            if (!string.IsNullOrEmpty(selectedMonth))
            {
                if (DateTime.TryParse(selectedMonth + "-01", out var mDate))
                {
                    var startOfMonth = new DateTime(mDate.Year, mDate.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1);
                    billQuery = billQuery.Where(b => b.SourceMonth == selectedMonth || (b.BillDate >= startOfMonth && b.BillDate < endOfMonth));
                }
                else
                {
                    billQuery = billQuery.Where(b => b.SourceMonth == selectedMonth);
                }
            }

            var bills = await billQuery
                .Select(b => new {
                    b.BillNo,
                    b.BillDate,
                    b.SourceMonth,
                    SalesRep = string.IsNullOrEmpty(b.SalesRep) ? "ไม่ระบุ" : b.SalesRep
                }).ToListAsync();

            var billNos = bills.Select(b => b.BillNo).Distinct().ToList();

            var items = await _db.SalesBillItems
                .Where(i => billNos.Contains(i.BillNo) && i.Amount > 0 && (i.ProductName == null || (!i.ProductName.Contains("แถม") && !i.ProductName.Contains("ชดเชย") && !i.ProductName.Contains("พิเศษ"))))
                .Select(i => new {
                    i.BillNo,
                    i.ProductCode,
                    i.ProductName,
                    i.Qty,
                    i.Unit,
                    i.Price,
                    i.Amount
                }).ToListAsync();

            var deliveringBillNos = await _db.OutstandingDebts
                .Where(d => billNos.Contains(d.BillNo) && d.Status == DebtStatus.Delivering)
                .Select(d => d.BillNo)
                .Distinct()
                .ToListAsync();
            var deliveringSet = new HashSet<string>(deliveringBillNos);

            var allReps = await _db.SalesBills
                .Where(b => !string.IsNullOrEmpty(b.SalesRep))
                .Select(b => b.SalesRep)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            var vm = new ProductDetailsReportViewModel
            {
                MonthKeys = monthKeys,
                Months = monthLabels,
                SalesReps = allReps,
                SelectedSalesRep = selectedRep,
                SelectedMonth = selectedMonth
            };

            var targetReps = string.IsNullOrEmpty(selectedRep) ? allReps : new List<string> { selectedRep };
            var targetMonths = string.IsNullOrEmpty(selectedMonth) ? monthKeys : new List<string> { selectedMonth };

            foreach (var rep in targetReps)
            {
                var repReport = new SalesRepProductReport { SalesRep = rep };

                foreach (var mk in targetMonths)
                {
                    var repMonthBills = bills.Where(b => (b.SalesRep != null && b.SalesRep.Contains(rep.Trim())) && (b.SourceMonth == mk || b.BillDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture) == mk)).Select(b => b.BillNo).ToHashSet();
                    if (!repMonthBills.Any()) continue;

                    var monthItems = items.Where(i => repMonthBills.Contains(i.BillNo)).ToList();
                    if (!monthItems.Any()) continue;

                    var monthMovement = new RepMonthlyProductMovement
                    {
                        MonthKey = mk,
                        MonthNameThai = StandardMonthsMap.ContainsKey(mk) ? StandardMonthsMap[mk] : mk
                    };

                    var groupedProducts = monthItems
                        .GroupBy(i => new { i.ProductCode, i.ProductName, i.Unit, i.Price })
                        .Select(g => {
                            decimal totalQty = g.Sum(x => x.Qty);
                            decimal totalAmt = g.Sum(x => x.Amount);
                            decimal delQty = g.Where(x => deliveringSet.Contains(x.BillNo)).Sum(x => x.Qty);
                            decimal delAmt = g.Where(x => deliveringSet.Contains(x.BillNo)).Sum(x => x.Amount);
                            return new ProductMovementGroup
                            {
                                ProductCode = g.Key.ProductCode,
                                ProductName = g.Key.ProductName,
                                Price = g.Key.Price,
                                Unit = g.Key.Unit,
                                TotalQty = totalQty,
                                TotalAmount = totalAmt,
                                DeliveringQty = delQty,
                                DeliveringAmount = delAmt
                            };
                        })
                        .OrderBy(p => p.ProductCode)
                        .ToList();

                    monthMovement.Products = groupedProducts;
                    repReport.MonthlyMovements.Add(monthMovement);
                }

                if (repReport.MonthlyMovements.Any())
                {
                    vm.RepReports.Add(repReport);
                }
            }

            return vm;
        }

        // Export Matrix Excel
        public async Task<byte[]> ExportMatrixExcelAsync(SalesSummaryMatrixViewModel vm)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("สรุปยอดขาย-ยอดเก็บรายเดือน");

            ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";
            ws.Cells["A1:J1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2"].Value = "รายงานสรุปยอดขายและประสิทธิภาพการเก็บเงินรายผู้แทน (ม.ค. - ส.ค. 2569)";
            ws.Cells["A2:J2"].Merge = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int row = 4;
            ws.Cells[row, 1].Value = "ผู้แทนขาย";
            for (int mi = 0; mi < vm.Months.Count; mi++)
            {
                ws.Cells[row, mi + 2].Value = vm.Months[mi];
            }
            ws.Cells[row, vm.Months.Count + 2].Value = "รวมทั้งสิ้น (YTD)";
            ws.Cells[row, 1, row, vm.Months.Count + 2].Style.Font.Bold = true;
            ws.Cells[row, 1, row, vm.Months.Count + 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, vm.Months.Count + 2].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(26, 41, 66));
            ws.Cells[row, 1, row, vm.Months.Count + 2].Style.Font.Color.SetColor(Color.White);
            ws.Cells[row, 1, row, vm.Months.Count + 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            foreach (var rep in vm.RepRows)
            {
                row++;
                ws.Cells[row, 1].Value = rep.SalesRep;
                for (int mi = 0; mi < rep.MonthlyData.Count; mi++)
                {
                    var m = rep.MonthlyData[mi];
                    ws.Cells[row, mi + 2].Value = $"ขาย: {m.TotalSales:N0}\nเก็บ: {m.TotalCollected:N0}\nค้าง: {m.Outstanding:N0}\n({m.CollectionRate}%)";
                    ws.Cells[row, mi + 2].Style.WrapText = true;
                }
                ws.Cells[row, rep.MonthlyData.Count + 2].Value = $"ขาย: {rep.TotalSales:N0}\nเก็บ: {rep.TotalCollected:N0}\nค้าง: {rep.TotalOutstanding:N0}\n({rep.OverallCollectionRate}%)";
                ws.Cells[row, rep.MonthlyData.Count + 2].Style.WrapText = true;
                ws.Cells[row, rep.MonthlyData.Count + 2].Style.Font.Bold = true;
            }

            // Total row
            row++;
            ws.Cells[row, 1].Value = "รวมทั้งบริษัท (Total)";
            ws.Cells[row, 1].Style.Font.Bold = true;
            for (int mi = 0; mi < vm.TotalCompany.MonthlyData.Count; mi++)
            {
                var m = vm.TotalCompany.MonthlyData[mi];
                ws.Cells[row, mi + 2].Value = $"ขาย: {m.TotalSales:N0}\nเก็บ: {m.TotalCollected:N0}\nค้าง: {m.Outstanding:N0}\n({m.CollectionRate}%)";
                ws.Cells[row, mi + 2].Style.WrapText = true;
                ws.Cells[row, mi + 2].Style.Font.Bold = true;
            }
            ws.Cells[row, vm.TotalCompany.MonthlyData.Count + 2].Value = $"ขาย: {vm.TotalCompany.TotalSales:N0}\nเก็บ: {vm.TotalCompany.TotalCollected:N0}\nค้าง: {vm.TotalCompany.TotalOutstanding:N0}\n({vm.TotalCompany.OverallCollectionRate}%)";
            ws.Cells[row, vm.TotalCompany.MonthlyData.Count + 2].Style.WrapText = true;
            ws.Cells[row, vm.TotalCompany.MonthlyData.Count + 2].Style.Font.Bold = true;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return await pkg.GetAsByteArrayAsync();
        }

        // Export Product Details Excel
        public async Task<byte[]> ExportProductDetailsExcelAsync(ProductDetailsReportViewModel vm)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("รายละเอียดสินค้าที่ขาย");

            ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";
            ws.Cells["A1:G1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2"].Value = "รายงานการเคลื่อนไหวสินค้าแยกตามผู้แทนขายและรายเดือน";
            ws.Cells["A2:G2"].Merge = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int row = 4;
            string[] headers = { "ผู้แทนขาย", "เดือน", "รหัสสินค้า", "ชื่อสินค้า", "จำนวนขายสุทธิ (หน่วย)", "ราคาต่อหน่วย", "ยอดเงินสุทธิ (บาท)" };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cells[row, c + 1].Value = headers[c];
                ws.Cells[row, c + 1].Style.Font.Bold = true;
                ws.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(25, 135, 84));
                ws.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[row, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            foreach (var rep in vm.RepReports)
            {
                foreach (var m in rep.MonthlyMovements)
                {
                    foreach (var p in m.Products)
                    {
                        row++;
                        ws.Cells[row, 1].Value = rep.SalesRep;
                        ws.Cells[row, 2].Value = m.MonthNameThai;
                        ws.Cells[row, 3].Value = p.ProductCode;
                        ws.Cells[row, 4].Value = p.ProductName;
                        ws.Cells[row, 5].Value = $"{p.NetQty:N0} {p.Unit}";
                        ws.Cells[row, 6].Value = p.NetAmount;
                        ws.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
                    }
                }
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return await pkg.GetAsByteArrayAsync();
        }

        public async Task<List<SalesReportData>> GetSalesSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            var billQ = _db.SalesBills.AsQueryable();
            if (from.HasValue) billQ = billQ.Where(b => b.BillDate >= from.Value);
            if (to.HasValue) billQ = billQ.Where(b => b.BillDate <= to.Value);

            var rawBills = await billQ
                .Select(b => new { b.SalesRep, b.TotalAmount })
                .ToListAsync();

            var sales = rawBills
                .GroupBy(b => b.SalesRep)
                .Select(g => new { SalesRep = g.Key, Total = g.Sum(b => b.TotalAmount) })
                .ToList();

            var rawPayments = await _db.PaymentRecords
                .Include(p => p.OutstandingDebt)
                .Select(p => new { SalesRep = p.OutstandingDebt!.SalesRep, p.PaidAmount })
                .ToListAsync();

            var collected = rawPayments
                .GroupBy(p => p.SalesRep)
                .Select(g => new { SalesRep = g.Key, Total = g.Sum(p => p.PaidAmount) })
                .ToList();

            var rawDebts = await _db.OutstandingDebts
                .Where(d => d.Status == DebtStatus.Outstanding || d.Status == DebtStatus.Installment)
                .Select(d => new { d.SalesRep, d.RemainingAmount })
                .ToListAsync();

            var outstanding = rawDebts
                .GroupBy(d => d.SalesRep)
                .Select(g => new { SalesRep = g.Key, Total = g.Sum(d => d.RemainingAmount) })
                .ToList();

            var allReps = sales.Select(s => s.SalesRep)
                .Union(outstanding.Select(o => o.SalesRep))
                .Distinct().ToList();

            return allReps.Select(rep => new SalesReportData
            {
                SalesRep = rep ?? "ไม่ระบุ",
                TotalSales = sales.FirstOrDefault(s => s.SalesRep == rep)?.Total ?? 0,
                TotalCollected = collected.FirstOrDefault(c => c.SalesRep == rep)?.Total ?? 0,
                Outstanding = outstanding.FirstOrDefault(o => o.SalesRep == rep)?.Total ?? 0
            }).OrderBy(r => r.SalesRep).ToList();
        }

        public async Task<byte[]> ExportToExcelAsync(List<SalesReportData> data, DateTime? from, DateTime? to)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("รายงานผู้แทนขาย");

            ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";
            ws.Cells["A1:G1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2"].Value = "รายงานยอดขาย-เก็บเงิน-หนี้คงค้าง แยกตามผู้แทนขาย";
            ws.Cells["A2:G2"].Merge = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            string period = (from.HasValue && to.HasValue) ?
                $"วันที่ {from:dd/MM/yyyy} ถึง {to:dd/MM/yyyy}" : "ทั้งหมด";
            ws.Cells["A3"].Value = $"ช่วงเวลา: {period}";
            ws.Cells["A3:F3"].Merge = true;

            int row = 5;
            string[] headers = { "ลำดับ", "ผู้แทนขาย", "ยอดขาย (บาท)", "ยอดเก็บได้ (บาท)", "หนี้คงค้าง (บาท)", "% เก็บได้" };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cells[row, c + 1].Value = headers[c];
                ws.Cells[row, c + 1].Style.Font.Bold = true;
                ws.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 112, 185));
                ws.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[row, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int idx = 1;
            foreach (var d in data)
            {
                row++;
                decimal pct = d.TotalSales > 0 ? Math.Round(d.TotalCollected / d.TotalSales * 100, 1) : 0;
                ws.Cells[row, 1].Value = idx++;
                ws.Cells[row, 2].Value = d.SalesRep;
                ws.Cells[row, 3].Value = d.TotalSales;
                ws.Cells[row, 4].Value = d.TotalCollected;
                ws.Cells[row, 5].Value = d.Outstanding;
                ws.Cells[row, 6].Value = pct;
                ws.Cells[row, 3, row, 6].Style.Numberformat.Format = "#,##0.00";
                if (idx % 2 == 0)
                {
                    ws.Cells[row, 1, row, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(230, 242, 255));
                }
            }

            row++;
            ws.Cells[row, 1, row, 2].Merge = true;
            ws.Cells[row, 1].Value = "รวมทั้งหมด";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 3].Value = data.Sum(d => d.TotalSales);
            ws.Cells[row, 4].Value = data.Sum(d => d.TotalCollected);
            ws.Cells[row, 5].Value = data.Sum(d => d.Outstanding);
            ws.Cells[row, 3, row, 5].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 3, row, 5].Style.Font.Bold = true;

            row += 2;
            string[] sigTitles = { "ผู้รายงาน", "ผู้ตรวจสอบ", "ผู้มีอำนาจ", "ผู้แทนขาย" };
            for (int s = 0; s < 4; s++)
            {
                int col = s * 2 + 1;
                ws.Cells[row + 2, col].Value = $"({sigTitles[s]})";
                ws.Cells[row + 3, col].Value = $"วันที่.........................";
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            ws.Column(2).Width = 25;

            return await pkg.GetAsByteArrayAsync();
        }

        public async Task<List<WaitingGoodsData>> GetWaitingGoodsAsync()
        {
            var debts = await _db.OutstandingDebts
                .Where(d => d.Status == DebtStatus.WaitingGoods)
                .OrderBy(d => d.BillDate)
                .ToListAsync();

            var billNos = debts.Select(d => d.BillNo).Distinct().ToList();
            var pendingProducts = await _db.PendingProducts
                .Where(p => billNos.Contains(p.BillNo))
                .ToListAsync();
            var billItems = await _db.SalesBillItems
                .Where(p => billNos.Contains(p.BillNo))
                .ToListAsync();

            var result = new List<WaitingGoodsData>();
            foreach(var d in debts)
            {
                var debtPending = pendingProducts.Where(p => p.OutstandingDebtId == d.Id || p.BillNo == d.BillNo).ToList();
                var displayProducts = new List<SalesBillItem>();

                if (debtPending.Any())
                {
                    foreach (var pp in debtPending)
                    {
                        var matchedItem = billItems.FirstOrDefault(i => i.BillNo == d.BillNo && i.ProductCode == pp.ProductCode);
                        displayProducts.Add(new SalesBillItem
                        {
                            BillNo = d.BillNo,
                            ProductCode = pp.ProductCode,
                            ProductName = pp.ProductName,
                            Qty = pp.Quantity,
                            Unit = matchedItem?.Unit ?? "ชิ้น",
                            Price = matchedItem?.Price ?? 0,
                            Amount = (matchedItem?.Price ?? 0) * pp.Quantity
                        });
                    }
                }
                else
                {
                    displayProducts = billItems.Where(p => p.BillNo == d.BillNo).ToList();
                }

                result.Add(new WaitingGoodsData
                {
                    BillNo = d.BillNo,
                    BillDate = d.BillDate,
                    CustomerName = d.CustomerName,
                    District = d.District,
                    Province = d.Province,
                    SalesRep = d.SalesRep,
                    Products = displayProducts
                });
            }
            return result;
        }
        public async Task<CustomerProductViewModel> GetCustomerProductReportAsync(string? selectedRep, string? selectedMonth, DateTime? selectedDate, string? q = null)
        {
            var vm = new CustomerProductViewModel
            {
                SearchQuery = q,
                SelectedRep = selectedRep,
                SelectedMonth = selectedMonth,
                SelectedDate = selectedDate,
                AllReps = await _db.SalesBills.Where(b => b.SalesRep != null && b.SalesRep != "").Select(b => b.SalesRep).Distinct().OrderBy(x => x).ToListAsync(),
                AllMonths = StandardMonthsMap
            };

            var query = _db.SalesBillItems
                .Include(i => i.SalesBill)
                .AsQueryable()
                .Where(i => i.Price > 0);

            if (!string.IsNullOrEmpty(selectedRep))
                query = query.Where(i => i.SalesBill.SalesRep == selectedRep);

            if (selectedDate.HasValue)
                query = query.Where(i => i.SalesBill.BillDate.Date == selectedDate.Value.Date);

            if (!string.IsNullOrEmpty(selectedMonth))
            {
                if (DateTime.TryParse(selectedMonth + "-01", out var mDate))
                {
                    var startOfMonth = new DateTime(mDate.Year, mDate.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1);
                    query = query.Where(i => i.SalesBill.SourceMonth == selectedMonth || (i.SalesBill.BillDate >= startOfMonth && i.SalesBill.BillDate < endOfMonth));
                }
                else
                {
                    query = query.Where(i => i.SalesBill.SourceMonth == selectedMonth);
                }
            }

            var items = await query.ToListAsync();

            // Apply search query (in-memory filter)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.Trim().ToLower();
                items = items.Where(i =>
                    (i.SalesBill.CustomerCode != null && i.SalesBill.CustomerCode.ToLower().Contains(qLower)) ||
                    (i.SalesBill.CustomerName != null && i.SalesBill.CustomerName.ToLower().Contains(qLower)) ||
                    (i.ProductCode != null && i.ProductCode.ToLower().Contains(qLower)) ||
                    (i.ProductName != null && i.ProductName.ToLower().Contains(qLower))
                ).ToList();
            }

            var allCustomers = await _db.Customers.ToDictionaryAsync(c => c.CustomerCode ?? "", c => c.Name ?? "");
            var billCustomers = await _db.SalesBills
                .Where(b => !string.IsNullOrEmpty(b.CustomerCode) && !string.IsNullOrEmpty(b.CustomerName))
                .Select(b => new { b.CustomerCode, b.CustomerName })
                .Distinct()
                .ToListAsync();
            foreach(var bc in billCustomers)
            {
                if (!string.IsNullOrEmpty(bc.CustomerCode) && !allCustomers.ContainsKey(bc.CustomerCode))
                {
                    allCustomers[bc.CustomerCode] = bc.CustomerName ?? "";
                }
            }


            var grouped = items.GroupBy(i => new { 
                    CustCode = i.SalesBill.CustomerCode,
                    Cust = i.SalesBill.CustomerName, 
                    Rep = i.SalesBill.SalesRep, 
                    Code = i.ProductCode, 
                    Name = i.ProductName, 
                    Price = i.Price
                })
                .Select(g => {
                    var uniqueMonths = g.Select(x => new { x.SalesBill.BillDate.Year, x.SalesBill.BillDate.Month })
                                        .Distinct()
                                        .OrderBy(m => m.Year).ThenBy(m => m.Month)
                                        .Select(m => $"{m.Month:D2}/{m.Year}")
                                        .ToList();
                    string monthDisplay = "";
                    if (uniqueMonths.Count == 1) monthDisplay = uniqueMonths[0];
                    else if (uniqueMonths.Count > 1) monthDisplay = uniqueMonths.First() + "-" + uniqueMonths.Last();
                    
                    string cName = g.Key.Cust ?? "";
                    if (string.IsNullOrWhiteSpace(cName) && allCustomers.TryGetValue(g.Key.CustCode ?? "", out var dbName))
                    {
                        cName = dbName;
                    }
                    
                    return new CustomerProductItem
                    {
                        Month = monthDisplay,
                        CustomerCode = g.Key.CustCode ?? "",
                        CustomerName = cName,
                        SalesRep = g.Key.Rep ?? "",
                        ProductCode = g.Key.Code ?? "",
                        ProductName = g.Key.Name ?? "",
                        Price = g.Key.Price,
                        Credit = g.First().SalesBill.Credit,
                        Qty = g.Sum(x => x.Qty),
                        TotalAmount = g.Sum(x => x.Amount)
                    };
                })
                .OrderBy(x => x.CustomerName).ThenBy(x => x.ProductName)
                .ToList();

            vm.Items = grouped;
            return vm;
        }

        public async Task<byte[]> ExportCustomerProductExcelAsync(CustomerProductViewModel data)
        {
            using var pkg = new OfficeOpenXml.ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("CustomerSales");

            // Headers
            var headers = new[] { "เน€เธเธ…เน€เธเธ“เน€เธโ€เน€เธเธ‘เน€เธย", "เน€เธโฌเน€เธโ€เน€เธเธ—เน€เธเธเน€เธย", "เน€เธเธเน€เธเธเน€เธเธ‘เน€เธเธเน€เธเธ…เน€เธเธเน€เธยเน€เธยเน€เธยเน€เธเธ’", "เน€เธยเน€เธเธ—เน€เธยเน€เธเธเน€เธเธ…เน€เธเธเน€เธยเน€เธยเน€เธยเน€เธเธ’", "เน€เธเธเน€เธเธเน€เธเธ‘เน€เธเธเน€เธเธเน€เธเธ”เน€เธยเน€เธยเน€เธยเน€เธเธ’ : เน€เธยเน€เธเธ—เน€เธยเน€เธเธเน€เธเธเน€เธเธ”เน€เธยเน€เธยเน€เธยเน€เธเธ’", "เน€เธเธเน€เธเธ’เน€เธยเน€เธเธ’เน€เธโ€ขเน€เธยเน€เธเธเน€เธเธเน€เธยเน€เธยเน€เธเธเน€เธเธ", "เน€เธโฌเน€เธยเน€เธเธเน€เธโ€เน€เธเธ”เน€เธโ€ข(เน€เธเธเน€เธเธ‘เน€เธย)", "เน€เธยเน€เธเธ—เน€เธยเน€เธเธเน€เธยเน€เธเธเน€เธยเน€เธยเน€เธโ€”เน€เธยเน€เธยเน€เธเธ’เน€เธเธ" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
            }

            int row = 2;
            int idx = 1;
            foreach (var item in data.Items)
            {
                ws.Cells[row, 1].Value = idx++;
                ws.Cells[row, 2].Value = item.Month;
                ws.Cells[row, 3].Value = item.CustomerCode;
                ws.Cells[row, 4].Value = item.CustomerName;
                ws.Cells[row, 5].Value = $"{item.ProductCode} : {item.ProductName}";
                ws.Cells[row, 6].Value = item.Price;
                ws.Cells[row, 7].Value = item.Credit;
                ws.Cells[row, 8].Value = item.SalesRep;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return await pkg.GetAsByteArrayAsync();
        }
        public async Task<CustomerPurchaseSummaryViewModel> GetCustomerPurchaseSummaryAsync(string selectedRep, string selectedMonth)
        {
            var vm = new CustomerPurchaseSummaryViewModel();
            vm.MonthKeys = StandardMonthsMap.Keys.ToList();
            vm.Months = StandardMonthsMap.Values.ToList();
            vm.SalesReps = await _db.SalesBills.Where(b => b.SalesRep != null && b.SalesRep != "").Select(b => b.SalesRep).Distinct().OrderBy(x => x).ToListAsync();
            vm.SelectedSalesRep = selectedRep;
            vm.SelectedMonth = selectedMonth;
            // Query SalesBillItems directly (Price > 0 excludes free/bonus items)
            var itemQuery = _db.SalesBillItems
                .Include(i => i.SalesBill)
                .Where(i => i.Price > 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(selectedRep))
                itemQuery = itemQuery.Where(i => i.SalesBill.SalesRep == selectedRep);

            if (!string.IsNullOrEmpty(selectedMonth))
            {
                if (DateTime.TryParse(selectedMonth + "-01", out var mDate))
                {
                    var startOfMonth = new DateTime(mDate.Year, mDate.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1);
                    itemQuery = itemQuery.Where(i => i.SalesBill.SourceMonth == selectedMonth ||
                        (i.SalesBill.BillDate >= startOfMonth && i.SalesBill.BillDate < endOfMonth));
                }
                else
                {
                    itemQuery = itemQuery.Where(i => i.SalesBill.SourceMonth == selectedMonth);
                }
            }

            var allItems = await itemQuery.ToListAsync();

            // Group by Customer + Product + UnitPrice (price split logic - separate rows per price)
            vm.Rows = allItems
                .GroupBy(x => new {
                    CustomerId = x.SalesBill.CustomerCode ?? "",
                    CustomerName = x.SalesBill.CustomerName ?? "",
                    ProductCode = x.ProductCode ?? "",
                    ProductName = x.ProductName ?? "",
                    Unit = x.Unit ?? "",
                    UnitPrice = x.Price
                })
                .Select(g => new CustomerPurchaseSummaryRow {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    Unit = g.Key.Unit,
                    UnitPrice = g.Key.UnitPrice,
                    Quantity = g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.Qty * x.Price)
                })
                .OrderBy(r => r.CustomerId).ThenBy(r => r.ProductCode).ThenBy(r => r.UnitPrice)
                .ToList();

            return vm;
        }
    }

    public class CustomerProductViewModel
    {
        public string? SearchQuery { get; set; }
        public string? SelectedRep { get; set; }
        public string? SelectedMonth { get; set; }
        public DateTime? SelectedDate { get; set; }
        public List<string> AllReps { get; set; } = new();
        public Dictionary<string, string> AllMonths { get; set; } = new();
        public List<CustomerProductItem> Items { get; set; } = new();
    }

    public class CustomerProductItem
    {
        public string Month { get; set; } = "";
        public string CustomerCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string SalesRep { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Credit { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }
    public class CustomerPurchaseSummaryRow
    {
        public string CustomerId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class CustomerPurchaseSummaryViewModel
    {
        public List<string> Months { get; set; } = new();
        public List<string> MonthKeys { get; set; } = new();
        public List<string> SalesReps { get; set; } = new();
        public string SelectedMonth { get; set; } = "";
        public string SelectedSalesRep { get; set; } = "";
        public List<CustomerPurchaseSummaryRow> Rows { get; set; } = new();
    }
}









