using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class CreditNoteController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public CreditNoteController(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // ==========================================
        // 1. รายการใบลดหนี้ (Index)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? salesRep,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 30)
        {
            var q = _db.CreditNotes.AsNoTracking()
                .Include(c => c.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                q = q.Where(c => c.CreditNoteNo.Contains(search) ||
                                 c.CustomerName.Contains(search) ||
                                 c.CustomerCode.Contains(search) ||
                                 c.OriginalBillNo.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(salesRep))
            {
                q = q.Where(c => c.SalesRep == salesRep || c.OriginalSalesRep == salesRep);
            }

            if (fromDate.HasValue)
            {
                var fDate = fromDate.Value.Date;
                q = q.Where(c => c.CreditNoteDate >= fDate);
            }

            if (toDate.HasValue)
            {
                var tDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                q = q.Where(c => c.CreditNoteDate <= tDate);
            }

            // Calculate KPI Totals
            var allFiltered = await q.ToListAsync();
            ViewBag.TotalCount = allFiltered.Count;
            ViewBag.TotalBeforeVat = allFiltered.Sum(c => c.TotalAmountBeforeVat);
            ViewBag.TotalVat = allFiltered.Sum(c => c.VatAmount);
            ViewBag.GrandTotal = allFiltered.Sum(c => c.GrandTotal);

            // Pagination
            var pagedData = allFiltered
                .OrderByDescending(c => c.CreditNoteDate)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)allFiltered.Count / pageSize);
            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            // Sales reps list for filter
            ViewBag.SalesReps = await SalesRepHelper.GetAllKnownSalesRepsAsync(_db, _cache);

            return View(pagedData);
        }

        // ==========================================
        // 2. หน้าจอรับข้อมูลและออกใบลดหนี้ (Create)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(string? customerCode = null, string? billNo = null)
        {
            var reps = await SalesRepHelper.GetAllKnownSalesRepsAsync(_db, _cache);
            ViewBag.SalesReps = reps;

            // Load master customers for autocomplete
            var customers = await _db.Customers.AsNoTracking()
                .OrderBy(c => c.CustomerCode)
                .Select(c => new { c.CustomerCode, c.Name, c.Address, c.TaxId, c.Branch })
                .Take(2000)
                .ToListAsync();
            ViewBag.Customers = customers;

            // Load master products for autocomplete
            var products = await LoadMasterProductsAsync();
            ViewBag.Products = products;

            // Pre-selected customer or bill if provided
            ViewBag.PreCustomerCode = customerCode ?? "";
            ViewBag.PreBillNo = billNo ?? "";

            return View();
        }

        // ==========================================
        // 3. API: ค้นหาราคาเฉลี่ยสุทธิและยุบรวมยอด Group By
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CalculateReturnSummary([FromBody] ReturnCalcRequest request)
        {
            if (request == null || request.RawItems == null || !request.RawItems.Any())
            {
                return Json(new { success = false, message = "ไม่พบรายการคืนสินค้า" });
            }

            string custCode = (request.CustomerCode ?? "").Trim();
            string custName = (request.CustomerName ?? "").Trim();

            // Try to resolve customer if only code or name is provided
            Customer? customer = null;
            if (!string.IsNullOrEmpty(custCode))
            {
                customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerCode == custCode);
            }
            if (customer == null && !string.IsNullOrEmpty(custName))
            {
                customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Name.Contains(custName));
            }

            if (customer != null)
            {
                custCode = customer.CustomerCode;
                custName = customer.Name;
            }

            // Automatic Group By productCode across all warehouse entries
            var groupedRaw = request.RawItems
                .Where(r => !string.IsNullOrWhiteSpace(r.ProductCode) && r.Qty > 0)
                .GroupBy(r => r.ProductCode.Trim().ToUpperInvariant())
                .ToList();

            var resultItems = new List<CalculatedReturnItemDto>();
            string defaultSalesRep = "";
            string detectedOriginalBillNo = "";
            DateTime? detectedOriginalBillDate = null;
            decimal detectedOriginalBillTotal = 0;

            foreach (var group in groupedRaw)
            {
                string prodCode = group.Key;
                decimal totalQty = group.Sum(g => g.Qty);
                string prodName = group.FirstOrDefault(g => !string.IsNullOrEmpty(g.ProductName))?.ProductName ?? "";
                string unit = group.FirstOrDefault(g => !string.IsNullOrEmpty(g.Unit))?.Unit ?? "ชิ้น";

                // Build warehouse breakdown text
                var whList = group.GroupBy(g => g.WarehouseNo)
                                  .OrderBy(g => g.Key)
                                  .Select(g => $"คลัง {g.Key}: {g.Sum(x => x.Qty):N0}")
                                  .ToList();
                string whBreakdown = string.Join(", ", whList);

                // Find latest sales bill for this customer that includes this product
                var query = _db.SalesBills.AsNoTracking()
                    .Include(b => b.Items)
                    .Where(b => !b.IsCancelled);

                if (!string.IsNullOrEmpty(custCode))
                {
                    query = query.Where(b => b.CustomerCode == custCode);
                }
                else if (!string.IsNullOrEmpty(custName))
                {
                    query = query.Where(b => b.CustomerName.Contains(custName));
                }

                // Match product code in bill items
                var matchingBill = await query
                    .Where(b => b.Items.Any(i => i.ProductCode.ToUpper() == prodCode || prodCode.Contains(i.ProductCode.ToUpper())))
                    .OrderByDescending(b => b.BillDate)
                    .FirstOrDefaultAsync();

                decimal originalNetUnitPrice = 0;
                string refBillNo = "";
                bool billNotFound = false;

                if (matchingBill != null)
                {
                    refBillNo = matchingBill.BillNo;
                    if (string.IsNullOrEmpty(defaultSalesRep))
                    {
                        defaultSalesRep = matchingBill.SalesRep;
                        detectedOriginalBillNo = matchingBill.BillNo;
                        detectedOriginalBillDate = matchingBill.BillDate;
                        detectedOriginalBillTotal = matchingBill.TotalAmount;
                    }

                    // Formula: [ ยอดรวมสุทธิหลังหักส่วนลดของบิลเดิม ÷ จำนวนสินค้ารวมทั้งหมดในบิล (หลัก+แถม) ]
                    decimal totalBillQty = matchingBill.Items.Sum(i => i.Qty);
                    if (totalBillQty > 0 && matchingBill.TotalAmount > 0)
                    {
                        originalNetUnitPrice = Math.Round(matchingBill.TotalAmount / totalBillQty, 4);
                    }
                    else
                    {
                        // Fallback to item specific net unit price: (Item.Amount / Item.Qty)
                        var itemMatch = matchingBill.Items.FirstOrDefault(i => i.ProductCode.ToUpper() == prodCode || prodCode.Contains(i.ProductCode.ToUpper()));
                        if (itemMatch != null && itemMatch.Qty > 0)
                        {
                            originalNetUnitPrice = Math.Round(itemMatch.Amount / itemMatch.Qty, 4);
                        }
                    }

                    if (string.IsNullOrEmpty(prodName))
                    {
                        var itemMatch = matchingBill.Items.FirstOrDefault(i => i.ProductCode.ToUpper() == prodCode);
                        if (itemMatch != null)
                        {
                            prodName = itemMatch.ProductName;
                            unit = itemMatch.Unit;
                        }
                    }
                }
                else
                {
                    // No sales bill found for this product and customer
                    billNotFound = true;
                    originalNetUnitPrice = 0;
                }

                // If product name is still missing, lookup in master
                if (string.IsNullOrEmpty(prodName))
                {
                    var masterProd = (await LoadMasterProductsAsync()).FirstOrDefault(p => p.Code.ToUpper() == prodCode);
                    if (masterProd != null)
                    {
                        prodName = masterProd.Name;
                        unit = masterProd.Unit;
                    }
                }

                decimal amountBeforeVat = Math.Round(totalQty * originalNetUnitPrice, 2);
                decimal vatAmount = Math.Round(amountBeforeVat * 0.07m, 2);
                decimal totalAmount = amountBeforeVat + vatAmount;

                resultItems.Add(new CalculatedReturnItemDto
                {
                    ProductCode = prodCode,
                    ProductName = string.IsNullOrEmpty(prodName) ? $"สินค้า {prodCode}" : prodName,
                    Unit = string.IsNullOrEmpty(unit) ? "ชิ้น" : unit,
                    TotalReturnQty = totalQty,
                    OriginalNetUnitPrice = originalNetUnitPrice,
                    UnitPrice = originalNetUnitPrice, // default to original
                    IsPriceOverridden = false,
                    AmountBeforeVat = amountBeforeVat,
                    VatAmount = vatAmount,
                    TotalAmount = totalAmount,
                    ReferenceBillNo = refBillNo,
                    BillNotFound = billNotFound,
                    WarehouseBreakdown = whBreakdown,
                    Entries = group.Select(g => new WarehouseEntryDto
                    {
                        WarehouseNo = g.WarehouseNo,
                        ExpiryDate = g.ExpiryDate,
                        Qty = g.Qty,
                        LotNo = g.LotNo ?? ""
                    }).ToList()
                });
            }

            decimal grandBeforeVat = resultItems.Sum(i => i.AmountBeforeVat);
            decimal grandVat = resultItems.Sum(i => i.VatAmount);
            decimal grandTotal = grandBeforeVat + grandVat;

            return Json(new
            {
                success = true,
                customer = new
                {
                    code = customer?.CustomerCode ?? custCode,
                    name = customer?.Name ?? custName,
                    address = customer?.Address ?? "",
                    taxId = customer?.TaxId ?? "",
                    branch = customer?.Branch ?? "สำนักงานใหญ่"
                },
                defaultSalesRep = defaultSalesRep,
                detectedOriginalBillNo = detectedOriginalBillNo,
                detectedOriginalBillDate = detectedOriginalBillDate?.ToString("yyyy-MM-dd"),
                detectedOriginalBillTotal = detectedOriginalBillTotal,
                items = resultItems,
                summary = new
                {
                    totalBeforeVat = grandBeforeVat,
                    vatAmount = grandVat,
                    grandTotal = grandTotal,
                    hasNotFoundBill = resultItems.Any(i => i.BillNotFound)
                }
            });
        }

        // ==========================================
        // 4. API: นำเข้าไฟล์ Excel / CSV
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ParseExcel(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "กรุณาเลือกไฟล์ Excel (.xlsx, .xls) หรือ CSV" });
            }

            var entries = new List<RawReturnEntry>();
            string detectedCustomerName = "";
            string detectedCustomerCode = "";

            try
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                using var stream = file.OpenReadStream();

                if (ext == ".csv")
                {
                    using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                    string? headerLine = await reader.ReadLineAsync();
                    if (headerLine == null) return Json(new { success = false, message = "ไฟล์ CSV ว่างเปล่า" });

                    var headers = headerLine.Split(',').Select(h => h.Trim().Replace("\"", "")).ToList();
                    int colCust = headers.FindIndex(h => h.Contains("ร้าน") || h.Contains("ลูกค้า") || h.Contains("Customer"));
                    int colCode = headers.FindIndex(h => h.Contains("รหัสสินค้า") || h.Contains("Item") || h.Contains("Code") || h.Contains("รหัส"));
                    int colName = headers.FindIndex(h => h.Contains("ชื่อสินค้า") || h.Contains("รายละเอียด") || h.Contains("Description"));
                    int colWh1 = headers.FindIndex(h => h.Contains("คลัง 1") || h.Contains("คลัง1") || h.Contains("WH1"));
                    int colWh3 = headers.FindIndex(h => h.Contains("คลัง 3") || h.Contains("คลัง3") || h.Contains("WH3"));
                    int colWh7 = headers.FindIndex(h => h.Contains("คลัง 7") || h.Contains("คลัง7") || h.Contains("WH7"));
                    int colQty = headers.FindIndex(h => h.Contains("จำนวน") || h.Contains("Qty") || h.Contains("คืน"));
                    int colExp = headers.FindIndex(h => h.Contains("หมดอายุ") || h.Contains("Exp"));

                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(',').Select(p => p.Trim().Replace("\"", "")).ToArray();
                        if (parts.Length <= colCode || colCode < 0) continue;

                        string pCode = parts[colCode];
                        if (string.IsNullOrWhiteSpace(pCode)) continue;
                        string pName = (colName >= 0 && parts.Length > colName) ? parts[colName] : "";

                        if (colCust >= 0 && parts.Length > colCust && string.IsNullOrEmpty(detectedCustomerName))
                        {
                            detectedCustomerName = parts[colCust];
                        }

                        // Check warehouse specific columns
                        bool hasWhCols = false;
                        if (colWh1 >= 0 && parts.Length > colWh1 && decimal.TryParse(parts[colWh1], out decimal q1) && q1 > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "1", Qty = q1 });
                            hasWhCols = true;
                        }
                        if (colWh3 >= 0 && parts.Length > colWh3 && decimal.TryParse(parts[colWh3], out decimal q3) && q3 > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "3", Qty = q3 });
                            hasWhCols = true;
                        }
                        if (colWh7 >= 0 && parts.Length > colWh7 && decimal.TryParse(parts[colWh7], out decimal q7) && q7 > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "7", Qty = q7 });
                            hasWhCols = true;
                        }

                        if (!hasWhCols && colQty >= 0 && parts.Length > colQty && decimal.TryParse(parts[colQty], out decimal qTotal) && qTotal > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "1", Qty = qTotal });
                        }
                    }
                }
                else
                {
                    // EPPlus for Excel
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลในแผ่นงาน Excel" });
                    }

                    int rows = worksheet.Dimension.End.Row;
                    int cols = worksheet.Dimension.End.Column;

                    // Search for headers in first 5 rows
                    int headerRow = 1;
                    int colCust = -1, colCode = -1, colName = -1, colWh1 = -1, colWh3 = -1, colWh7 = -1, colQty = -1;

                    for (int r = 1; r <= Math.Min(5, rows); r++)
                    {
                        for (int c = 1; c <= cols; c++)
                        {
                            var text = worksheet.Cells[r, c].Text?.Trim() ?? "";
                            if (colCode == -1 && (text.Contains("รหัสสินค้า") || text.Contains("Item") || text.Contains("Code") || text.Contains("รหัส"))) colCode = c;
                            if (colName == -1 && (text.Contains("ชื่อสินค้า") || text.Contains("รายละเอียด") || text.Contains("Description"))) colName = c;
                            if (colCust == -1 && (text.Contains("ร้าน") || text.Contains("ลูกค้า") || text.Contains("Customer"))) colCust = c;
                            if (colWh1 == -1 && (text.Contains("คลัง 1") || text.Contains("คลัง1") || text.Contains("WH1"))) colWh1 = c;
                            if (colWh3 == -1 && (text.Contains("คลัง 3") || text.Contains("คลัง3") || text.Contains("WH3"))) colWh3 = c;
                            if (colWh7 == -1 && (text.Contains("คลัง 7") || text.Contains("คลัง7") || text.Contains("WH7"))) colWh7 = c;
                            if (colQty == -1 && (text.Contains("จำนวน") || text.Contains("Qty") || text.Contains("คืน"))) colQty = c;
                        }
                        if (colCode != -1) { headerRow = r; break; }
                    }

                    if (colCode == -1) colCode = 1; // fallback first column

                    for (int r = headerRow + 1; r <= rows; r++)
                    {
                        string pCode = worksheet.Cells[r, colCode].Text?.Trim() ?? "";
                        if (string.IsNullOrWhiteSpace(pCode)) continue;

                        string pName = colName > 0 ? worksheet.Cells[r, colName].Text?.Trim() ?? "" : "";
                        if (colCust > 0 && string.IsNullOrEmpty(detectedCustomerName))
                        {
                            detectedCustomerName = worksheet.Cells[r, colCust].Text?.Trim() ?? "";
                        }

                        bool added = false;
                        if (colWh1 > 0 && decimal.TryParse(worksheet.Cells[r, colWh1].Text, out decimal q1) && q1 > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "1", Qty = q1 });
                            added = true;
                        }
                        if (colWh3 > 0 && decimal.TryParse(worksheet.Cells[r, colWh3].Text, out decimal q3) && q3 > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "3", Qty = q3 });
                            added = true;
                        }
                        if (colWh7 > 0 && decimal.TryParse(worksheet.Cells[r, colWh7].Text, out decimal q7) && q7 > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "7", Qty = q7 });
                            added = true;
                        }

                        if (!added && colQty > 0 && decimal.TryParse(worksheet.Cells[r, colQty].Text, out decimal qTot) && qTot > 0)
                        {
                            entries.Add(new RawReturnEntry { ProductCode = pCode, ProductName = pName, WarehouseNo = "1", Qty = qTot });
                        }
                    }
                }

                if (!entries.Any())
                {
                    return Json(new { success = false, message = "ไม่พบรายการรหัสสินค้าหรือจำนวนคืนในไฟล์ที่เลือก" });
                }

                return Json(new
                {
                    success = true,
                    detectedCustomer = detectedCustomerName,
                    entries = entries
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการอ่านไฟล์: " + ex.Message });
            }
        }

        // ==========================================
        // 5. API: สแกนภาพ / OCR Text Parser
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ParseOcr([FromBody] OcrTextRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RawText))
            {
                return Json(new { success = false, message = "ไม่พบข้อความที่สแกนได้" });
            }

            var text = request.RawText;
            var entries = new List<RawReturnEntry>();
            string detectedCustomer = "";

            // Try to find customer pattern: "ร้าน", "ลูกค้า", "ชื่อร้าน", "บจก."
            var custMatch = Regex.Match(text, @"(?:ร้าน|ลูกค้า|ชื่อร้าน|บจก\.|บริษัท)\s*[:\s]?\s*([^\r\n]+)");
            if (custMatch.Success)
            {
                detectedCustomer = custMatch.Groups[1].Value.Trim();
            }

            // Find product code patterns (e.g. 100xxx, or 5-7 digit numbers) with nearby quantities
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // Pattern: [Code 100xxx] ... [qty]
                var codeMatch = Regex.Match(line, @"\b(100\d{3}(?:-\d+)?)\b");
                if (codeMatch.Success)
                {
                    string pCode = codeMatch.Groups[1].Value;
                    // Extract numbers after code as quantity
                    var numMatches = Regex.Matches(line.Substring(codeMatch.Index + codeMatch.Length), @"\b\d+(?:\.\d+)?\b");
                    if (numMatches.Count > 0 && decimal.TryParse(numMatches[0].Value, out decimal qty) && qty > 0)
                    {
                        entries.Add(new RawReturnEntry
                        {
                            ProductCode = pCode,
                            ProductName = "",
                            WarehouseNo = "1",
                            Qty = qty
                        });
                    }
                }
            }

            return Json(new
            {
                success = true,
                detectedCustomer = detectedCustomer,
                entries = entries
            });
        }

        // ==========================================
        // 6. บันทึกใบลดหนี้ (Save Credit Note)
        // ==========================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCreditNote([FromBody] SaveCreditNoteRequest model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return Json(new { success = false, message = "ข้อมูลไม่สมบูรณ์ หรือไม่มีรายการสินค้าคืน" });
            }

            // Check if price override occurred and validate mandatory reason
            bool hasPriceOverride = model.Items.Any(i => i.IsPriceOverridden || i.UnitPrice != i.OriginalNetUnitPrice);
            if (hasPriceOverride && string.IsNullOrWhiteSpace(model.PriceOverrideReason))
            {
                return Json(new { success = false, message = "มีการแก้ไขราคาต่อหน่วย กรุณาระบุ 'เหตุผลในการแก้ไขราคา' เพื่อบันทึกประวัติการตรวจสอบ (Audit Log)" });
            }

            try
            {
                // Auto-numbering: CN + YY + "-" + MM + "-" + Running 4 digits (e.g. CN26-10-0001)
                var now = DateTime.Now;
                var yy = (now.Year % 100).ToString("D2");
                var mm = now.Month.ToString("D2");
                var prefix = $"CN{yy}-{mm}-";

                // Get max running sequence for current month
                var existingCNs = await _db.CreditNotes.AsNoTracking()
                    .Where(c => c.CreditNoteNo.StartsWith(prefix))
                    .Select(c => c.CreditNoteNo)
                    .ToListAsync();

                int maxSeq = 0;
                foreach (var cn in existingCNs)
                {
                    var parts = cn.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int seq) && seq > maxSeq)
                    {
                        maxSeq = seq;
                    }
                }
                int nextSeq = maxSeq + 1;
                string newCreditNoteNo = $"{prefix}{nextSeq:D4}";

                // Calculate totals
                decimal totalBeforeVat = model.Items.Sum(i => Math.Round(i.TotalReturnQty * i.UnitPrice, 2));
                decimal vatAmount = Math.Round(totalBeforeVat * 0.07m, 2);
                decimal grandTotal = totalBeforeVat + vatAmount;

                // Lookup customer for branch / address
                var cust = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerCode == model.CustomerCode);

                // Original bill values for tax compliance (ม.86/10)
                decimal origBillAmount = model.OriginalBillAmount;
                decimal diffAmount = totalBeforeVat;
                decimal correctAmount = Math.Max(0, origBillAmount - diffAmount);

                var creditNote = new CreditNote
                {
                    CreditNoteNo = newCreditNoteNo,
                    CreditNoteDate = model.CreditNoteDate ?? DateTime.Today,
                    CustomerCode = model.CustomerCode ?? "",
                    CustomerName = model.CustomerName ?? (cust?.Name ?? ""),
                    CustomerAddress = model.CustomerAddress ?? (cust?.Address ?? ""),
                    CustomerTaxId = model.CustomerTaxId ?? (cust?.TaxId ?? ""),
                    CustomerBranch = model.CustomerBranch ?? (cust?.Branch ?? "สำนักงานใหญ่"),
                    OriginalBillNo = model.OriginalBillNo ?? "",
                    OriginalBillDate = model.OriginalBillDate,
                    OriginalBillAmount = origBillAmount,
                    CorrectAmount = correctAmount,
                    DifferenceAmount = diffAmount,
                    OriginalSalesRep = model.OriginalSalesRep ?? "",
                    SalesRep = model.SalesRep ?? model.OriginalSalesRep ?? "",
                    CommissionDeductTarget = model.CommissionDeductTarget ?? "OriginalRep", // กฎเหล็ก: ไม่หักเซลส์คนใหม่
                    Reason = string.IsNullOrWhiteSpace(model.Reason) ? "รับคืนสินค้าชำรุด/หมดอายุ/ผิดสเปก" : model.Reason,
                    TotalAmountBeforeVat = totalBeforeVat,
                    VatAmount = vatAmount,
                    GrandTotal = grandTotal,
                    IsPriceOverridden = hasPriceOverride,
                    PriceOverrideReason = model.PriceOverrideReason ?? "",
                    IsArOffset = model.IsArOffset,
                    Status = "Approved",
                    CreatedBy = User.Identity?.Name ?? "Accounting",
                    CreatedAt = DateTime.Now,
                    PaperSizePreference = model.PaperSizePreference ?? "A4",
                    Remark = model.Remark ?? ""
                };

                _db.CreditNotes.Add(creditNote);
                await _db.SaveChangesAsync();

                // Add Items
                foreach (var itemDto in model.Items)
                {
                    decimal lineBefore = Math.Round(itemDto.TotalReturnQty * itemDto.UnitPrice, 2);
                    decimal lineVat = Math.Round(lineBefore * 0.07m, 2);

                    var item = new CreditNoteItem
                    {
                        CreditNoteId = creditNote.Id,
                        ProductCode = itemDto.ProductCode ?? "",
                        ProductName = itemDto.ProductName ?? "",
                        Unit = string.IsNullOrEmpty(itemDto.Unit) ? "ชิ้น" : itemDto.Unit,
                        TotalReturnQty = itemDto.TotalReturnQty,
                        OriginalNetUnitPrice = itemDto.OriginalNetUnitPrice,
                        UnitPrice = itemDto.UnitPrice,
                        IsPriceOverridden = itemDto.IsPriceOverridden || (itemDto.UnitPrice != itemDto.OriginalNetUnitPrice),
                        PriceOverrideReason = model.PriceOverrideReason ?? "",
                        AmountBeforeVat = lineBefore,
                        VatAmount = lineVat,
                        TotalAmount = lineBefore + lineVat,
                        ReferenceBillNo = itemDto.ReferenceBillNo ?? model.OriginalBillNo ?? "",
                        BillNotFound = itemDto.BillNotFound,
                        WarehouseBreakdown = itemDto.WarehouseBreakdown ?? ""
                    };
                    _db.CreditNoteItems.Add(item);

                    // Add warehouse breakdown entries if present
                    if (itemDto.Entries != null)
                    {
                        foreach (var w in itemDto.Entries)
                        {
                            _db.CreditNoteWarehouseEntries.Add(new CreditNoteWarehouseEntry
                            {
                                CreditNoteId = creditNote.Id,
                                ProductCode = itemDto.ProductCode ?? "",
                                ProductName = itemDto.ProductName ?? "",
                                WarehouseNo = w.WarehouseNo ?? "1",
                                ExpiryDate = w.ExpiryDate,
                                Qty = w.Qty,
                                LotNo = w.LotNo ?? ""
                            });
                        }
                    }
                }

                // [CD Organizer Integration Policy]
                // ไม่มีการตัดลดยอดหนี้ในการ์ดลูกหนี้หลังบ้านของเว็บนี้ (No AR Deduction in Web App)
                // บัญชีจะนำตัวเลขราคาเฉลี่ยสุทธิที่พิมพ์ออกไปคีย์บันทึกรับคืนและตัดยอดลูกหนี้ในโปรแกรม CD Organizer เอง
                creditNote.IsArOffset = false;

                // Audit Log
                if (hasPriceOverride)
                {
                    _db.AuditLogs.Add(new AuditLog
                    {
                        Username = User.Identity?.Name ?? "Accounting",
                        Action = "OverrideCreditNotePrice",
                        Detail = $"ใบลดหนี้ {creditNote.CreditNoteNo}: แก้ไขราคาต่อหน่วยโดยบัญชี เหตุผล: {model.PriceOverrideReason}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "Accounting",
                    Action = "CreateCreditNote",
                    Detail = $"ออกใบลดหนี้เลขที่ {creditNote.CreditNoteNo} ลูกค้า: {creditNote.CustomerName} ผู้แทนขาย: {creditNote.SalesRep} ยอดรวม: {creditNote.GrandTotal:N2} บาท (เซลส์เดิมตามบิล: {creditNote.OriginalSalesRep}, หักคอมมิชชัน: {creditNote.CommissionDeductTarget})",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    creditNoteId = creditNote.Id,
                    creditNoteNo = creditNote.CreditNoteNo,
                    message = $"ออกใบลดหนี้เลขที่ {creditNote.CreditNoteNo} เรียบร้อยแล้ว"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึก: " + ex.Message });
            }
        }

        // ==========================================
        // 7. ดูรายละเอียดใบลดหนี้ (Detail)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var cn = await _db.CreditNotes
                .Include(c => c.Items)
                .Include(c => c.WarehouseEntries)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cn == null) return NotFound();

            ViewBag.ThaiBahtText = ThaiBahtTextHelper.ToThaiBahtText(cn.GrandTotal);
            return View(cn);
        }

        // ==========================================
        // 8. พิมพ์เอกสารใบลดหนี้ (Print: A4 & 9x11 Continuous)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Print(int id, string paperSize = "A4")
        {
            var cn = await _db.CreditNotes
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cn == null) return NotFound();

            ViewBag.PaperSize = paperSize == "Continuous9x11" ? "Continuous9x11" : "A4";
            ViewBag.ThaiBahtText = ThaiBahtTextHelper.ToThaiBahtText(cn.GrandTotal);

            return View(cn);
        }

        // ==========================================
        // 9. รายงานการคืนสินค้าและใบลดหนี้ (Report Dashboard)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Report(
            DateTime? fromDate,
            DateTime? toDate,
            string? customer,
            string? productCode,
            string? salesRep)
        {
            var q = _db.CreditNotes.AsNoTracking()
                .Include(c => c.Items)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var f = fromDate.Value.Date;
                q = q.Where(c => c.CreditNoteDate >= f);
            }
            if (toDate.HasValue)
            {
                var t = toDate.Value.Date.AddDays(1).AddTicks(-1);
                q = q.Where(c => c.CreditNoteDate <= t);
            }
            if (!string.IsNullOrWhiteSpace(customer))
            {
                customer = customer.Trim();
                q = q.Where(c => c.CustomerName.Contains(customer) || c.CustomerCode.Contains(customer));
            }
            if (!string.IsNullOrWhiteSpace(productCode))
            {
                productCode = productCode.Trim();
                q = q.Where(c => c.Items.Any(i => i.ProductCode.Contains(productCode)));
            }
            if (!string.IsNullOrWhiteSpace(salesRep))
            {
                salesRep = salesRep.Trim();
                q = q.Where(c => c.SalesRep == salesRep || c.OriginalSalesRep == salesRep);
            }

            var list = await q.OrderByDescending(c => c.CreditNoteDate).ThenByDescending(c => c.Id).ToListAsync();

            // Summary Stats
            ViewBag.TotalCount = list.Count;
            ViewBag.TotalBeforeVat = list.Sum(c => c.TotalAmountBeforeVat);
            ViewBag.TotalVat = list.Sum(c => c.VatAmount);
            ViewBag.GrandTotal = list.Sum(c => c.GrandTotal);

            // Group by Sales Rep for Commission (กฎเหล็ก: ไม่หักยอดเซลส์คนใหม่)
            // เซลส์ที่จะโดนหักคือ OriginalSalesRep (เซลส์เดิมตามบิล) หรือถ้าเป็น CompanyCentral ก็ไม่หักใคร
            var commissionGroups = list
                .GroupBy(c => !string.IsNullOrEmpty(c.OriginalSalesRep) ? c.OriginalSalesRep : c.SalesRep)
                .Select(g => new SalesRepCommissionSummary
                {
                    SalesRepName = g.Key,
                    TotalCreditNotes = g.Count(),
                    TotalDeductionBeforeVat = g.Where(c => c.CommissionDeductTarget == "OriginalRep").Sum(c => c.TotalAmountBeforeVat),
                    TotalDeductionGrand = g.Where(c => c.CommissionDeductTarget == "OriginalRep").Sum(c => c.GrandTotal),
                    CentralAbsorbedAmount = g.Where(c => c.CommissionDeductTarget != "OriginalRep").Sum(c => c.GrandTotal),
                    ProtectedCurrentRepCount = g.Count(c => c.SalesRep != c.OriginalSalesRep && !string.IsNullOrEmpty(c.SalesRep))
                })
                .OrderByDescending(s => s.TotalDeductionGrand)
                .ToList();

            ViewBag.CommissionSummaries = commissionGroups;

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Customer = customer;
            ViewBag.ProductCode = productCode;
            ViewBag.SalesRep = salesRep;
            ViewBag.AllSalesReps = await SalesRepHelper.GetAllKnownSalesRepsAsync(_db, _cache);

            return View(list);
        }

        // ==========================================
        // 10. ส่งออกไฟล์ Excel (ExportReportExcel)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportReportExcel(
            DateTime? fromDate,
            DateTime? toDate,
            string? customer,
            string? productCode,
            string? salesRep)
        {
            var q = _db.CreditNotes.AsNoTracking()
                .Include(c => c.Items)
                .AsQueryable();

            if (fromDate.HasValue) q = q.Where(c => c.CreditNoteDate >= fromDate.Value.Date);
            if (toDate.HasValue) q = q.Where(c => c.CreditNoteDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            if (!string.IsNullOrWhiteSpace(customer)) q = q.Where(c => c.CustomerName.Contains(customer) || c.CustomerCode.Contains(customer));
            if (!string.IsNullOrWhiteSpace(productCode)) q = q.Where(c => c.Items.Any(i => i.ProductCode.Contains(productCode)));
            if (!string.IsNullOrWhiteSpace(salesRep)) q = q.Where(c => c.SalesRep == salesRep || c.OriginalSalesRep == salesRep);

            var list = await q.OrderByDescending(c => c.CreditNoteDate).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

            // Sheet 1: รายการใบลดหนี้
            var ws1 = package.Workbook.Worksheets.Add("รายงานใบลดหนี้");
            ws1.Cells["A1:J1"].Merge = true;
            ws1.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด - รายงานการคืนสินค้าและใบลดหนี้ (Credit Note Report)";
            ws1.Cells["A1"].Style.Font.Bold = true;
            ws1.Cells["A1"].Style.Font.Size = 14;
            ws1.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.DarkSlateGray);

            string[] headers1 = { "ลำดับ", "เลขที่ใบลดหนี้", "วันที่เอกสาร", "รหัสลูกค้า", "ชื่อร้านค้า", "อ้างอิงบิลเดิม", "ผู้แทนขายปัจจุบัน", "ผู้แทนขายเดิมตามบิล", "เป้าหมายหักคอม", "ยอดก่อน VAT", "VAT 7%", "ยอดรวมสุทธิ" };
            for (int i = 0; i < headers1.Length; i++)
            {
                ws1.Cells[3, i + 1].Value = headers1[i];
                ws1.Cells[3, i + 1].Style.Font.Bold = true;
                ws1.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws1.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(13, 148, 136));
                ws1.Cells[3, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            int row = 4;
            int idx = 1;
            foreach (var item in list)
            {
                ws1.Cells[row, 1].Value = idx++;
                ws1.Cells[row, 2].Value = item.CreditNoteNo;
                ws1.Cells[row, 3].Value = item.CreditNoteDate.ToString("dd/MM/yyyy");
                ws1.Cells[row, 4].Value = item.CustomerCode;
                ws1.Cells[row, 5].Value = item.CustomerName;
                ws1.Cells[row, 6].Value = item.OriginalBillNo;
                ws1.Cells[row, 7].Value = item.SalesRep;
                ws1.Cells[row, 8].Value = item.OriginalSalesRep;
                ws1.Cells[row, 9].Value = item.CommissionDeductTarget == "OriginalRep" ? "หักเซลส์เดิม (ไม่หักคนใหม่)" : "ส่วนกลางบริษัท";
                ws1.Cells[row, 10].Value = item.TotalAmountBeforeVat;
                ws1.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";
                ws1.Cells[row, 11].Value = item.VatAmount;
                ws1.Cells[row, 11].Style.Numberformat.Format = "#,##0.00";
                ws1.Cells[row, 12].Value = item.GrandTotal;
                ws1.Cells[row, 12].Style.Numberformat.Format = "#,##0.00";
                row++;
            }

            // Total row
            ws1.Cells[row, 1, row, 9].Merge = true;
            ws1.Cells[row, 1].Value = "รวมทั้งสิ้น";
            ws1.Cells[row, 1].Style.Font.Bold = true;
            ws1.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[row, 10].Formula = $"SUM(J4:J{row - 1})";
            ws1.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";
            ws1.Cells[row, 10].Style.Font.Bold = true;
            ws1.Cells[row, 11].Formula = $"SUM(K4:K{row - 1})";
            ws1.Cells[row, 11].Style.Numberformat.Format = "#,##0.00";
            ws1.Cells[row, 11].Style.Font.Bold = true;
            ws1.Cells[row, 12].Formula = $"SUM(L4:L{row - 1})";
            ws1.Cells[row, 12].Style.Numberformat.Format = "#,##0.00";
            ws1.Cells[row, 12].Style.Font.Bold = true;
            ws1.Cells[3, 1, row, 12].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            ws1.Cells[3, 1, row, 12].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws1.Cells.AutoFitColumns();

            // Sheet 2: รายละเอียดสินค้าคืน
            var ws2 = package.Workbook.Worksheets.Add("รายละเอียดสินค้าคืน");
            string[] headers2 = { "เลขที่ใบลดหนี้", "รหัสสินค้า", "ชื่อสินค้า", "จำนวนคืน", "หน่วย", "ราคาต่อหน่วย", "ยอดเงินก่อน VAT", "VAT 7%", "ยอดรวม", "แยกคลัง", "แก้ไขราคา" };
            for (int i = 0; i < headers2.Length; i++)
            {
                ws2.Cells[1, i + 1].Value = headers2[i];
                ws2.Cells[1, i + 1].Style.Font.Bold = true;
                ws2.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws2.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(15, 118, 110));
                ws2.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }
            int r2 = 2;
            foreach (var cn in list)
            {
                foreach (var it in cn.Items)
                {
                    ws2.Cells[r2, 1].Value = cn.CreditNoteNo;
                    ws2.Cells[r2, 2].Value = it.ProductCode;
                    ws2.Cells[r2, 3].Value = it.ProductName;
                    ws2.Cells[r2, 4].Value = it.TotalReturnQty;
                    ws2.Cells[r2, 5].Value = it.Unit;
                    ws2.Cells[r2, 6].Value = it.UnitPrice;
                    ws2.Cells[r2, 6].Style.Numberformat.Format = "#,##0.0000";
                    ws2.Cells[r2, 7].Value = it.AmountBeforeVat;
                    ws2.Cells[r2, 7].Style.Numberformat.Format = "#,##0.00";
                    ws2.Cells[r2, 8].Value = it.VatAmount;
                    ws2.Cells[r2, 8].Style.Numberformat.Format = "#,##0.00";
                    ws2.Cells[r2, 9].Value = it.TotalAmount;
                    ws2.Cells[r2, 9].Style.Numberformat.Format = "#,##0.00";
                    ws2.Cells[r2, 10].Value = it.WarehouseBreakdown;
                    ws2.Cells[r2, 11].Value = it.IsPriceOverridden ? $"แก้ไข (เดิม {it.OriginalNetUnitPrice:N2})" : "ราคาเฉลี่ยสุทธิ";
                    r2++;
                }
            }
            ws2.Cells.AutoFitColumns();

            var bytes = package.GetAsByteArray();
            string fileName = $"CreditNote_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==========================================
        // Helper: ดึง Master สินค้า
        // ==========================================
        private async Task<List<ProductMasterDto>> LoadMasterProductsAsync()
        {
            return await _cache.GetOrCreateAsync("products_master_list", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);
                var list = new List<ProductMasterDto>();
                var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "products_master.csv");
                if (System.IO.File.Exists(csvPath))
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(csvPath);
                    foreach (var l in lines.Skip(1))
                    {
                        var parts = l.Split(',');
                        if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]))
                        {
                            list.Add(new ProductMasterDto
                            {
                                Code = parts[0].Trim(),
                                Name = parts[1].Trim(),
                                Unit = parts.Length > 2 ? parts[2].Trim() : "ชิ้น"
                            });
                        }
                    }
                }
                return list;
            }) ?? new List<ProductMasterDto>();
        }
    }

    // ==========================================
    // DTO Models
    // ==========================================
    public class ProductMasterDto
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "ชิ้น";
    }

    public class RawReturnEntry
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string WarehouseNo { get; set; } = "1";
        public DateTime? ExpiryDate { get; set; }
        public decimal Qty { get; set; }
        public string? LotNo { get; set; }
        public string? Unit { get; set; }
    }

    public class ReturnCalcRequest
    {
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public List<RawReturnEntry>? RawItems { get; set; }
    }

    public class CalculatedReturnItemDto
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "ชิ้น";
        public decimal TotalReturnQty { get; set; }
        public decimal OriginalNetUnitPrice { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsPriceOverridden { get; set; }
        public decimal AmountBeforeVat { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string ReferenceBillNo { get; set; } = "";
        public bool BillNotFound { get; set; }
        public string WarehouseBreakdown { get; set; } = "";
        public List<WarehouseEntryDto> Entries { get; set; } = new();
    }

    public class WarehouseEntryDto
    {
        public string WarehouseNo { get; set; } = "1";
        public DateTime? ExpiryDate { get; set; }
        public decimal Qty { get; set; }
        public string LotNo { get; set; } = "";
    }

    public class OcrTextRequest
    {
        public string? RawText { get; set; }
    }

    public class SaveCreditNoteRequest
    {
        public DateTime? CreditNoteDate { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTaxId { get; set; }
        public string? CustomerBranch { get; set; }
        public string? OriginalBillNo { get; set; }
        public DateTime? OriginalBillDate { get; set; }
        public decimal OriginalBillAmount { get; set; }
        public string? OriginalSalesRep { get; set; }
        public string? SalesRep { get; set; }
        public string? CommissionDeductTarget { get; set; }
        public string? Reason { get; set; }
        public string? PriceOverrideReason { get; set; }
        public bool IsArOffset { get; set; }
        public string? PaperSizePreference { get; set; }
        public string? Remark { get; set; }
        public List<CalculatedReturnItemDto>? Items { get; set; }
    }

    public class SalesRepCommissionSummary
    {
        public string SalesRepName { get; set; } = "";
        public int TotalCreditNotes { get; set; }
        public decimal TotalDeductionBeforeVat { get; set; }
        public decimal TotalDeductionGrand { get; set; }
        public decimal CentralAbsorbedAmount { get; set; }
        public int ProtectedCurrentRepCount { get; set; }
    }
}
