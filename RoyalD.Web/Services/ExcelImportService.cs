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
            var existingBills = await _db.SalesBills.Include(b => b.Items).Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

            foreach (var b in p.Items.GroupBy(x => x.BillNo).Select(g => g.First()))
            {
                if (existingBills.TryGetValue(b.BillNo, out var ex))
                {
                    ex.CustomerName = b.CustomerName != null && b.CustomerName.Length > 100 ? b.CustomerName.Substring(0, 100) : (b.CustomerName ?? "");
                    ex.District = b.District != null && b.District.Length > 100 ? b.District.Substring(0, 100) : (b.District ?? "");
                    ex.Province = b.Province != null && b.Province.Length > 100 ? b.Province.Substring(0, 100) : (b.Province ?? "");
                    ex.SalesRep = b.SalesRep != null && b.SalesRep.Length > 100 ? b.SalesRep.Substring(0, 100) : (b.SalesRep ?? "");
                    ex.Phone = b.Phone != null && b.Phone.Length > 50 ? b.Phone.Substring(0, 50) : (b.Phone ?? "");
                    ex.Credit = b.Credit;
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
            var existingBills = await _db.SalesBills.Include(b => b.Items).Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

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
                        ex.Credit = b.Credit;
                        ex.TotalAmount = b.TotalAmount;
                        
                        ex.SourceMonth = p.FileType != null && p.FileType.Length > 10 ? p.FileType.Substring(0, 10) : (p.FileType ?? "");
                        
                        ex.PoNumber = b.PoNumber != null && b.PoNumber.Length > 100 ? b.PoNumber.Substring(0, 100) : (b.PoNumber ?? "");
                        
                        _db.SalesBillItems.RemoveRange(ex.Items);
                        ex.Items = b.Items ?? new List<SalesBillItem>();
                        
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
            var conf = new ExcelReaderConfiguration { FallbackEncoding = System.Text.Encoding.GetEncoding(874) };
              using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                  ? ExcelReaderFactory.CreateCsvReader(stream, conf) 
                  : ExcelReaderFactory.CreateReader(stream, conf);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null) return 0;

            try
            {
                _db.OutstandingDebts.RemoveRange(_db.OutstandingDebts.Where(d => d.Status == DebtStatus.Outstanding));
                await _db.SaveChangesAsync();

                int headerRow = FindHeaderRow(tbl, out var map, "เธฃเธซเธฑเธช", "เธเธทเนเธญ", "เธเธดเธฅ", "เธเธณเธเธงเธเน€เธเธดเธ");
                if (headerRow < 0) headerRow = 3;

                int cCustCode = GetCol(map, "เธฃเธซเธฑเธชเธฅเธนเธเธเนเธฒ", "เธฃเธซเธฑเธช");
                int cCustName = GetCol(map, "เธเธทเนเธญ");
                int cDistrict = GetCol(map, "เธญเธณเน€เธ เธญ");
                int cProvince = GetCol(map, "เธเธฑเธเธซเธงเธฑเธ”");
                int cBillNo = GetCol(map, "เธเธดเธฅ", "เน€เธฅเธเธ—เธตเน");
                int cBillDate = GetCol(map, "เธงเธฑเธเธ—เธตเนเธเธดเธฅ", "เธงเธฑเธเธ—เธตเน");
                int cDueDate = GetCol(map, "เธเธณเธซเธเธ”เธเธณเธฃเธฐ");
                int cAmount = GetCol(map, "เธเธณเธเธงเธเน€เธเธดเธ", "เธขเธญเธ”เธชเธธเธ—เธเธด", "เธขเธญเธ”เธซเธเธตเน");
                int cCredit = GetCol(map, "เน€เธเธฃเธ”เธดเธ•");
                int cSalesRep = GetCol(map, "เธเธนเนเนเธ—เธ");

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
                    if (colCust.Contains("เธฃเธงเธก") || colCust.Contains("เธ—เธฑเนเธเธซเธกเธ”")) continue;

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
                            if (strParts[0].Contains("เธ.") || strParts[0].Contains("เธเธฃเธธเธเน€เธ—เธ")) dynProv = strParts[0];
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

                    if (!string.IsNullOrEmpty(billNo) && !billNo.Contains("เธขเธญเธ”เธขเธเธกเธฒ") && !billNo.Contains("เธขเธญเธ”เธฃเธงเธก")) {
                        var billDate = ParseDate(cBillDate >= 0 && cBillDate < tbl.Columns.Count ? row[cBillDate] : null);
                        var dueDate = ParseDate(cDueDate >= 0 && cDueDate < tbl.Columns.Count ? row[cDueDate] : null);
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
            var conf = new ExcelReaderConfiguration { FallbackEncoding = System.Text.Encoding.GetEncoding(874) };
              using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                  ? ExcelReaderFactory.CreateCsvReader(stream, conf) 
                  : ExcelReaderFactory.CreateReader(stream, conf);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null || tbl.Rows.Count < 4) return result;

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

            for (int r = 0; r < tbl.Rows.Count; r++)
            {
                var row = tbl.Rows[r];
                var col0 = tbl.Columns.Count > 0 ? row[0]?.ToString()?.Trim() ?? "" : "";
                
                if (string.IsNullOrWhiteSpace(col0))
                {
                    // Check if it's a footer row to scrape total or other values
                    for (int c = 0; c < tbl.Columns.Count; c++)
                    {
                        var cellVal = row[c]?.ToString()?.Trim() ?? "";
                        if (cellVal == "เธฃเธงเธกเธ—เธฑเนเธเธชเธดเนเธ")
                        {
                            decimal amt = 0;
                            if (tbl.Columns.Count > 14) amt = ParseDecimal(row[14]?.ToString());
                            if (amt == 0 && tbl.Columns.Count > 15) amt = ParseDecimal(row[15]?.ToString());
                            if (amt > 0) currentTotal = amt;
                            break;
                        }
                    }
                    continue;
                }

                if (col0.StartsWith("S/N", StringComparison.OrdinalIgnoreCase)) continue;

                // Check if Column 1 (index 0) starts with digits and slash, or starts with 'R'
                bool startsWithR = col0.StartsWith("R", StringComparison.OrdinalIgnoreCase);
                bool isNoVatSlash = col0.Length > 0 && char.IsDigit(col0[0]) && col0.Contains("/");
                bool isBillHeader = startsWithR || isNoVatSlash;

                if (isBillHeader)
                {
                    CollectCurrentBill();
                    currentItems = new List<SalesBillItem>();

                    // Map Header Columns
                    // Column 1: Bill Number -> index 0
                    currentBillNo = col0;
                    
                    // Column 2: Date -> index 1
                    currentBillDate = ParseDate(tbl.Columns.Count > 1 ? row[1] : null);
                    
                    // Column 3: Customer ID -> index 2
                    currentCustCode = tbl.Columns.Count > 2 ? row[2]?.ToString()?.Trim() ?? "" : "";
                    
                    // Column 4: PO Number -> index 3 (If empty, display blank)
                    currentPoNumber = tbl.Columns.Count > 3 ? row[3]?.ToString()?.Trim() ?? "" : "";
                    
                    // Column 5: Customer Name -> index 4
                    currentCustName = tbl.Columns.Count > 4 ? row[4]?.ToString()?.Trim() ?? "" : "";
                    
                    // Column 7: District/Area -> index 6
                    currentDistrict = tbl.Columns.Count > 6 ? row[6]?.ToString()?.Trim() ?? "" : "";
                    
                    // Column 9: Province -> index 8
                    currentProvince = tbl.Columns.Count > 8 ? row[8]?.ToString()?.Trim() ?? "" : "";
                    
                    // Column 11: Credit Terms -> could be shifted to index 9, 10 or 11
                    int c10 = tbl.Columns.Count > 10 ? ParseInt(row[10]?.ToString()) : 0;
                    int c9 = tbl.Columns.Count > 9 ? ParseInt(row[9]?.ToString()) : 0;
                    int c11 = tbl.Columns.Count > 11 ? ParseInt(row[11]?.ToString()) : 0;
                    currentCredit = c10 > 0 ? c10 : (c9 > 0 ? c9 : c11);
                    
                    // Column 16: Sales Representative -> index 15
                    currentSalesRep = tbl.Columns.Count > 15 ? row[15]?.ToString()?.Trim() ?? "" : "";

                    // Phone Number: row 2 containing "เนเธ—เธฃ." (row immediately following header row)
                    currentPhone = "";
                    if (r + 1 < tbl.Rows.Count)
                    {
                        var nextRow = tbl.Rows[r + 1];
                        for (int c = 0; c < tbl.Columns.Count; c++)
                        {
                            var val = nextRow[c]?.ToString()?.Trim() ?? "";
                            if (val.Contains("เนเธ—เธฃ") || val.Contains("เน."))
                            {
                                currentPhone = val.Replace("เนเธ—เธฃ.", "").Replace("เนเธ—เธฃ", "").Replace("เน.", "").Trim();
                                break;
                            }
                        }
                    }

                    currentTotal = 0;
                }
                else
                {
                    // Product detail row: Column 1 (index 0) has the product code + name
                    string rawProd = col0.Replace((char)160, ' ').Trim();
                    string prodCode = "";
                    string prodName = rawProd;
                    var parts = rawProd.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        prodCode = parts[0].Trim();
                        prodName = parts[1].Trim();
                    }

                    decimal qty = 0, price = 0, discount = 0, amt = 0;
                    string unit = "";

                    int lastIdx = -1;
                    for (int i = tbl.Columns.Count - 1; i >= 1; i--)
                    {
                        if (!string.IsNullOrWhiteSpace(row[i]?.ToString()))
                        {
                            lastIdx = i;
                            break;
                        }
                    }

                    if (lastIdx >= 4)
                    {
                        amt = ParseDecimal(row[lastIdx]?.ToString());
                        string rawDiscount = row[lastIdx - 1]?.ToString()?.Trim() ?? "";
                        if (rawDiscount.EndsWith("%"))
                        {
                            discount = ParseDecimal(rawDiscount.Replace("%", ""));
                        }
                        else
                        {
                            discount = ParseDecimal(rawDiscount);
                        }
                        price = ParseDecimal(row[lastIdx - 2]?.ToString());
                        unit = row[lastIdx - 3]?.ToString()?.Trim() ?? "";
                        qty = ParseDecimal(row[lastIdx - 4]?.ToString());
                    }

                    if (qty > 0 && price == 0 && amt > 0)
                    {
                        price = Math.Round(amt / qty, 2);
                    }

                    if (qty == 0 && amt == 0) continue; // skip non-product lines
                    if (amt == 0 && qty > 0 && price > 0) amt = qty * price;

                    var item = new SalesBillItem
                    {
                        ProductCode = prodCode.Length > 30 ? prodCode.Substring(0, 30) : prodCode,
                        ProductName = prodName.Length > 100 ? prodName.Substring(0, 100) : prodName,
                        Qty = qty,
                        Unit = unit.Length > 30 ? unit.Substring(0, 30) : unit,
                        Price = price,
                        Discount = discount,
                        Amount = amt
                    };
                    currentItems.Add(item);
                    if (currentTotal == 0)
                    {
                        currentTotal += amt; // fallback if no "เธฃเธงเธกเธ—เธฑเนเธเธชเธดเนเธ" row has set it yet
                    }
                }
            }
            CollectCurrentBill();
            
            var billNos = parsedBills.Select(b => b.BillNo).Distinct().ToList();
            var existingBills = await _db.SalesBills.Include(b => b.Items).Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

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
            var conf = new ExcelReaderConfiguration { FallbackEncoding = System.Text.Encoding.GetEncoding(874) };
              using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                  ? ExcelReaderFactory.CreateCsvReader(stream, conf) 
                  : ExcelReaderFactory.CreateReader(stream, conf);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null) return (0, 0);

            int matched = 0, notFound = 0;
            int headerRow = FindHeaderRow(tbl, out var map, "เธเธดเธฅ", "เนเธเน€เธชเธฃเนเธ", "เธงเธฑเธเธ—เธตเนเธฃเธฑเธเน€เธเธดเธ");
            if (headerRow < 0) headerRow = 3;

            int billColIndex = GetCol(map, "เธเธดเธฅ", "เน€เธฅเธเธ—เธตเนเธเธดเธฅ", "เน€เธฅเธเธ—เธตเนเน€เธญเธเธชเธฒเธฃ");
            int receiptColIndex = GetCol(map, "เนเธเน€เธชเธฃเนเธ", "เน€เธฅเธเธ—เธตเนเนเธเน€เธชเธฃเนเธ");
            int dateColIndex = GetCol(map, "เธงเธฑเธเธ—เธตเนเธฃเธฑเธเน€เธเธดเธ", "เธงเธฑเธเธ—เธตเน");
            int custColIndex = GetCol(map, "เธฃเธซเธฑเธช", "เธฅเธนเธเธเนเธฒ", "เธฃเธซเธฑเธชเธฅเธนเธเธเนเธฒ");

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
                        var rDate = ParseDate((object)receiptDateStr);
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
                        var rDate = ParseDate((object)receiptDateStr);
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

        private static DateTime ParseDate(object? obj)
        {
            string s = obj?.ToString() ?? "";
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
            var match = System.Text.RegularExpressions.Regex.Match(s, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }
    }
}





