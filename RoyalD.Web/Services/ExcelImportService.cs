using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoyalD.Web.Models;

namespace RoyalD.Web.Services
{
    public class ExcelImportService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(AppDbContext db, ILogger<ExcelImportService> logger)
        {
            _db = db;
            _logger = logger;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        private int FindHeaderRow(DataTable tbl, out Dictionary<string, int> map, params string[] requiredKeywords)
        {
            map = new Dictionary<string, int>();
            for (int r = 0; r < Math.Min(15, tbl.Rows.Count); r++)
            {
                var rowMap = new Dictionary<string, int>();
                int matchCount = 0;
                for (int c = 0; c < tbl.Columns.Count; c++)
                {
                    var val = tbl.Rows[r][c]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(val)) continue;
                    rowMap[val] = c;
                    if (requiredKeywords.Any(k => val.Contains(k))) matchCount++;
                }
                
                if (matchCount >= requiredKeywords.Length - 1 && matchCount > 0)
                {
                    map = rowMap;
                    return r;
                }
            }
            return -1;
        }

        private int GetCol(Dictionary<string, int> map, params string[] keywords)
        {
            foreach (var k in keywords)
            {
                var match = map.FirstOrDefault(x => x.Key.Contains(k));
                if (match.Key != null) return match.Value;
            }
            return -1;
        }

        public async Task<(int inserted, int updated)> ImportSalesBillAsync(Stream stream, string sourceMonth, bool isCurrentMonth = false, string fileName = "DirectUpload")
        {
            var p = await PreviewSalesBillAsync(stream, sourceMonth, isCurrentMonth, fileName);
            int inserted = 0, updated = 0;
            var billNos = p.Items.Select(b => b.BillNo).Distinct().ToList();
            var existingBills = await _db.SalesBills.Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

            foreach (var b in p.Items.GroupBy(x => x.BillNo).Select(g => g.First()))
            {
                if (existingBills.TryGetValue(b.BillNo, out var ex))
                {
                    ex.CustomerName = b.CustomerName != null && b.CustomerName.Length > 100 ? b.CustomerName.Substring(0, 100) : (b.CustomerName ?? "");
                    ex.TotalAmount = b.TotalAmount;
                    
                    ex.SourceMonth = sourceMonth != null && sourceMonth.Length > 10 ? sourceMonth.Substring(0, 10) : (sourceMonth ?? "");
                    
                    ex.PoNumber = b.PoNumber != null && b.PoNumber.Length > 100 ? b.PoNumber.Substring(0, 100) : (b.PoNumber ?? "");
                    
                    updated++;
                }
                else
                {
                    _db.SalesBills.Add(new SalesBill
                    {
                        BillNo = b.BillNo != null && b.BillNo.Length > 50 ? b.BillNo.Substring(0, 50) : (b.BillNo ?? ""),
                        BillDate = b.BillDate,
                        CustomerCode = b.CustomerCode != null && b.CustomerCode.Length > 20 ? b.CustomerCode.Substring(0, 20) : (b.CustomerCode ?? ""),
                        CustomerName = b.CustomerName != null && b.CustomerName.Length > 100 ? b.CustomerName.Substring(0, 100) : (b.CustomerName ?? ""),
                        District = b.District != null && b.District.Length > 100 ? b.District.Substring(0, 100) : (b.District ?? ""),
                        Province = b.Province != null && b.Province.Length > 100 ? b.Province.Substring(0, 100) : (b.Province ?? ""),
                        Phone = b.Phone != null && b.Phone.Length > 50 ? b.Phone.Substring(0, 50) : (b.Phone ?? ""),
                        Credit = b.Credit,
                        SalesRep = b.SalesRep != null && b.SalesRep.Length > 100 ? b.SalesRep.Substring(0, 100) : (b.SalesRep ?? ""),
                        TotalAmount = b.TotalAmount,
                        SourceMonth = sourceMonth != null && sourceMonth.Length > 10 ? sourceMonth.Substring(0, 10) : (sourceMonth ?? ""),
                        PoNumber = b.PoNumber != null && b.PoNumber.Length > 100 ? b.PoNumber.Substring(0, 100) : (b.PoNumber ?? ""),
                        Items = b.Items ?? new List<SalesBillItem>()
                    });
                    inserted++;
                }
                if ((inserted + updated) % 200 == 0)
                {
                    await _db.SaveChangesAsync();
                }
            }
            await _db.SaveChangesAsync();
            return (inserted, updated);
        }

