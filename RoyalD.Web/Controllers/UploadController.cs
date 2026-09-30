using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class UploadController : Controller
    {
        private readonly ExcelImportService _importer;
        private readonly AppDbContext _db;

        public UploadController(ExcelImportService importer, AppDbContext db)
        {
            _importer = importer;
            _db = db;
        }

        private (bool canBills, bool canDebtors, bool canReceipts) GetUploadPermissions()
        {
            if (User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin") || (User.Identity?.Name?.ToLower() == "art"))
            {
                return (true, true, true);
            }

            var username = User.Identity?.Name ?? "";
            var dbUser = _db.Users.AsNoTracking().FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
            var pages = (dbUser?.AllowedPages ?? User.FindFirst("AllowedPages")?.Value ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().ToLower())
                .ToList();

            bool hasGeneralUpload = pages.Contains("upload");
            bool canBills = hasGeneralUpload || pages.Contains("uploadsalesbill");
            bool canDebtors = hasGeneralUpload || pages.Contains("uploaddebtor");
            bool canReceipts = hasGeneralUpload || pages.Contains("uploadreceipt");

            return (canBills, canDebtors, canReceipts);
        }

        private async Task PopulateViewDataAsync()
        {
            var (canBills, canDebtors, canReceipts) = GetUploadPermissions();
            ViewBag.CanUploadBills = canBills;
            ViewBag.CanUploadDebtors = canDebtors;
            ViewBag.CanUploadReceipts = canReceipts;

            ViewBag.LatestBillDate = await _db.SalesBills.OrderByDescending(b => b.BillDate).Select(b => (DateTime?)b.BillDate).FirstOrDefaultAsync();
            ViewBag.LatestDebtorDate = await _db.OutstandingDebts.OrderByDescending(d => d.BillDate).Select(d => (DateTime?)d.BillDate).FirstOrDefaultAsync();
            ViewBag.LatestReceiptDate = await _db.SalesBills.Where(b => b.ReceiptDate != null).OrderByDescending(b => b.ReceiptDate).Select(b => (DateTime?)b.ReceiptDate).FirstOrDefaultAsync()
                                      ?? await _db.OutstandingDebts.Where(d => d.ReceiptDate != null).OrderByDescending(d => d.ReceiptDate).Select(d => (DateTime?)d.ReceiptDate).FirstOrDefaultAsync();

            ViewBag.TotalBillsCount = await _db.SalesBills.CountAsync();
            ViewBag.TotalDebtorsCount = await _db.OutstandingDebts.CountAsync();
            ViewBag.TotalReceiptsCount = await _db.SalesBills.CountAsync(b => b.ReceiptNo != null && b.ReceiptNo != "");

            // Top preview rows from current database if not in TempData
            if (ViewBag.LatestBillDate != null)
            {
                ViewBag.DbBillPreview = await _db.SalesBills.OrderByDescending(b => b.BillDate).Take(10).ToListAsync();
            }
            if (ViewBag.LatestDebtorDate != null)
            {
                ViewBag.DbDebtorPreview = await _db.OutstandingDebts.OrderByDescending(d => d.BillDate).Take(10).ToListAsync();
            }
            if (ViewBag.LatestReceiptDate != null)
            {
                ViewBag.DbReceiptPreview = await _db.SalesBills.Where(b => b.ReceiptNo != null && b.ReceiptNo != "").OrderByDescending(b => b.ReceiptDate).Take(10).ToListAsync();
            }
        }

        public async Task<IActionResult> Index()
        {
            var (canBills, canDebtors, canReceipts) = GetUploadPermissions();
            if (!canBills && !canDebtors && !canReceipts)
            {
                TempData["Error"] = "ท่านไม่มีสิทธิ์เข้าใช้งานระบบอัปโหลดข้อมูล";
                return RedirectToAction("Index", "Home");
            }

            await PopulateViewDataAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ClearData(string target = "all")
        {
            try
            {
                string msg = "";
                if (target == "bills" || target == "all")
                {
                    _db.SalesBillItems.RemoveRange(_db.SalesBillItems);
                    _db.SalesBills.RemoveRange(_db.SalesBills);
                    msg += "บิลขาย ";
                }
                if (target == "debtors" || target == "all")
                {
                    _db.PaymentRecords.RemoveRange(_db.PaymentRecords);
                    _db.OutstandingDebts.RemoveRange(_db.OutstandingDebts);
                    msg += "การ์ดลูกหนี้ ";
                }
                if (target == "receipts" || target == "all")
                {
                    var bills = await _db.SalesBills.Where(b => b.ReceiptNo != null && b.ReceiptNo != "").ToListAsync();
                    foreach (var b in bills)
                    {
                        b.ReceiptNo = "";
                        b.ReceiptDate = null;
                        b.IsFullyPaid = false;
                    }
                    var debts = await _db.OutstandingDebts.Where(d => d.ReceiptNo != null && d.ReceiptNo != "").ToListAsync();
                    foreach (var d in debts)
                    {
                        d.ReceiptNo = "";
                        d.ReceiptDate = null;
                        d.Status = DebtStatus.Outstanding;
                        d.RemainingAmount = d.OriginalAmount;
                    }
                    _db.PaymentRecords.RemoveRange(_db.PaymentRecords);
                    msg += "ใบเสร็จรับเงิน ";
                }

                await _db.SaveChangesAsync();

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "CLEAR_DATA",
                    Detail = $"Cleared: {msg}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();

                TempData["Success"] = $"ล้างข้อมูล ({msg.Trim()}) เก่าในระบบเรียบร้อยแล้ว พร้อมรับการนำเข้าข้อมูลชุดใหม่";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เกิดข้อผิดพลาดในการล้างข้อมูล: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string fileType, bool isCurrentMonth)
        {
            var (canBills, canDebtors, _) = GetUploadPermissions();
            if (fileType == "outstanding")
            {
                if (!canDebtors)
                {
                    TempData["Error"] = "ท่านไม่มีสิทธิ์อัปโหลดการ์ดลูกหนี้ค้างชำระ";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                if (!canBills)
                {
                    TempData["Error"] = "ท่านไม่มีสิทธิ์อัปโหลดบิลขาย";
                    return RedirectToAction("Index");
                }
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "กรุณาเลือกไฟล์";
                return RedirectToAction("Index");
            }

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xls" && ext != ".xlsx" && ext != ".csv")
            {
                TempData["Error"] = "รองรับเฉพาะไฟล์ .xls, .xlsx และ .csv เท่านั้น";
                return RedirectToAction("Index");
            }

            try
            {
                using var stream = file.OpenReadStream();

                if (fileType == "outstanding")
                {
                    // 100% Overwrite without duplicate alerts
                    var (count, latestDate, previewRows) = await _importer.ImportOutstandingDebtsAsync(stream, file.FileName);
                    string dateStr = latestDate != DateTime.MinValue ? latestDate.ToString("dd/MM/yyyy") : "-";
                    TempData["Success"] = $"ระบบได้อัปเดตข้อมูลลูกหนี้ค้างชำระล่าสุด ณ วันที่: {dateStr} เรียบร้อยแล้ว (นำเข้าสำเร็จ {count} รายการ)";

                    _db.AuditLogs.Add(new AuditLog
                    {
                        Username = User.Identity?.Name ?? "",
                        Action = "UPLOAD_OUTSTANDING_DEBTS",
                        Detail = $"File={file.FileName}, Count={count}, LatestDate={dateStr}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                        CreatedAt = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                    ReportPrewarmBackgroundService.SignalRefresh();
                    return RedirectToAction("Index");
                }
                else
                {
                    // Daily Sales Bills -> Preview & Check Duplicates
                    var month = fileType;
                    var preview = await _importer.PreviewSalesBillAsync(stream, month, isCurrentMonth, file.FileName);
                    if (preview.DuplicateChangedCount > 0)
                    {
                        return View("Preview", preview);
                    }
                    else
                    {
                        var (ins, upd, _, maxDate) = await _importer.ConfirmImportSalesBillAsync(preview.PreviewId, updateDuplicates: true);
                        string dateStr = maxDate != DateTime.MinValue ? maxDate.ToString("dd/MM/yyyy") : "-";
                        TempData["Success"] = $"นำเข้าบิลขายสำเร็จ {ins + upd} บิล | ข้อมูลบิลขายล่าสุดในระบบ ณ วันที่: {dateStr}";
                        ReportPrewarmBackgroundService.SignalRefresh();
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เกิดข้อผิดพลาด: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmImport(string previewId, bool updateDuplicates = true, bool skipDuplicates = false)
        {
            var (canBills, _, _) = GetUploadPermissions();
            if (!canBills)
            {
                TempData["Error"] = "ท่านไม่มีสิทธิ์อัปโหลดบิลขาย";
                return RedirectToAction("Index");
            }

            try
            {
                var preview = ExcelImportService.GetPreview(previewId);
                if (preview == null)
                {
                    TempData["Error"] = "ไม่พบข้อมูลพรีวิวที่รอยืนยัน หรือเซสชันหมดอายุ กรุณาอัปโหลดไฟล์ใหม่อีกครั้ง";
                    return RedirectToAction("Index");
                }

                var (ins, upd, skip, maxDate) = await _importer.ConfirmImportSalesBillAsync(previewId, updateDuplicates, skipDuplicates);
                string dateStr = maxDate != DateTime.MinValue ? maxDate.ToString("dd/MM/yyyy") : "-";
                TempData["Success"] = $"ยืนยันนำเข้าข้อมูลสำเร็จ: เพิ่มบิลใหม่ {ins} บิล, อัปเดตบิลซ้ำ {upd} บิล, ข้าม {skip} บิล | ข้อมูลบิลขายล่าสุดในระบบ ณ วันที่: {dateStr}";

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "CONFIRM_IMPORT_SALESBILL",
                    Detail = $"File={preview.FileName}, Inserted={ins}, Updated={upd}, Skipped={skip}, LatestDate={dateStr}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
                ReportPrewarmBackgroundService.SignalRefresh();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เกิดข้อผิดพลาดในการบันทึกข้อมูล: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CancelPreview(string previewId)
        {
            ExcelImportService.RemovePreview(previewId);
            TempData["Info"] = "ยกเลิกการนำเข้าไฟล์เรียบร้อยแล้ว";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        public async Task<IActionResult> UploadSalesBills(List<IFormFile> files)
        {
            var (canBills, _, _) = GetUploadPermissions();
            if (!canBills)
            {
                TempData["Error"] = "ท่านไม่มีสิทธิ์อัปโหลดบิลขาย";
                return RedirectToAction("Index");
            }

            if (files == null || files.Count == 0)
            {
                TempData["Error"] = "กรุณาเลือกไฟล์";
                return RedirectToAction("Index");
            }

            int totalInserted = 0;
            int totalUpdated = 0;
            int processedFiles = 0;
            DateTime overallMaxDate = DateTime.MinValue;

            try
            {
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (ext != ".xls" && ext != ".xlsx" && ext != ".csv") continue;

                    using var stream = file.OpenReadStream();
                    var (ins, upd, maxDate, _) = await _importer.ImportSalesBillAsync(stream, "Direct", true, file.FileName);
                    totalInserted += ins;
                    totalUpdated += upd;
                    if (maxDate > overallMaxDate) overallMaxDate = maxDate;
                    processedFiles++;
                }

                string dateStr = overallMaxDate != DateTime.MinValue ? overallMaxDate.ToString("dd/MM/yyyy") : "-";
                TempData["Success"] = $"นำเข้าบิลขายสำเร็จ {totalInserted + totalUpdated} บิล จาก {processedFiles} ไฟล์ | ข้อมูลบิลขายล่าสุดในระบบ ณ วันที่: {dateStr}";

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "UPLOAD_SALES_BILLS_MULTIPLE",
                    Detail = $"Files={processedFiles}, Inserted={totalInserted}, Updated={totalUpdated}, LatestDate={dateStr}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เกิดข้อผิดพลาด: {ex.InnerException?.Message ?? ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadReceipt(IFormFile file)
        {
            var (_, _, canReceipts) = GetUploadPermissions();
            if (!canReceipts)
            {
                TempData["Error"] = "ท่านไม่มีสิทธิ์อัปโหลดสรุปรับเงินตามใบเสร็จ";
                return RedirectToAction("Index");
            }
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "กรุณาเลือกไฟล์";
                return RedirectToAction("Index");
            }

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xls" && ext != ".xlsx" && ext != ".csv")
            {
                TempData["Error"] = "รองรับเฉพาะไฟล์ .xls, .xlsx และ .csv เท่านั้น";
                return RedirectToAction("Index");
            }

            try
            {
                using var stream = file.OpenReadStream();
                var preview = await _importer.PreviewReceiptMatchAsync(stream, file.FileName);

                if (!preview.Duplicates.Any())
                {
                    var (matched, notFound, maxDate) = await _importer.ConfirmReceiptMatchAsync(preview.PreviewId, updateDuplicates: false);
                    string dateStr = maxDate != DateTime.MinValue ? maxDate.ToString("dd/MM/yyyy") : "-";
                    TempData["Success"] = $"จับคู่ใบเสร็จสำเร็จ {matched} รายการ (ไม่พบในระบบ: {notFound}) | ข้อมูลใบเสร็จรับเงินล่าสุดในระบบ ณ วันที่: {dateStr}";
                    _db.AuditLogs.Add(new AuditLog
                    {
                        Username = User.Identity?.Name ?? "",
                        Action = "UPLOAD_RECEIPTS",
                        Detail = $"File={file.FileName}, Matched={matched}, NotFound={notFound}, LatestDate={dateStr}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                        CreatedAt = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

                return View("ReceiptPreview", preview);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เกิดข้อผิดพลาด: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceiptImport(string previewId, List<string>? selectedDuplicates)
        {
            var (_, _, canReceipts) = GetUploadPermissions();
            if (!canReceipts)
            {
                TempData["Error"] = "ท่านไม่มีสิทธิ์อัปโหลดสรุปรับเงินตามใบเสร็จ";
                return RedirectToAction("Index");
            }

            var preview = ExcelImportService.GetReceiptPreview(previewId);
            if (preview == null)
            {
                TempData["Error"] = "ไม่พบข้อมูลพรีวิวที่รอยืนยัน หรือเซสชันหมดอายุ กรุณาอัปโหลดไฟล์ใหม่";
                return RedirectToAction("Index");
            }

            try
            {
                bool updateDups = selectedDuplicates != null && selectedDuplicates.Count > 0;
                var (matched, notFound, maxDate) = await _importer.ConfirmReceiptMatchAsync(previewId, updateDups, selectedDuplicates);
                int skippedDups = preview.Duplicates.Count - (selectedDuplicates?.Count ?? 0);
                string dateStr = maxDate != DateTime.MinValue ? maxDate.ToString("dd/MM/yyyy") : "-";
                TempData["Success"] = $"จับคู่ใบเสร็จสำเร็จ {matched} รายการ, ข้ามซ้ำ {skippedDups} รายการ (ไม่พบในระบบ: {notFound}) | ข้อมูลใบเสร็จรับเงินล่าสุดในระบบ ณ วันที่: {dateStr}";

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "CONFIRM_RECEIPT_IMPORT",
                    Detail = $"File={preview.FileName}, Matched={matched}, NotFound={notFound}, Skipped={skippedDups}, LatestDate={dateStr}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เกิดข้อผิดพลาด: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CancelReceiptPreview(string previewId)
        {
            ExcelImportService.RemoveReceiptPreview(previewId);
            TempData["Info"] = "ยกเลิกการนำเข้าใบเสร็จเรียบร้อยแล้ว";
            return RedirectToAction("Index");
        }
    }
}
