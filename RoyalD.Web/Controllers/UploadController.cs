using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class UploadController : Controller
    {
        private readonly ExcelImportService _importer;
        private readonly AppDbContext _db;

        public UploadController(ExcelImportService importer, AppDbContext db)
        {
            _importer = importer;
            _db = db;
        }

        public IActionResult Index() => View();

        [AllowAnonymous]
        public async Task<IActionResult> ForceUploadSalesBills()
        {
            try
            {
                var path = @"C:\Users\User2\Desktop\อาร์ต\รายละเอียดการขาย วันที่ 1-28.8.69.XLS";
                using var stream = System.IO.File.OpenRead(path);
                var (ins, upd) = await _importer.ImportSalesBillAsync(stream, "Direct", true);
                return Content($"Success! Inserted {ins}, Updated {upd}");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string fileType, bool isCurrentMonth)
        {
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
                    var count = await _importer.ImportOutstandingDebtsAsync(stream, file.FileName);
                    TempData["Success"] = $"นำเข้าลูกหนี้คงค้างสำเร็จ {count} รายการ";

                    _db.AuditLogs.Add(new AuditLog
                    {
                        Username = User.Identity?.Name ?? "",
                        Action = "UPLOAD_OUTSTANDING_DEBTS",
                        Detail = $"File={file.FileName}, Count={count}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                        CreatedAt = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                else
                {
                    // Daily Sales Bills -> Preview & Check Duplicates first!
                    var month = fileType; // e.g. "2026-08"
                    var preview = await _importer.PreviewSalesBillAsync(stream, month, isCurrentMonth, file.FileName);
                    return View("Preview", preview);
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
            try
            {
                var preview = ExcelImportService.GetPreview(previewId);
                if (preview == null)
                {
                    TempData["Error"] = "ไม่พบข้อมูลพรีวิวที่รอยืนยัน หรือเซสชันหมดอายุ กรุณาอัปโหลดไฟล์ใหม่อีกครั้ง";
                    return RedirectToAction("Index");
                }

                var (ins, upd, skip) = await _importer.ConfirmImportSalesBillAsync(previewId, updateDuplicates, skipDuplicates);
                TempData["Success"] = $"ยืนยันนำเข้าข้อมูลสำเร็จ: เพิ่มบิลใหม่ {ins} บิล, อัปเดตบิลซ้ำ {upd} บิล, ข้าม {skip} บิล";

                // Log Audit
                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "CONFIRM_IMPORT_SALESBILL",
                    Detail = $"File={preview.FileName}, Month={preview.FileType}, Inserted={ins}, Updated={upd}, Skipped={skip}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
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
            if (files == null || files.Count == 0)
            {
                TempData["Error"] = "กรุณาเลือกไฟล์";
                return RedirectToAction("Index");
            }
            if (files.Count > 8)
            {
                TempData["Error"] = "เลือกได้สูงสุด 8 ไฟล์";
                return RedirectToAction("Index");
            }

            int totalInserted = 0;
            int totalUpdated = 0;
            int processedFiles = 0;

            try
            {
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (ext != ".xls" && ext != ".xlsx" && ext != ".csv") continue;

                    using var stream = file.OpenReadStream();
                    var (ins, upd) = await _importer.ImportSalesBillAsync(stream, "Direct", true, file.FileName);
                    totalInserted += ins;
                    totalUpdated += upd;
                    processedFiles++;
                }

                TempData["Success"] = $"นำเข้าบิลขายสำเร็จ {totalInserted + totalUpdated} บิล จาก {processedFiles} ไฟล์";
                
                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "UPLOAD_SALES_BILLS_MULTIPLE",
                    Detail = $"Files={processedFiles}, Inserted={totalInserted}, Updated={totalUpdated}",
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
                var (matched, notFound) = await _importer.ImportReceiptMatchAsync(stream, file.FileName);
                
                TempData["Success"] = $"จับคู่ใบเสร็จสำเร็จ {matched} รายการ (ไม่พบ: {notFound})";

                _db.AuditLogs.Add(new AuditLog
                {
                    Username = User.Identity?.Name ?? "",
                    Action = "UPLOAD_RECEIPTS",
                    Detail = $"File={file.FileName}, Matched={matched}, NotFound={notFound}",
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
    }
}