        public async Task<(int inserted, int updated, int skipped)> ConfirmImportSalesBillAsync(string previewId, bool updateDuplicates = true, bool skipDuplicates = false)
        {
            var p = GetPreview(previewId);
            if (p == null) return (0, 0, 0);
            
            int inserted = 0, updated = 0, skipped = 0;
            var billNos = p.Items.Select(b => b.BillNo).Distinct().ToList();
            var existingBills = await _db.SalesBills.Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

            foreach (var b in p.Items.GroupBy(x => x.BillNo).Select(g => g.First()))
            {
                if (existingBills.TryGetValue(b.BillNo, out var ex))
                {
                    if (skipDuplicates) { skipped++; continue; }
                    if (updateDuplicates)
                    {
                        ex.CustomerName = b.CustomerName != null && b.CustomerName.Length > 100 ? b.CustomerName.Substring(0, 100) : (b.CustomerName ?? "");
                        ex.District = b.District != null && b.District.Length > 100 ? b.District.Substring(0, 100) : (b.District ?? "");
                        ex.Province = b.Province != null && b.Province.Length > 100 ? b.Province.Substring(0, 100) : (b.Province ?? "");
                        ex.SalesRep = b.SalesRep != null && b.SalesRep.Length > 100 ? b.SalesRep.Substring(0, 100) : (b.SalesRep ?? "");
                        ex.Phone = b.Phone != null && b.Phone.Length > 50 ? b.Phone.Substring(0, 50) : (b.Phone ?? "");
                        ex.TotalAmount = b.TotalAmount;
                        
                        ex.SourceMonth = p.FileType != null && p.FileType.Length > 10 ? p.FileType.Substring(0, 10) : (p.FileType ?? "");
                        
                        ex.PoNumber = b.PoNumber != null && b.PoNumber.Length > 100 ? b.PoNumber.Substring(0, 100) : (b.PoNumber ?? "");
                        
                        updated++;
                    }
                }
                else
                {
                    _db.SalesBills.Add(new SalesBill
                    {
                        BillNo = b.BillNo != null && b.BillNo.Length > 50 ? b.BillNo.Substring(0, 50) : (b.BillNo ?? ""),
                        BillDate = b.BillDate,
                        CustomerCode = b.CustomerCode != null && b.CustomerCode.Length > 20 ? b.CustomerCode.Substring(0, 20) : (b.CustomerCode ?? ""),
                        CustomerName = b.CustomerName != null && b.CustomerName.Length > 100 ? b.CustomerName.Substring(0, 100) : (b.CustomerName ?? ""),
                        District = b.District != null && b.District.Length > 100 ? b.District.Substring(0, 100) : (b.District ?? ""),
                        Province = b.Province != null && b.Province.Length > 100 ? b.Province.Substring(0, 100) : (b.Province ?? ""),
                        Phone = b.Phone != null && b.Phone.Length > 50 ? b.Phone.Substring(0, 50) : (b.Phone ?? ""),
                        Credit = b.Credit,
                        SalesRep = b.SalesRep != null && b.SalesRep.Length > 100 ? b.SalesRep.Substring(0, 100) : (b.SalesRep ?? ""),
                        TotalAmount = b.TotalAmount,
                        SourceMonth = p.FileType != null && p.FileType.Length > 10 ? p.FileType.Substring(0, 10) : (p.FileType ?? ""),
                        PoNumber = b.PoNumber != null && b.PoNumber.Length > 100 ? b.PoNumber.Substring(0, 100) : (b.PoNumber ?? ""),
                        Items = b.Items ?? new List<SalesBillItem>()
                    });
                    inserted++;
                }
                if ((inserted + updated) % 200 == 0) await _db.SaveChangesAsync();
            }
            await _db.SaveChangesAsync();
            RemovePreview(previewId);
            return (inserted, updated, skipped);
        }

