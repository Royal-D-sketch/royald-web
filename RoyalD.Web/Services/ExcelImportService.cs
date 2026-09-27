using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // ==========================================
        // 1. DATA CLEANSING & PARSING FOR SALES BILLS
        // ==========================================
        public static List<BillPreviewItem> CleanAndParseSalesBills(DataTable tbl, out DateTime maxDate)
        {
            maxDate = DateTime.MinValue;
            var list = new List<BillPreviewItem>();
            if (tbl == null || tbl.Rows.Count < 2) return list;

            BillPreviewItem? currentBill = null;
            var subtotalKeywords = new[] { "รวมทั้งสิ้น", "ยอดคงเหลือ", "VAT.=", "รวม....", "ส่วนลด", "ยอดรวม", "รวมทั้งหมด", "ยอดรวมทั้งสิ้น", "ยอดสุทธิ" };

            for (int r = 0; r < tbl.Rows.Count; r++)
            {
                var row = tbl.Rows[r];
                string c0 = tbl.Columns.Count > 0 ? row[0]?.ToString()?.Trim() ?? "" : "";
                string fullRow = string.Join(" ", row.ItemArray.Select(x => x?.ToString()?.Trim() ?? "")).Trim();

                if (string.IsNullOrWhiteSpace(fullRow)) continue;

                // 1. Skip CD Page Header Rows
                if (fullRow.Contains("รายละเอียด บิลขาย") || fullRow.Contains("Royal-D (Thailand)") ||
                    fullRow.Contains("รหัส-ชื่อสินค้า") || (fullRow.Contains("บิล") && fullRow.Contains("รหัสลูกค้า")))
                {
                    continue;
                }

                // 2. Detect & Discard Summary Rows (extract total if available)
                bool isSummary = false;
                foreach (var kw in subtotalKeywords)
                {
                    if (fullRow.Contains(kw))
                    {
                        isSummary = true;
                        if (kw.Contains("รวมทั้งสิ้น") && currentBill != null)
                        {
                            for (int c = tbl.Columns.Count - 1; c >= 0; c--)
                            {
                                decimal amt = ParseDecimal(row[c]?.ToString());
                                if (amt > 0)
                                {
                                    currentBill.TotalAmount = amt;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                if (isSummary) continue;

                // 3. Detect Bill Header Row (e.g. R153342, 630/31465, SO..., IV...)
                bool startsWithR = c0.StartsWith("R", StringComparison.OrdinalIgnoreCase);
                bool startsWithSO = c0.StartsWith("SO", StringComparison.OrdinalIgnoreCase);
                bool startsWithIV = c0.StartsWith("IV", StringComparison.OrdinalIgnoreCase);
                bool isSlashBill = c0.Length > 0 && char.IsDigit(c0[0]) && c0.Contains("/") && c0.Split('/').Length == 2 && c0.Length < 25;
                bool isHeader = (startsWithR || startsWithSO || startsWithIV || isSlashBill) &&
                                tbl.Columns.Count > 1 && !string.IsNullOrWhiteSpace(row[1]?.ToString());

                if (isHeader)
                {
                    if (currentBill != null && !string.IsNullOrEmpty(currentBill.BillNo))
                    {
                        if (currentBill.TotalAmount == 0 && currentBill.Items.Count > 0)
                            currentBill.TotalAmount = currentBill.Items.Sum(i => i.Amount);
                        currentBill.ItemCount = currentBill.Items.Count;
                        list.Add(currentBill);
                    }

                    var billDate = ParseThaiDate(tbl.Columns.Count > 1 ? row[1] : null);
                    if (billDate != DateTime.MinValue && billDate.Year >= 2000 && billDate.Year <= 2100)
                    {
                        if (billDate > maxDate) maxDate = billDate;
                    }

                    // Extract Credit Terms (shifted across col 9, 10, 11)
                    int cr10 = tbl.Columns.Count > 10 ? ParseInt(row[10]?.ToString()) : 0;
                    int cr9 = tbl.Columns.Count > 9 ? ParseInt(row[9]?.ToString()) : 0;
                    int cr11 = tbl.Columns.Count > 11 ? ParseInt(row[11]?.ToString()) : 0;
                    int credit = cr10 > 0 ? cr10 : (cr9 > 0 ? cr9 : cr11);

                    string rep = "";
                    if (tbl.Columns.Count > 16 && !string.IsNullOrWhiteSpace(row[16]?.ToString()))
                        rep = row[16].ToString()!.Trim();
                    else if (tbl.Columns.Count > 15 && !string.IsNullOrWhiteSpace(row[15]?.ToString()))
                        rep = row[15].ToString()!.Trim();

                    var rawCustName = tbl.Columns.Count > 4 ? row[4]?.ToString()?.Trim() ?? "" : "";
                    currentBill = new BillPreviewItem
                    {
                        BillNo = c0,
                        BillDate = billDate == DateTime.MinValue ? DateTime.Today : billDate,
                        CustomerCode = tbl.Columns.Count > 2 ? row[2]?.ToString()?.Trim() ?? "" : "",
                        PoNumber = tbl.Columns.Count > 3 ? row[3]?.ToString()?.Trim() ?? "" : "",
                        CustomerName = rawCustName.Length > 200 ? rawCustName.Substring(0, 200) : rawCustName,
                        District = tbl.Columns.Count > 6 ? row[6]?.ToString()?.Trim() ?? "" : "",
                        Province = tbl.Columns.Count > 8 ? row[8]?.ToString()?.Trim() ?? "" : "",
                        Credit = credit,
                        SalesRep = rep,
                        Items = new List<SalesBillItem>()
                    };

                    // Check next row for Phone Number
                    if (r + 1 < tbl.Rows.Count)
                    {
                        var nextRow = tbl.Rows[r + 1];
                        for (int c = 0; c < tbl.Columns.Count; c++)
                        {
                            var v = nextRow[c]?.ToString()?.Trim() ?? "";
                            if (v.Contains("โทร") || v.Contains("โ.") || v.StartsWith("Tel", StringComparison.OrdinalIgnoreCase))
                            {
                                currentBill.Phone = v.Replace("โทร.", "").Replace("โทร", "").Replace("โ.", "").Replace("Tel.", "").Replace("Tel", "").Trim();
                                break;
                            }
                        }
                    }
                    continue;
                }

                // 4. Product Item Row
                if (currentBill != null && !string.IsNullOrWhiteSpace(c0) &&
                    !c0.StartsWith("S/N", StringComparison.OrdinalIgnoreCase) &&
                    !c0.StartsWith("โทร", StringComparison.OrdinalIgnoreCase) &&
                    !c0.StartsWith("โ.", StringComparison.OrdinalIgnoreCase) &&
                    !c0.StartsWith("Tel", StringComparison.OrdinalIgnoreCase))
                {
                    string rawProd = c0.Replace((char)160, ' ').Trim();
                    string prodCode = "";
                    string prodName = rawProd;
                    var parts = rawProd.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && parts[0].Any(char.IsDigit))
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
                        string rawDisc = row[lastIdx - 1]?.ToString()?.Trim() ?? "";
                        discount = rawDisc.EndsWith("%") ? ParseDecimal(rawDisc.Replace("%", "")) : ParseDecimal(rawDisc);
                        price = ParseDecimal(row[lastIdx - 2]?.ToString());
                        unit = row[lastIdx - 3]?.ToString()?.Trim() ?? "";
                        qty = ParseDecimal(row[lastIdx - 4]?.ToString());
                    }
                    else if (lastIdx >= 1)
                    {
                        for (int i = 1; i <= lastIdx; i++)
                        {
                            var val = row[i]?.ToString()?.Trim() ?? "";
                            if (string.IsNullOrEmpty(val)) continue;
                            if (qty == 0 && decimal.TryParse(val.Replace(",", ""), out decimal q) && q > 0)
                                qty = q;
                            else if (string.IsNullOrEmpty(unit) && !decimal.TryParse(val.Replace(",", ""), out _))
                                unit = val;
                            else if (price == 0 && decimal.TryParse(val.Replace(",", ""), out decimal p))
                                price = p;
                            else if (amt == 0 && decimal.TryParse(val.Replace(",", ""), out decimal a))
                                amt = a;
                        }
                    }

                    if (qty > 0 && price == 0 && amt > 0) price = Math.Round(amt / qty, 2);
                    if (qty == 0 && price > 0 && amt > 0) qty = Math.Round(amt / price, 2);
                    if (amt == 0 && qty > 0 && price > 0) amt = Math.Round(qty * price, 2);

                    if (qty > 0 || amt > 0)
                    {
                        currentBill.Items.Add(new SalesBillItem
                        {
                            BillNo = currentBill.BillNo,
                            ProductCode = prodCode.Length > 50 ? prodCode.Substring(0, 50) : prodCode,
                            ProductName = prodName.Length > 200 ? prodName.Substring(0, 200) : prodName,
                            Qty = qty,
                            Unit = unit.Length > 30 ? unit.Substring(0, 30) : unit,
                            Price = price,
                            Discount = discount,
                            Amount = amt
                        });
                    }
                }
            }

            if (currentBill != null && !string.IsNullOrEmpty(currentBill.BillNo))
            {
                if (currentBill.TotalAmount == 0 && currentBill.Items.Count > 0)
                    currentBill.TotalAmount = currentBill.Items.Sum(i => i.Amount);
                currentBill.ItemCount = currentBill.Items.Count;
                list.Add(currentBill);
            }

            return list;
        }

        // ==========================================
        // 2. DATA CLEANSING & PARSING FOR DEBTORS
        // ==========================================
        public static List<OutstandingDebt> CleanAndParseDebtors(DataTable tbl, out DateTime maxDate)
        {
            maxDate = DateTime.MinValue;
            var list = new List<OutstandingDebt>();
            if (tbl == null || tbl.Rows.Count < 2) return list;

            int headerRow = -1;
            for (int r = 0; r < Math.Min(15, tbl.Rows.Count); r++)
            {
                string full = string.Join(" ", tbl.Rows[r].ItemArray.Select(x => x?.ToString()?.Trim() ?? "")).Trim();
                if (full.Contains("รหัสลูกค้า") || full.Contains("บิล") || (full.Contains("ชื่อ") && full.Contains("จำนวนเงิน")))
                {
                    headerRow = r;
                    break;
                }
            }
            if (headerRow < 0) headerRow = 3;

            string currentCustCode = "", currentCustName = "", currentDistrict = "", currentProvince = "", currentSalesRep = "";

            for (int r = headerRow + 1; r < tbl.Rows.Count; r++)
            {
                var row = tbl.Rows[r];
                string fullRow = string.Join(" ", row.ItemArray.Select(x => x?.ToString()?.Trim() ?? "")).Trim();
                if (string.IsNullOrWhiteSpace(fullRow)) continue;

                // Discard page headers & subtotal summary lines
                if (fullRow.Contains("รายงานลูกหนี้") || fullRow.Contains("Royal-D") || fullRow.Contains("รหัสลูกค้า") ||
                    fullRow.Contains("ยอดรวม") || fullRow.Contains("รวมทั้งหมด") || fullRow.Contains("รวมทั้งสิ้น") || fullRow.Contains("ยอดรวมตาม"))
                {
                    continue;
                }

                string col0 = tbl.Columns.Count > 0 ? row[0]?.ToString()?.Trim() ?? "" : "";
                string col1 = tbl.Columns.Count > 1 ? row[1]?.ToString()?.Trim() ?? "" : "";

                // Propagate customer info across merged/blank cells (when col0 is customer code, not a bill)
                if (!string.IsNullOrEmpty(col0) && !col0.Contains("/") && !col0.StartsWith("R", StringComparison.OrdinalIgnoreCase) && !col0.StartsWith("CN", StringComparison.OrdinalIgnoreCase))
                {
                    currentCustCode = col0;
                    currentCustName = col1;

                    string dynDist = "", dynProv = "", dynRep = "";
                    var strParts = new List<string>();
                    for (int i = 2; i < tbl.Columns.Count; i++)
                    {
                        var v = row[i]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(v) && !decimal.TryParse(v.Replace(",", ""), out _) && !DateTime.TryParse(v, out _) && !v.Contains("/"))
                        {
                            strParts.Add(v);
                        }
                    }
                    if (strParts.Count > 0) dynRep = strParts.Last();
                    if (strParts.Count >= 3) { dynDist = strParts[0]; dynProv = strParts[1]; }
                    else if (strParts.Count == 2)
                    {
                        if (strParts[0].Contains("จ.") || strParts[0].Contains("กรุงเทพ")) dynProv = strParts[0];
                        else dynDist = strParts[0];
                    }

                    if (tbl.Columns.Count > 5 && !string.IsNullOrWhiteSpace(row[5]?.ToString())) currentDistrict = row[5].ToString()!.Trim();
                    else if (!string.IsNullOrEmpty(dynDist)) currentDistrict = dynDist;

                    if (tbl.Columns.Count > 6 && !string.IsNullOrWhiteSpace(row[6]?.ToString())) currentProvince = row[6].ToString()!.Trim();
                    else if (!string.IsNullOrEmpty(dynProv)) currentProvince = dynProv;

                    if (tbl.Columns.Count > 12 && !string.IsNullOrWhiteSpace(row[12]?.ToString())) currentSalesRep = row[12].ToString()!.Trim();
                    else if (!string.IsNullOrEmpty(dynRep)) currentSalesRep = dynRep;
                }

                // Locate bill number and its column index
                string billNo = "";
                int billColIdx = -1;

                if (tbl.Columns.Count > 7 && !string.IsNullOrWhiteSpace(row[7]?.ToString()))
                {
                    string v7 = row[7].ToString()!.Trim();
                    if (v7.Contains("/") || v7.StartsWith("R", StringComparison.OrdinalIgnoreCase) || v7.StartsWith("CN", StringComparison.OrdinalIgnoreCase) || v7.StartsWith("SO", StringComparison.OrdinalIgnoreCase) || v7.StartsWith("IV", StringComparison.OrdinalIgnoreCase) || v7.StartsWith("DN", StringComparison.OrdinalIgnoreCase))
                    {
                        billNo = v7;
                        billColIdx = 7;
                    }
                }

                if (string.IsNullOrEmpty(billNo))
                {
                    for (int c = 0; c < tbl.Columns.Count; c++)
                    {
                        var v = row[c]?.ToString()?.Trim() ?? "";
                        if (string.IsNullOrEmpty(v)) continue;
                        if (v.StartsWith("R", StringComparison.OrdinalIgnoreCase) || v.StartsWith("CN", StringComparison.OrdinalIgnoreCase) || v.StartsWith("SO", StringComparison.OrdinalIgnoreCase) || v.StartsWith("IV", StringComparison.OrdinalIgnoreCase) || v.StartsWith("DN", StringComparison.OrdinalIgnoreCase) || (v.Contains("/") && v.Split('/').Length == 2 && v.Length < 25))
                        {
                            billNo = v;
                            billColIdx = c;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(billNo) || billColIdx < 0) continue;

                // Parse bill details strictly AFTER billColIdx to never mistake customer code for amount
                DateTime billDate = DateTime.MinValue;
                DateTime dueDate = DateTime.MinValue;
                decimal amount = 0;
                int credit = 0;
                string rep = currentSalesRep;

                if (billColIdx == 7 && tbl.Columns.Count >= 12)
                {
                    billDate = ParseThaiDate(row[8]);
                    dueDate = ParseThaiDate(row[9]);
                    amount = ParseDecimal(row[10]?.ToString());
                    credit = ParseInt(row[11]?.ToString());
                    if (tbl.Columns.Count > 12 && !string.IsNullOrWhiteSpace(row[12]?.ToString()))
                        rep = row[12].ToString()!.Trim();
                }
                else
                {
                    for (int c = billColIdx + 1; c < tbl.Columns.Count; c++)
                    {
                        var v = row[c]?.ToString()?.Trim() ?? "";
                        if (string.IsNullOrEmpty(v)) continue;

                        var dt = ParseThaiDate(v);
                        if (dt != DateTime.MinValue && dt.Year >= 2000 && dt.Year <= 2100)
                        {
                            if (billDate == DateTime.MinValue)
                                billDate = dt;
                            else if (dueDate == DateTime.MinValue)
                                dueDate = dt;
                            continue;
                        }

                        if (decimal.TryParse(v.Replace(",", ""), out decimal num))
                        {
                            if (amount == 0 && (v.Contains(".") || num > 365 || num < 0))
                                amount = num;
                            else if (credit == 0 && num > 0 && num <= 365 && !v.Contains("."))
                                credit = (int)num;
                            else if (amount == 0)
                                amount = num;
                        }
                        else if (!string.IsNullOrEmpty(v) && v.Length > 1 && !v.Contains("/"))
                        {
                            rep = v;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(billNo) && amount != 0)
                {
                    if (billDate != DateTime.MinValue && billDate.Year >= 2000 && billDate.Year <= 2100)
                    {
                        if (billDate > maxDate) maxDate = billDate;
                    }

                    list.Add(new OutstandingDebt
                    {
                        CustomerCode = currentCustCode.Length > 20 ? currentCustCode.Substring(0, 20) : currentCustCode,
                        CustomerName = currentCustName.Length > 200 ? currentCustName.Substring(0, 200) : currentCustName,
                        District = currentDistrict.Length > 200 ? currentDistrict.Substring(0, 200) : currentDistrict,
                        Province = currentProvince.Length > 200 ? currentProvince.Substring(0, 200) : currentProvince,
                        BillNo = billNo.Length > 200 ? billNo.Substring(0, 200) : billNo,
                        BillDate = billDate == DateTime.MinValue ? DateTime.Today : billDate,
                        DueDate = dueDate == DateTime.MinValue ? (credit > 0 ? (billDate == DateTime.MinValue ? DateTime.Today : billDate).AddDays(credit) : DateTime.Today) : dueDate,
                        OriginalAmount = amount,
                        RemainingAmount = amount,
                        Credit = credit,
                        SalesRep = string.IsNullOrEmpty(rep) ? currentSalesRep : rep,
                        Status = DebtStatus.Outstanding
                    });
                }
            }

            return list;
        }

        // ==========================================
        // 3. DATA CLEANSING & PARSING FOR RECEIPTS
        // ==========================================
        public static List<ReceiptPreviewRow> CleanAndParseReceipts(DataTable tbl, out DateTime maxDate)
        {
            maxDate = DateTime.MinValue;
            var list = new List<ReceiptPreviewRow>();
            if (tbl == null || tbl.Rows.Count < 2) return list;

            int headerRow = -1;
            for (int r = 0; r < Math.Min(15, tbl.Rows.Count); r++)
            {
                string full = string.Join(" ", tbl.Rows[r].ItemArray.Select(x => x?.ToString()?.Trim() ?? "")).Trim();
                if (full.Contains("ใบเสร็จ") || full.Contains("วันที่รับเงิน") || (full.Contains("บิล") && full.Contains("จำนวนเงิน")))
                {
                    headerRow = r;
                    break;
                }
            }
            if (headerRow < 0) headerRow = 3;

            string currentReceiptNo = "";
            DateTime currentReceiptDate = DateTime.MinValue;
            string currentReceiptDateStr = "";
            string currentCustCode = "";

            for (int r = headerRow + 1; r < tbl.Rows.Count; r++)
            {
                var row = tbl.Rows[r];
                string fullRow = string.Join(" ", row.ItemArray.Select(x => x?.ToString()?.Trim() ?? "")).Trim();
                if (string.IsNullOrWhiteSpace(fullRow)) continue;

                // Discard page headers & subtotal summary lines
                if (fullRow.Contains("สรุปการชำระเงิน") || fullRow.Contains("Royal-D") || fullRow.Contains("วันที่รับเงิน") ||
                    fullRow.Contains("ยอดรวม") || fullRow.Contains("รวมทั้งหมด") || fullRow.Contains("รวมทั้งสิ้น"))
                {
                    continue;
                }

                string colDate = tbl.Columns.Count > 0 ? row[0]?.ToString()?.Trim() ?? "" : "";
                string colReceipt = tbl.Columns.Count > 1 ? row[1]?.ToString()?.Trim() ?? "" : "";
                string colBill = tbl.Columns.Count > 2 ? row[2]?.ToString()?.Trim() ?? "" : "";
                string colCust = tbl.Columns.Count > 3 ? row[3]?.ToString()?.Trim() ?? "" : "";
                string colAmt = tbl.Columns.Count > 7 ? row[7]?.ToString()?.Trim() ?? "" : "";

                var dt = ParseThaiDate(colDate);
                if (dt != DateTime.MinValue && dt.Year >= 2000 && dt.Year <= 2100)
                {
                    currentReceiptDate = dt;
                    currentReceiptDateStr = colDate;
                    if (currentReceiptDate > maxDate) maxDate = currentReceiptDate;
                }

                if (!string.IsNullOrEmpty(colReceipt)) currentReceiptNo = colReceipt;
                if (!string.IsNullOrEmpty(colCust)) currentCustCode = colCust;

                // Extract clean Bill No (strip `#` characters)
                string billNo = colBill.Replace("#", "").Trim();
                decimal amt = ParseDecimal(colAmt);

                if (!string.IsNullOrEmpty(billNo) && !string.IsNullOrEmpty(currentReceiptNo))
                {
                    list.Add(new ReceiptPreviewRow
                    {
                        ReceiptNo = currentReceiptNo,
                        ReceiptDate = currentReceiptDate == DateTime.MinValue ? DateTime.Today : currentReceiptDate,
                        ReceiptDateStr = currentReceiptDateStr,
                        BillNo = billNo,
                        CustomerCode = currentCustCode,
                        Amount = amt
                    });
                }
            }

            return list;
        }

        // ==========================================
        // PREVIEW / CONFIRM SALES BILL FLOW
        // ==========================================
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ImportPreviewResult> _previewCache = new();
        public static void SetPreview(string id, ImportPreviewResult preview) => _previewCache[id] = preview;
        public static ImportPreviewResult? GetPreview(string id) => _previewCache.TryGetValue(id, out var p) ? p : null;
        public static void RemovePreview(string id) => _previewCache.TryRemove(id, out _);

        public async Task<ImportPreviewResult> PreviewSalesBillAsync(Stream stream, string sourceMonth, bool isCurrentMonth, string fileName)
        {
            var result = new ImportPreviewResult { FileType = sourceMonth, FileName = fileName, IsCurrentMonth = isCurrentMonth };
            var conf = new ExcelReaderConfiguration { FallbackEncoding = Encoding.GetEncoding(874) };
            using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? ExcelReaderFactory.CreateCsvReader(stream, conf)
                : ExcelReaderFactory.CreateReader(stream, conf);

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null || tbl.Rows.Count < 2) return result;

            var parsedBills = CleanAndParseSalesBills(tbl, out DateTime maxDate);
            result.LatestDate = maxDate;
            result.TotalRows = parsedBills.Count;
            result.TotalAmount = parsedBills.Sum(b => b.TotalAmount);

            var billNos = parsedBills.Select(b => b.BillNo).Distinct().ToList();
            var existingBills = await _db.SalesBills.Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

            foreach (var b in parsedBills)
            {
                if (existingBills.TryGetValue(b.BillNo, out var ex))
                {
                    b.StatusType = "CHANGED";
                    b.ExistingAmount = ex.TotalAmount;
                    result.DuplicateChangedCount++;
                    result.DuplicateAmount += b.TotalAmount;
                }
                else
                {
                    b.StatusType = "NEW";
                    result.NewCount++;
                    result.NewAmount += b.TotalAmount;
                }
            }

            result.Items = parsedBills;
            SetPreview(result.PreviewId, result);
            return result;
        }

        public async Task<(int inserted, int updated, int skipped, DateTime latestDate)> ConfirmImportSalesBillAsync(string previewId, bool updateDuplicates = true, bool skipDuplicates = false)
        {
            var p = GetPreview(previewId);
            if (p == null) return (0, 0, 0, DateTime.MinValue);

            int inserted = 0, updated = 0, skipped = 0;

            // Ensure Customers exist to prevent foreign key errors
            var custCodes = p.Items.Where(b => !string.IsNullOrEmpty(b.CustomerCode)).Select(b => b.CustomerCode).Distinct().ToList();
            var existingCustEntities = await _db.Customers.Where(c => custCodes.Contains(c.CustomerCode)).ToListAsync();
            var existingCustsSet = existingCustEntities.Select(c => c.CustomerCode).ToHashSet();
            // Build best name map (longest name per customer code from current import)
            var bestNameMap = p.Items
                .Where(b => !string.IsNullOrEmpty(b.CustomerCode) && !string.IsNullOrEmpty(b.CustomerName))
                .GroupBy(b => b.CustomerCode)
                .ToDictionary(g => g.Key!, g => g.OrderByDescending(b => b.CustomerName!.Length).First().CustomerName!);
            foreach (var b in p.Items)
            {
                if (!string.IsNullOrEmpty(b.CustomerCode) && !existingCustsSet.Contains(b.CustomerCode))
                {
                    _db.Customers.Add(new Customer
                    {
                        CustomerCode = b.CustomerCode,
                        Name = b.CustomerName ?? "",
                        District = b.District ?? "",
                        Province = b.Province ?? "",
                        Phone = b.Phone ?? ""
                    });
                    existingCustsSet.Add(b.CustomerCode);
                }
            }
            // Update existing customers if import has a longer name
            foreach (var cust in existingCustEntities)
            {
                if (bestNameMap.TryGetValue(cust.CustomerCode, out var newName) && newName.Length > (cust.Name?.Length ?? 0))
                    cust.Name = newName;
            }
            await _db.SaveChangesAsync();

            var billNos = p.Items.Select(b => b.BillNo).Distinct().ToList();
            var existingBills = await _db.SalesBills.Include(b => b.Items).Where(b => billNos.Contains(b.BillNo)).ToDictionaryAsync(b => b.BillNo);

            foreach (var b in p.Items.GroupBy(x => x.BillNo).Select(g => g.First()))
            {
                if (existingBills.TryGetValue(b.BillNo, out var ex))
                {
                    if (skipDuplicates) { skipped++; continue; }
                    if (updateDuplicates)
                    {
                        ex.BillDate = b.BillDate;
                        ex.CustomerCode = b.CustomerCode;
                        ex.CustomerName = b.CustomerName;
                        ex.District = b.District;
                        ex.Province = b.Province;
                        ex.SalesRep = b.SalesRep;
                        ex.Phone = b.Phone;
                        ex.Credit = b.Credit;
                        ex.TotalAmount = b.TotalAmount;
                        ex.SourceMonth = p.FileType ?? "";
                        ex.PoNumber = b.PoNumber;

                        _db.SalesBillItems.RemoveRange(ex.Items);
                        if (b.Items != null) { foreach (var i in b.Items) i.BillNo = b.BillNo; }
                        ex.Items = b.Items ?? new List<SalesBillItem>();

                        updated++;
                    }
                }
                else
                {
                    if (b.Items != null) { foreach (var i in b.Items) i.BillNo = b.BillNo; }
                    _db.SalesBills.Add(new SalesBill
                    {
                        BillNo = b.BillNo,
                        BillDate = b.BillDate,
                        CustomerCode = b.CustomerCode,
                        CustomerName = b.CustomerName,
                        District = b.District,
                        Province = b.Province,
                        Phone = b.Phone,
                        Credit = b.Credit,
                        SalesRep = b.SalesRep,
                        TotalAmount = b.TotalAmount,
                        SourceMonth = p.FileType ?? "",
                        PoNumber = b.PoNumber,
                        Items = b.Items ?? new List<SalesBillItem>()
                    });
                    inserted++;
                }

                if ((inserted + updated) % 200 == 0) await _db.SaveChangesAsync();
            }

            await _db.SaveChangesAsync();
            RemovePreview(previewId);
            return (inserted, updated, skipped, p.LatestDate);
        }

        public async Task<(int inserted, int updated, DateTime latestDate, List<BillPreviewItem> previewItems)> ImportSalesBillAsync(Stream stream, string sourceMonth, bool isCurrentMonth = false, string fileName = "DirectUpload")
        {
            var p = await PreviewSalesBillAsync(stream, sourceMonth, isCurrentMonth, fileName);
            var (ins, upd, _, maxDate) = await ConfirmImportSalesBillAsync(p.PreviewId, updateDuplicates: true, skipDuplicates: false);
            return (ins, upd, maxDate, p.Items.Take(10).ToList());
        }

        // ==========================================
        // IMPORT OUTSTANDING DEBTS FLOW (OVERWRITE 100%)
        // ==========================================
        public async Task<(int count, DateTime latestDate, List<OutstandingDebt> previewRows)> ImportOutstandingDebtsAsync(Stream stream, string fileName = "")
        {
            var conf = new ExcelReaderConfiguration { FallbackEncoding = Encoding.GetEncoding(874) };
            using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? ExcelReaderFactory.CreateCsvReader(stream, conf)
                : ExcelReaderFactory.CreateReader(stream, conf);

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            if (tbl == null || tbl.Rows.Count < 2) return (0, DateTime.MinValue, new List<OutstandingDebt>());

            var cleanedDebts = CleanAndParseDebtors(tbl, out DateTime maxDate);

            // Ensure Customers exist to prevent foreign key errors
            var custCodes = cleanedDebts.Where(d => !string.IsNullOrEmpty(d.CustomerCode)).Select(d => d.CustomerCode).Distinct().ToList();
            var existingCustEntitiesOD = await _db.Customers.Where(c => custCodes.Contains(c.CustomerCode)).ToListAsync();
            var existingCustsOD = existingCustEntitiesOD.Select(c => c.CustomerCode).ToHashSet();
            // Build best name map (longest name per customer code from current import)
            var bestNameMapOD = cleanedDebts
                .Where(d => !string.IsNullOrEmpty(d.CustomerCode) && !string.IsNullOrEmpty(d.CustomerName))
                .GroupBy(d => d.CustomerCode)
                .ToDictionary(g => g.Key!, g => g.OrderByDescending(d => d.CustomerName!.Length).First().CustomerName!);
            foreach (var d in cleanedDebts)
            {
                if (!string.IsNullOrEmpty(d.CustomerCode) && !existingCustsOD.Contains(d.CustomerCode))
                {
                    _db.Customers.Add(new Customer
                    {
                        CustomerCode = d.CustomerCode,
                        Name = d.CustomerName ?? "",
                        District = d.District ?? "",
                        Province = d.Province ?? ""
                    });
                    existingCustsOD.Add(d.CustomerCode);
                }
            }
            // Update existing customers if import has a longer name
            foreach (var cust in existingCustEntitiesOD)
            {
                if (bestNameMapOD.TryGetValue(cust.CustomerCode, out var newNameOD) && newNameOD.Length > (cust.Name?.Length ?? 0))
                    cust.Name = newNameOD;
            }
            await _db.SaveChangesAsync();

            // Clear old outstanding debts (100% overwrite)
            _db.OutstandingDebts.RemoveRange(_db.OutstandingDebts.Where(d => d.Status == DebtStatus.Outstanding));
            await _db.SaveChangesAsync();

            // Insert new cleaned debts
            int count = 0;
            foreach (var d in cleanedDebts)
            {
                _db.OutstandingDebts.Add(d);
                count++;
                if (count % 200 == 0)
                {
                    try { await _db.SaveChangesAsync(); }
                    catch { _db.ChangeTracker.Clear(); }
                }
            }
            await _db.SaveChangesAsync();

            return (count, maxDate, cleanedDebts.Take(10).ToList());
        }

        // ==========================================
        // PREVIEW / CONFIRM RECEIPT MATCH FLOW
        // ==========================================
        private static readonly Dictionary<string, ReceiptPreviewResult> _receiptPreviews = new();

        public async Task<ReceiptPreviewResult> PreviewReceiptMatchAsync(Stream stream, string fileName = "")
        {
            var conf = new ExcelReaderConfiguration { FallbackEncoding = Encoding.GetEncoding(874) };
            using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? ExcelReaderFactory.CreateCsvReader(stream, conf)
                : ExcelReaderFactory.CreateReader(stream, conf);

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];

            var result = new ReceiptPreviewResult { FileName = fileName };
            if (tbl == null || tbl.Rows.Count < 2)
            {
                result.PreviewId = Guid.NewGuid().ToString("N");
                lock (_receiptPreviews) { _receiptPreviews[result.PreviewId] = result; }
                return result;
            }

            var cleanedRows = CleanAndParseReceipts(tbl, out DateTime maxDate);
            result.LatestDate = maxDate;

            var billNosInFile = cleanedRows.Select(r => r.BillNo).Distinct().ToList();
            var existingBillsWithReceipt = await _db.SalesBills
                .Where(b => billNosInFile.Contains(b.BillNo) && b.ReceiptNo != null && b.ReceiptNo != "")
                .ToDictionaryAsync(b => b.BillNo, b => b.ReceiptNo);

            foreach (var pr in cleanedRows)
            {
                if (existingBillsWithReceipt.TryGetValue(pr.BillNo, out var existingReceipt))
                {
                    pr.ExistingReceiptNo = existingReceipt ?? "";
                    pr.IsDuplicate = true;
                    result.Duplicates.Add(pr);
                }
                else
                {
                    result.NewRows.Add(pr);
                }
            }

            result.PreviewId = Guid.NewGuid().ToString("N");
            lock (_receiptPreviews) { _receiptPreviews[result.PreviewId] = result; }
            return result;
        }

        public static ReceiptPreviewResult? GetReceiptPreview(string previewId)
        {
            lock (_receiptPreviews) { return _receiptPreviews.TryGetValue(previewId, out var r) ? r : null; }
        }

        public static void RemoveReceiptPreview(string previewId)
        {
            lock (_receiptPreviews) { _receiptPreviews.Remove(previewId); }
        }

        public async Task<(int matched, int notFound, DateTime latestDate)> ConfirmReceiptMatchAsync(
            string previewId, bool updateDuplicates, List<string>? selectedDuplicateBillNos = null)
        {
            ReceiptPreviewResult? preview;
            lock (_receiptPreviews) { _receiptPreviews.TryGetValue(previewId, out preview); }

            var rowsToProcess = new List<ReceiptPreviewRow>();
            DateTime latestDate = preview?.LatestDate ?? DateTime.MinValue;

            if (preview != null)
            {
                rowsToProcess.AddRange(preview.NewRows);
                if (updateDuplicates)
                {
                    var dups = selectedDuplicateBillNos != null && selectedDuplicateBillNos.Count > 0
                        ? preview.Duplicates.Where(d => selectedDuplicateBillNos.Contains(d.BillNo)).ToList()
                        : preview.Duplicates;
                    rowsToProcess.AddRange(dups);
                }
            }

            int matched = 0, notFound = 0;

            foreach (var pr in rowsToProcess)
            {
                bool isMatch = false;

                var existingBills = await _db.SalesBills.Where(b => b.BillNo == pr.BillNo).ToListAsync();
                if (!string.IsNullOrEmpty(pr.CustomerCode))
                    existingBills = existingBills.Where(b => b.CustomerCode == pr.CustomerCode).ToList();

                if (existingBills.Any())
                {
                    foreach (var b in existingBills)
                    {
                        b.ReceiptNo = pr.ReceiptNo;
                        if (pr.ReceiptDate != DateTime.MinValue) b.ReceiptDate = pr.ReceiptDate;
                        b.IsFullyPaid = true;
                    }
                    isMatch = true;
                }

                var existingDebts = await _db.OutstandingDebts.Where(d => d.BillNo == pr.BillNo).ToListAsync();
                if (!string.IsNullOrEmpty(pr.CustomerCode))
                    existingDebts = existingDebts.Where(d => d.CustomerCode == pr.CustomerCode).ToList();

                if (existingDebts.Any())
                {
                    foreach (var d in existingDebts)
                    {
                        d.ReceiptNo = pr.ReceiptNo;
                        if (pr.ReceiptDate != DateTime.MinValue) d.ReceiptDate = pr.ReceiptDate;
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
            if (preview != null) RemoveReceiptPreview(previewId);
            return (matched, notFound, latestDate);
        }

        // ==========================================
        // DATE & NUMBER PARSING UTILITIES
        // ==========================================
        public static DateTime ParseThaiDate(object? obj)
        {
            string s = obj?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
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
            if (DateTime.TryParse(s, out var dt))
            {
                if (dt.Year > 2500) dt = dt.AddYears(-543);
                return dt;
            }
            return DateTime.MinValue;
        }

        public static decimal ParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Replace(",", "").Trim();
            return decimal.TryParse(s, out decimal result) ? result : 0;
        }

        public static int ParseInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var match = Regex.Match(s, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }
    }

    public class ReceiptPreviewRow
    {
        public string ReceiptNo { get; set; } = "";
        public string NewReceiptNo { get => ReceiptNo; set => ReceiptNo = value; }
        public string BillNo { get; set; } = "";
        public DateTime ReceiptDate { get; set; }
        public string ReceiptDateStr { get; set; } = "";
        public string CustomerCode { get; set; } = "";
        public decimal Amount { get; set; }
        public string ExistingReceiptNo { get; set; } = "";
        public bool IsDuplicate { get; set; }
    }

    public class ReceiptPreviewResult
    {
        public string PreviewId { get; set; } = "";
        public string FileName { get; set; } = "";
        public DateTime LatestDate { get; set; }
        public List<ReceiptPreviewRow> NewRows { get; set; } = new();
        public List<ReceiptPreviewRow> Duplicates { get; set; } = new();
    }
}