        public async Task<int> ImportOutstandingDebtsAsync(Stream stream, string fileName = "")
        {
            int count = 0;
            using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                ? ExcelReaderFactory.CreateCsvReader(stream) 
                : ExcelReaderFactory.CreateReader(stream);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null) return 0;

            try
            {
                _db.OutstandingDebts.RemoveRange(_db.OutstandingDebts.Where(d => d.Status == DebtStatus.Outstanding));
                await _db.SaveChangesAsync();

                int headerRow = FindHeaderRow(tbl, out var map, "รหัส", "ชื่อ", "บิล", "จำนวนเงิน");
                if (headerRow < 0) headerRow = 3;

                int cCustCode = GetCol(map, "รหัสลูกค้า", "รหัส");
                int cCustName = GetCol(map, "ชื่อ");
                int cDistrict = GetCol(map, "อำเภอ");
                int cProvince = GetCol(map, "จังหวัด");
                int cBillNo = GetCol(map, "บิล", "เลขที่");
                int cBillDate = GetCol(map, "วันที่บิล", "วันที่");
                int cDueDate = GetCol(map, "กำหนดชำระ");
                int cAmount = GetCol(map, "จำนวนเงิน", "ยอดสุทธิ", "ยอดหนี้");
                int cCredit = GetCol(map, "เครดิต");
                int cSalesRep = GetCol(map, "ผู้แทน");

                if (cCustCode < 0) cCustCode = 0;
                if (cCustName < 0) cCustName = 1;
                if (cBillNo < 0) cBillNo = 6;
                if (cBillDate < 0) cBillDate = 7;
                if (cDueDate < 0) cDueDate = 8;
                if (cAmount < 0) cAmount = 9;

                string currentCustCode = "";
                string currentCustName = "";
                string currentDistrict = "";
                string currentProvince = "";
                string currentSalesRep = "";

                var existingCustCodes = new HashSet<string>(_db.Customers.Select(c => c.CustomerCode).ToList());

                for (int r = headerRow + 1; r < tbl.Rows.Count; r++)
                {
                    var row = tbl.Rows[r];
                    var colCust = cCustCode >= 0 && cCustCode < tbl.Columns.Count ? row[cCustCode]?.ToString()?.Trim() ?? "" : "";
                    var colBill = cBillNo >= 0 && cBillNo < tbl.Columns.Count ? row[cBillNo]?.ToString()?.Trim() ?? "" : "";
                    
                    if (string.IsNullOrWhiteSpace(colCust) && string.IsNullOrWhiteSpace(colBill)) continue;
                    if (colCust.Contains("รวม") || colCust.Contains("ทั้งหมด")) continue;

                    if (!string.IsNullOrEmpty(colCust) && !colCust.Contains("/"))
                    {
                        currentCustCode = colCust;
                        currentCustName = cCustName >= 0 && cCustName < tbl.Columns.Count ? row[cCustName]?.ToString()?.Trim() ?? "" : "";
                        
                        string dynDist = "";
                        string dynProv = "";
                        string dynRep = "";
                        
                        var strParts = new List<string>();
                        for (int i = 4; i < tbl.Columns.Count; i++) {
                            var v = row[i]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(v) && !decimal.TryParse(v.Replace(",", ""), out _) && !DateTime.TryParse(v, out _) && !v.Contains("/")) {
                                strParts.Add(v);
                            }
                        }
                        if (strParts.Count > 0) dynRep = strParts.Last();
                        if (strParts.Count >= 3) { dynDist = strParts[0]; dynProv = strParts[1]; }
                        else if (strParts.Count == 2) { 
                            if (strParts[0].Contains("จ.") || strParts[0].Contains("กรุงเทพ")) dynProv = strParts[0];
                            else dynDist = strParts[0];
                        }

                        currentDistrict = cDistrict >= 0 && cDistrict < tbl.Columns.Count && !string.IsNullOrWhiteSpace(row[cDistrict]?.ToString()) ? row[cDistrict].ToString().Trim() : dynDist;
                        currentProvince = cProvince >= 0 && cProvince < tbl.Columns.Count && !string.IsNullOrWhiteSpace(row[cProvince]?.ToString()) ? row[cProvince].ToString().Trim() : dynProv;
                        currentSalesRep = cSalesRep >= 0 && cSalesRep < tbl.Columns.Count && !string.IsNullOrWhiteSpace(row[cSalesRep]?.ToString()) ? row[cSalesRep].ToString().Trim() : dynRep;

                        if (!string.IsNullOrEmpty(currentCustCode) && !existingCustCodes.Contains(currentCustCode))
                        {
                            _db.Customers.Add(new Customer { CustomerCode = currentCustCode, Name = currentCustName, District = currentDistrict, Province = currentProvince });
                            existingCustCodes.Add(currentCustCode);
                        }
                    }

                    string billNo = colBill;
                    if (string.IsNullOrEmpty(billNo) && colCust.Contains("/")) billNo = colCust;
                    if (string.IsNullOrEmpty(billNo) && cBillDate >= 0 && cBillDate < tbl.Columns.Count && (row[cBillDate]?.ToString() ?? "").Contains("/")) 
                    {
                        if (!(row[cBillDate]?.ToString() ?? "").Contains("202") && !(row[cBillDate]?.ToString() ?? "").Contains("256"))
                            billNo = row[cBillDate]?.ToString()?.Trim() ?? "";
                    }

                    if (!string.IsNullOrEmpty(billNo) && !billNo.Contains("ยอดยกมา") && !billNo.Contains("ยอดรวม")) {
                        var billDate = ParseDate(cBillDate >= 0 && cBillDate < tbl.Columns.Count ? row[cBillDate]?.ToString() : "");
                        var dueDate = ParseDate(cDueDate >= 0 && cDueDate < tbl.Columns.Count ? row[cDueDate]?.ToString() : "");
                        var amount = ParseDecimal(cAmount >= 0 && cAmount < tbl.Columns.Count ? row[cAmount]?.ToString() : "");
                        var credit = ParseInt(cCredit >= 0 && cCredit < tbl.Columns.Count ? row[cCredit]?.ToString() : "");

                        if (amount <= 0) continue;

                        if (currentDistrict.Length > 100) currentDistrict = currentDistrict.Substring(0, 100);
                        if (currentProvince.Length > 100) currentProvince = currentProvince.Substring(0, 100);
                        if (currentSalesRep.Length > 100) currentSalesRep = currentSalesRep.Substring(0, 100);
                        if (currentCustName.Length > 100) currentCustName = currentCustName.Substring(0, 100);
                        if (currentCustCode.Length > 20) currentCustCode = currentCustCode.Substring(0, 20);
                        if (billNo.Length > 50) billNo = billNo.Substring(0, 50);

                        _db.OutstandingDebts.Add(new OutstandingDebt
                        {
                            CustomerCode = currentCustCode, CustomerName = currentCustName,
                            District = currentDistrict, Province = currentProvince,
                            BillNo = billNo, BillDate = billDate, DueDate = dueDate,
                            OriginalAmount = amount, RemainingAmount = amount,
                            Credit = credit, SalesRep = currentSalesRep, Status = DebtStatus.Outstanding
                        });
                        count++;
                        if (count % 200 == 0)
                        {
                            try { await _db.SaveChangesAsync(); } catch { _db.ChangeTracker.Clear(); }
                        }
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) { _logger.LogError(ex.Message); throw; }
            return count;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ImportPreviewResult> _previewCache = new();
        public static void SetPreview(string id, ImportPreviewResult preview) => _previewCache[id] = preview;
        public static ImportPreviewResult? GetPreview(string id) => _previewCache.TryGetValue(id, out var p) ? p : null;
        public static void RemovePreview(string id) => _previewCache.TryRemove(id, out _);

        public async Task<ImportPreviewResult> PreviewSalesBillAsync(Stream stream, string sourceMonth, bool isCurrentMonth, string fileName)
        {
            var result = new ImportPreviewResult { FileType = sourceMonth, FileName = fileName, IsCurrentMonth = isCurrentMonth };
            using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                ? ExcelReaderFactory.CreateCsvReader(stream) 
                : ExcelReaderFactory.CreateReader(stream);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null || tbl.Rows.Count < 4) return result;

            int headerRow = FindHeaderRow(tbl, out var map, "บิล", "วันที่", "รหัส", "ชื่อ");
            if (headerRow < 0) headerRow = 3;

            int cBillNo = GetCol(map, "บิล", "เลขที่");
            int cBillDate = GetCol(map, "วันที่");
            int cCustCode = GetCol(map, "รหัส");
            int cPo = GetCol(map, "PO", "ใบสั่งซื้อ");
            int cCustName = GetCol(map, "ชื่อ");
            int cDistrict = GetCol(map, "อำเภอ");
            int cProvince = GetCol(map, "จังหวัด");
            int cPhone = GetCol(map, "โทร");
            int cCredit = GetCol(map, "เครดิต");
            int cQty = GetCol(map, "จำนวน");
            int cUnit = GetCol(map, "หน่วย");
            int cPrice = GetCol(map, "ราคา");
            int cDiscount = GetCol(map, "ส่วนลด");
            int cAmount = GetCol(map, "จำนวนเงิน", "ยอดสุทธิ");
            int cSalesRep = GetCol(map, "ผู้แทน");

            if (cBillNo < 0) cBillNo = 0;
            if (cQty < 0) cQty = 12;
            if (cPrice < 0) cPrice = 14;
            if (cAmount < 0) cAmount = 16;

            var parsedBills = new List<BillPreviewItem>();
            string currentBillNo = "";
            DateTime currentBillDate = DateTime.MinValue;
            string currentCustCode = "", currentCustName = "", currentPoNumber = "", currentDistrict = "", currentProvince = "", currentPhone = "", currentSalesRep = "";
            int currentCredit = 0;
            decimal currentTotal = 0;
            var currentItems = new List<SalesBillItem>();

            void CollectCurrentBill()
            {
                if (string.IsNullOrEmpty(currentBillNo)) return;
                
                // Prevent EF Core / Postgres 22001 value too long
                if (currentDistrict.Length > 100) currentDistrict = currentDistrict.Substring(0, 100);
                if (currentProvince.Length > 100) currentProvince = currentProvince.Substring(0, 100);
                if (currentSalesRep.Length > 100) currentSalesRep = currentSalesRep.Substring(0, 100);
                if (currentPoNumber.Length > 100) currentPoNumber = currentPoNumber.Substring(0, 100);
                if (currentPhone.Length > 50) currentPhone = currentPhone.Substring(0, 50);
                if (currentCustName.Length > 100) currentCustName = currentCustName.Substring(0, 100);
                if (currentCustCode.Length > 20) currentCustCode = currentCustCode.Substring(0, 20);
                if (currentBillNo.Length > 50) currentBillNo = currentBillNo.Substring(0, 50);

                parsedBills.Add(new BillPreviewItem
                {
                    BillNo = currentBillNo, BillDate = currentBillDate, CustomerCode = currentCustCode,
                    CustomerName = currentCustName, District = currentDistrict, Province = currentProvince,
                    Phone = currentPhone, Credit = currentCredit, SalesRep = currentSalesRep,
                    TotalAmount = currentTotal, ItemCount = currentItems.Count,
                    Items = new List<SalesBillItem>(currentItems), PoNumber = currentPoNumber
                });
            }

            for (int r = headerRow + 1; r < tbl.Rows.Count; r++)
            {
                var row = tbl.Rows[r];
                var col0 = row[0]?.ToString()?.Trim() ?? "";
                var col1 = tbl.Columns.Count > 1 ? row[1]?.ToString()?.Trim() ?? "" : "";
                var col2 = tbl.Columns.Count > 2 ? row[2]?.ToString()?.Trim() ?? "" : "";
                
                string mainStr = !string.IsNullOrEmpty(col0) ? col0 : (!string.IsNullOrEmpty(col1) ? col1 : col2);
                if (string.IsNullOrWhiteSpace(mainStr)) continue;
                if (mainStr.Contains("รวม") || mainStr.Contains("VAT") || mainStr.Contains("ภาษี")) continue;
                if (mainStr.StartsWith("S/N", StringComparison.OrdinalIgnoreCase)) continue;

                string newBillNo = cBillNo >= 0 && cBillNo < tbl.Columns.Count && !string.IsNullOrEmpty(row[cBillNo]?.ToString()) ? row[cBillNo]?.ToString()?.Trim() ?? "" : mainStr;
                bool isSameBill = !string.IsNullOrEmpty(currentBillNo) && newBillNo == currentBillNo;
                
                bool isBillHeader = false;
                if (!isSameBill) {
                    string checkStr = newBillNo.Trim().ToUpper();
                    if (!DateTime.TryParse(checkStr, out _)) {
                        if ((checkStr.Length > 0 && char.IsDigit(checkStr[0]) && checkStr.Contains("/")) ||
                            checkStr.StartsWith("R") || checkStr.StartsWith("B") || checkStr.StartsWith("INV")) {
                            isBillHeader = true;
                        }
                    }
                }

                if (isBillHeader)
                {
                    CollectCurrentBill();
                    currentItems = new List<SalesBillItem>();

                    currentBillNo = cBillNo >= 0 && cBillNo < tbl.Columns.Count && !string.IsNullOrEmpty(row[cBillNo]?.ToString()) ? row[cBillNo]?.ToString()?.Trim() ?? "" : mainStr;
                    currentBillDate = ParseDate(cBillDate >= 0 && cBillDate < tbl.Columns.Count && !string.IsNullOrEmpty(row[cBillDate]?.ToString()) ? row[cBillDate]?.ToString() : col1);
                    currentCustCode = cCustCode >= 0 && cCustCode < tbl.Columns.Count && !string.IsNullOrEmpty(row[cCustCode]?.ToString()) ? row[cCustCode]?.ToString()?.Trim() ?? "" : col2;
                    
                    var col3 = tbl.Columns.Count > 3 ? row[3]?.ToString()?.Trim() ?? "" : "";
                    var col4 = tbl.Columns.Count > 4 ? row[4]?.ToString()?.Trim() ?? "" : "";
                    currentPoNumber = cPo >= 0 && cPo < tbl.Columns.Count ? row[cPo]?.ToString()?.Trim() ?? "" : "";
                    for (int i = 2; i <= 6; i++)
                    {
                        if (i < tbl.Columns.Count)
                        {
                            var cellVal = row[i]?.ToString()?.Trim() ?? "";
                            if (cellVal.StartsWith("PO", StringComparison.OrdinalIgnoreCase) || cellVal.StartsWith("ใบสั่ง", StringComparison.OrdinalIgnoreCase))
                            {
                                currentPoNumber = cellVal;
                                if (i == 3) col3 = col4;
                                break;
                            }
                        }
                    }
                    currentCustName = cCustName >= 0 && cCustName < tbl.Columns.Count && !string.IsNullOrEmpty(row[cCustName]?.ToString()) ? row[cCustName]?.ToString()?.Trim() ?? "" : col3;

                    string dynDist = "";
                    string dynProv = "";
                    string dynRep = "";

                    var strParts = new List<string>();
                    for (int i = 5; i < tbl.Columns.Count; i++)
                    {
                        var val = row[i]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(val) && !decimal.TryParse(val.Replace(",", ""), out _))
                        {
                            strParts.Add(val);
                        }
                    }

                    if (strParts.Count > 0)
                    {
                        dynRep = strParts.Last();
                    }
                    
                    if (strParts.Count >= 3)
                    {
                        dynDist = strParts[0];
                        dynProv = strParts[1];
                    }
                    else if (strParts.Count == 2)
                    {
                        if (strParts[0].Contains("จ.") || strParts[0].Contains("กรุงเทพ"))
                        {
                            dynProv = strParts[0];
                        }
                        else
                        {
                            dynDist = strParts[0];
                        }
                    }

                    currentDistrict = cDistrict >= 0 && cDistrict < tbl.Columns.Count && !string.IsNullOrWhiteSpace(row[cDistrict]?.ToString()) ? row[cDistrict].ToString().Trim() : dynDist;
                    currentProvince = cProvince >= 0 && cProvince < tbl.Columns.Count && !string.IsNullOrWhiteSpace(row[cProvince]?.ToString()) ? row[cProvince].ToString().Trim() : dynProv;

                    currentPhone = cPhone >= 0 && cPhone < tbl.Columns.Count ? row[cPhone]?.ToString()?.Trim() ?? "" : "";
                    currentCredit = ParseInt(cCredit >= 0 && cCredit < tbl.Columns.Count && !string.IsNullOrEmpty(row[cCredit]?.ToString()) ? row[cCredit]?.ToString() : (tbl.Columns.Count > 11 ? row[11]?.ToString() : ""));
                    
                    currentSalesRep = cSalesRep >= 0 && cSalesRep < tbl.Columns.Count && !string.IsNullOrWhiteSpace(row[cSalesRep]?.ToString()) ? row[cSalesRep].ToString().Trim() : dynRep;
                    
                    currentTotal = 0;
                }
                else
                {
                    var prodName = mainStr;
                    var parts = prodName.Split(new[] { "  " }, 2, StringSplitOptions.RemoveEmptyEntries);
                    
                    decimal qty = 0, price = 0, amt = 0;
                    if (cQty >= 0 && cQty < tbl.Columns.Count) qty = ParseDecimal(row[cQty]?.ToString());
                    if (qty == 0 && tbl.Columns.Count > 13) qty = ParseDecimal(row[13]?.ToString());
                    if (qty == 0 && tbl.Columns.Count > 12) qty = ParseDecimal(row[12]?.ToString());
                    
                    if (cPrice >= 0 && cPrice < tbl.Columns.Count) price = ParseDecimal(row[cPrice]?.ToString());
                    if (price == 0 && tbl.Columns.Count > 15) price = ParseDecimal(row[15]?.ToString());
                    if (price == 0 && tbl.Columns.Count > 14) price = ParseDecimal(row[14]?.ToString());

                    if (cAmount >= 0 && cAmount < tbl.Columns.Count) amt = ParseDecimal(row[cAmount]?.ToString());
                    if (amt == 0 && tbl.Columns.Count > 17) amt = ParseDecimal(row[17]?.ToString());
                    if (amt == 0 && tbl.Columns.Count > 16) amt = ParseDecimal(row[16]?.ToString());

                    var item = new SalesBillItem
                    {
                        ProductCode = parts.Length > 0 ? parts[0].Trim() : "",
                        ProductName = parts.Length > 1 ? parts[1].Trim() : prodName,
                        Qty = qty,
                        Unit = cUnit >= 0 && cUnit < tbl.Columns.Count ? row[cUnit]?.ToString()?.Trim() ?? "" : "",
                        Price = price,
                        Discount = ParseDecimal(cDiscount >= 0 && cDiscount < tbl.Columns.Count ? row[cDiscount]?.ToString() : ""),
                        Amount = amt
                    };
                    
                    if (item.Amount == 0 && item.Qty == 0) continue;

                    if (item.Amount == 0 && item.Qty > 0 && item.Price > 0) item.Amount = item.Qty * item.Price;
                    
                    if (item.ProductCode.Length > 30) item.ProductCode = item.ProductCode.Substring(0, 30);
                    if (item.ProductName.Length > 100) item.ProductName = item.ProductName.Substring(0, 100);
                    if (item.Unit.Length > 30) item.Unit = item.Unit.Substring(0, 30);
                    
                    currentItems.Add(item);
                    currentTotal += item.Amount;
                }
            }
            CollectCurrentBill();
            
            var billNos = parsedBills.Select(b => b.BillNo).Distinct().ToList();
            var existingBills = await _db.SalesBills.Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

            foreach (var b in parsedBills)
            {
                if (existingBills.TryGetValue(b.BillNo, out var ex))
                {
                    b.StatusType = "CHANGED";
                    b.ExistingAmount = ex.TotalAmount;
                }
                else { b.StatusType = "NEW"; }
            }
            result.Items = parsedBills;
            return result;
        }

                public async Task<(int matched, int notFound)> ImportReceiptMatchAsync(Stream stream, string fileName = "")
        {
            using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                ? ExcelReaderFactory.CreateCsvReader(stream) 
                : ExcelReaderFactory.CreateReader(stream);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null) return (0, 0);

            int matched = 0, notFound = 0;
            int headerRow = FindHeaderRow(tbl, out var map, "บิล", "ใบเสร็จ", "วันที่รับเงิน");
            if (headerRow < 0) headerRow = 3;

            int billColIndex = GetCol(map, "บิล", "เลขที่บิล", "เลขที่เอกสาร");
            int receiptColIndex = GetCol(map, "ใบเสร็จ", "เลขที่ใบเสร็จ");
            int dateColIndex = GetCol(map, "วันที่รับเงิน", "วันที่");
            int custColIndex = GetCol(map, "รหัส", "ลูกค้า", "รหัสลูกค้า");

            if (billColIndex < 0) billColIndex = 2;
            if (receiptColIndex < 0) receiptColIndex = 1;
            if (dateColIndex < 0) dateColIndex = 0;

            for (int r = headerRow + 1; r < tbl.Rows.Count; r++)
            {
                var row = tbl.Rows[r];
                var billNo = billColIndex >= 0 && billColIndex < tbl.Columns.Count ? row[billColIndex]?.ToString()?.Trim() : null;
                var receiptNo = receiptColIndex >= 0 && receiptColIndex < tbl.Columns.Count ? row[receiptColIndex]?.ToString()?.Trim() : null;
                var receiptDateStr = dateColIndex >= 0 && dateColIndex < tbl.Columns.Count ? row[dateColIndex]?.ToString()?.Trim() : null;
                var custCode = custColIndex >= 0 && custColIndex < tbl.Columns.Count ? row[custColIndex]?.ToString()?.Trim() : null;

                if (string.IsNullOrEmpty(billNo) || string.IsNullOrEmpty(receiptNo)) continue;
                
                bool isMatch = false;

                // 1. Update SalesBills
                var existingBills = await _db.SalesBills.Where(b => b.BillNo == billNo).ToListAsync();
                if (!string.IsNullOrEmpty(custCode)) {
                    existingBills = existingBills.Where(b => b.CustomerCode == custCode).ToList();
                }

                if (existingBills.Any())
                {
                    foreach (var b in existingBills)
                    {
                        b.ReceiptNo = receiptNo;
                        var rDate = ParseDate(receiptDateStr);
                        if (rDate != DateTime.MinValue) b.ReceiptDate = rDate;
                        b.IsFullyPaid = true;
                    }
                    isMatch = true;
                }

                // 2. Update OutstandingDebts
                var existingDebts = await _db.OutstandingDebts.Where(d => d.BillNo == billNo).ToListAsync();
                if (!string.IsNullOrEmpty(custCode)) {
                    existingDebts = existingDebts.Where(d => d.CustomerCode == custCode).ToList();
                }
                
                if (existingDebts.Any())
                {
                    foreach (var d in existingDebts)
                    {
                        d.ReceiptNo = receiptNo;
                        var rDate = ParseDate(receiptDateStr);
                        if (rDate != DateTime.MinValue) d.ReceiptDate = rDate;
                        d.RemainingAmount = 0;
                        d.Status = DebtStatus.PaidTransfer;
                        d.FullyPaidDate = d.ReceiptDate ?? DateTime.Now;
                        d.PaidDate = d.ReceiptDate ?? DateTime.Now;
                    }
                    isMatch = true;
                }
                
                if (isMatch) matched++; else notFound++;

                if ((matched + notFound) % 200 == 0) await _db.SaveChangesAsync();
            }
            await _db.SaveChangesAsync();
            return (matched, notFound);
        }

        private static DateTime ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return DateTime.Today;
            s = s.Trim();
            if (s.Contains("/"))
            {
                var parts = s.Split('/');
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[0], out int d) && int.TryParse(parts[1], out int m) && int.TryParse(parts[2].Split(' ')[0], out int y))
                    {
                        if (y > 2500) y -= 543;
                        else if (y < 100) y += 2000;
                        if (y > 2050) y -= 43;
                        try { return new DateTime(y, m, d); } catch { }
                    }
                }
            }
            if (DateTime.TryParse(s, out var dt)) return dt;
            return DateTime.Today;
        }

        private static decimal ParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Replace(",", "").Trim();
            return decimal.TryParse(s, out decimal result) ? result : 0;
        }

        private static int ParseInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Split(' ')[0].Trim();
            return int.TryParse(s, out int result) ? result : 0;
        }
    }
}




