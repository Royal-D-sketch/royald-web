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
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ReportService _svc;

        public ReportController(ReportService svc) => _svc = svc;

        public async Task<IActionResult> Sales([FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            var data = await cache.GetOrCreateAsync("annual_performance_report_svc", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _svc.GetAnnualPerformanceAsync();
            });
            return View(data);
        }

        public async Task<IActionResult> ExportExcel(DateTime? from, DateTime? to)
        {
            var pos = User.FindFirst("Position")?.Value ?? "";
            bool isSalesRep = pos == "ผู้แทนขาย" || pos == "พนักงานขาย" || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย");
            if (isSalesRep || (!User.IsInRole("admin") && User.FindFirst("CanDownload")?.Value != "true")) return Forbid();

            var data = await _svc.GetSalesSummaryAsync(from, to);
            var bytes = await _svc.ExportToExcelAsync(data, from, to);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> WaitingGoods([FromServices] AppDbContext db, [FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache, string? search, string? salesRep)
        {
            var qPending = db.PendingProducts
                .Include(p => p.OutstandingDebt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                qPending = qPending.Where(p => p.OutstandingDebt.CustomerName.Contains(search) || p.OutstandingDebt.BillNo.Contains(search) || p.OutstandingDebt.CustomerCode.Contains(search));
            }
            if (!string.IsNullOrEmpty(salesRep))
            {
                qPending = qPending.Where(p => p.OutstandingDebt.SalesRep == salesRep);
            }

            var data1 = await qPending.Select(p => new {
                BillNo = p.OutstandingDebt.BillNo,
                BillDate = p.OutstandingDebt.BillDate,
                CustomerCode = p.OutstandingDebt.CustomerCode,
                CustomerName = p.OutstandingDebt.CustomerName,
                SalesRep = p.OutstandingDebt.SalesRep,
                UpdatedAt = p.OutstandingDebt.WaitingGoodsDate ?? p.OutstandingDebt.BillDate,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Quantity = p.Quantity,
                Note = p.OutstandingDebt.Note
            }).ToListAsync();

            var qDebt = db.OutstandingDebts
                .Include(d => d.PendingProducts)
                .Where(d => d.Status == DebtStatus.WaitingGoods && !d.PendingProducts.Any())
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                qDebt = qDebt.Where(d => d.CustomerName.Contains(search) || d.BillNo.Contains(search) || d.CustomerCode.Contains(search));
            }
            if (!string.IsNullOrEmpty(salesRep))
            {
                qDebt = qDebt.Where(d => d.SalesRep == salesRep);
            }

            var data2 = await qDebt.Select(d => new {
                BillNo = d.BillNo,
                BillDate = d.BillDate,
                CustomerCode = d.CustomerCode,
                CustomerName = d.CustomerName,
                SalesRep = d.SalesRep,
                UpdatedAt = d.WaitingGoodsDate ?? d.BillDate,
                ProductCode = "-",
                ProductName = "หลายรายการ / ไม่ได้ระบุรหัสสินค้า",
                Quantity = 0,
                Note = d.Note
            }).ToListAsync();

            var data = data1.Concat(data2).OrderByDescending(x => x.UpdatedAt).ToList();

            var reps = await cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await db.OutstandingDebts.AsNoTracking().Where(d => d.SalesRep != null && d.SalesRep != "").Select(d => d.SalesRep).Distinct().OrderBy(x => x).ToListAsync();
            }) ?? new List<string>();
            ViewBag.SalesReps = reps;
            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;

            return View(data);
        }
        
        public async Task<IActionResult> PaidHistory([FromServices] AppDbContext db, string? search)
        {
            var dateLimit = DateTime.Now.AddDays(-120);
            var q = db.SalesBills.AsNoTracking().Where(b => b.IsFullyPaid && b.ReceiptDate >= dateLimit);
            
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(b => b.BillNo.Contains(search) 
                              || b.CustomerName.Contains(search) 
                              || b.CustomerCode.Contains(search) 
                              || (b.ReceiptNo != null && b.ReceiptNo.Contains(search)));
            }
            
            var data = await q.OrderByDescending(b => b.ReceiptDate).ToListAsync();
            ViewBag.SearchTerm = search;
            return View(data);
        }

        public async Task<IActionResult> CancelledBills([FromServices] AppDbContext db)
        {
            var dateLimit = DateTime.Now.AddDays(-30);
            var data = await db.OutstandingDebts.AsNoTracking()
                .Where(d => d.Status == DebtStatus.Cancelled && d.CancelledDate >= dateLimit)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> ReturnNotes([FromServices] AppDbContext db, [FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache, string? search, string? salesRep)
        {
            var q = db.OutstandingDebts.AsNoTracking()
                .Include(d => d.Attachments)
                .Where(d => d.Status == DebtStatus.ReturnIssued || d.Status == DebtStatus.ReturnPending)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(d => d.CustomerName.Contains(search) || d.BillNo.Contains(search) || d.CustomerCode.Contains(search));
            }
            if (!string.IsNullOrEmpty(salesRep))
            {
                q = q.Where(d => d.SalesRep == salesRep);
            }

            var data = await q.OrderByDescending(d => d.BillDate).ToListAsync();
            
            var reps = await cache.GetOrCreateAsync("all_debtor_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await db.OutstandingDebts.AsNoTracking().Where(d => d.SalesRep != null && d.SalesRep != "").Select(d => d.SalesRep).Distinct().OrderBy(x => x).ToListAsync();
            }) ?? new List<string>();
            ViewBag.SalesReps = reps;
            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;

            return View(data);
        }

        public async Task<IActionResult> InstallmentDebtors([FromServices] AppDbContext db)
        {
            var data = await db.OutstandingDebts
                                .AsNoTracking()
                                .Where(d => (int)d.Status == 100)
                                .OrderBy(d => d.DueDate)
                                .ToListAsync();
            return View(data);
        }

        // ==============================================================
        // ==============================================================
        // HELPER: ตรวจสอบสิทธิ์ผู้ใช้ที่มีสิทธิ์กดส่งคืนไปจัดส่ง
        // ตำแหน่ง/บทบาท: admin, ผู้ดูแลระบบ, หัวหน้า, ผู้บริหาร
        // รหัสผู้ใช้ / ชื่อ: nid, art, admin, นิด, อาร์ต หรือมีสิทธิ์ CanChangeDebtStatus
        // ==============================================================
        public static bool IsAuthorizedUserForReturn(System.Security.Claims.ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;

            // 0. ตรวจสอบสิทธิ์โดยตรงจาก Claim / สิทธิ์ผู้ใช้งาน CanManageReturnedBills
            if (user.FindFirst("CanManageReturnedBills")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // 1. ตรวจสอบสิทธิ์ระดับ Role / Identity Role (admin)
            if (user.IsInRole("admin")) return true;
            var role = (user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? user.FindFirst("Role")?.Value ?? "").Trim().ToLower();
            if (role == "admin") return true;

            // 2. ตรวจสอบจาก ตำแหน่ง (Position) เช่น admin, ผู้ดูแลระบบ, หัวหน้างาน, หัวหน้า, ผู้บริหาร
            var position = (user.FindFirst("Position")?.Value ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(position))
            {
                if (position.Contains("admin") || position.Contains("ผู้ดูแลระบบ") || position.Contains("หัวหน้า") || position.Contains("ผู้บริหาร"))
                {
                    return true;
                }
            }

            // 3. ตรวจสอบสิทธิ์เปลี่ยนสถานะหนี้ (CanChangeDebtStatus)
            if (user.FindFirst("CanChangeDebtStatus")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // 4. ตรวจสอบรหัสผู้ใช้ (Username) หรือ ชื่อ-นามสกุล (FullName) เช่น ART, nid, admin, นิด, อาร์ต, ปภาวดี, อินจันทร์
            var uName = (user.Identity.Name ?? "").Trim().ToLower();
            var fName = (user.FindFirst("FullName")?.Value ?? "").Trim().ToLower();

            var allowedKeywords = new[] { "nid", "art", "admin", "หัวหน้า", "ผู้บริหาร", "นิด", "อาร์ต", "ปภาวดี", "อินจันทร์" };
            return allowedKeywords.Any(a =>
                uName.Equals(a, StringComparison.OrdinalIgnoreCase) ||
                uName.Contains(a) ||
                fName.Equals(a, StringComparison.OrdinalIgnoreCase) ||
                fName.Contains(a)
            );
        }

        // ==============================================================
        // 1. รายงานบิลส่งคืนกลับมาบัญชี (บิลไม่พร้อมส่ง/ลูกค้ายังไม่เอาของ)
        // ==============================================================
        public async Task<IActionResult> ReturnedToAccount([FromServices] AppDbContext db, [FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache, string? search, string? salesRep, DateTime? fromDate, DateTime? toDate)
        {
            // ดึงบิลที่มีประวัติส่งคืนกลับมาบัญชี และยังไม่จบดีล (คงค้างไว้จนกว่าจะจ่ายครบ 0.00 บาท)
            var q = db.OutstandingDebts
                .AsNoTracking()
                .Where(d => d.Status != DebtStatus.Cancelled &&
                            (d.Status == DebtStatus.ReturnedToAccount || d.ReturnedToDeliveryDate != null || d.ReturnedToAccountDate != null) &&
                            !(d.RemainingAmount <= 0 && (d.FullyPaidDate != null || d.Status == DebtStatus.PaidCash || d.Status == DebtStatus.PaidTransfer || d.Status == DebtStatus.PaidCheck || d.ReceiptDate != null)))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(d => d.BillNo.Contains(s) || d.CustomerName.Contains(s) || d.CustomerCode.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(salesRep))
            {
                q = q.Where(d => d.SalesRep == salesRep);
            }

            if (fromDate.HasValue)
            {
                q = q.Where(d => (d.ReturnedToAccountDate ?? d.BillDate) >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                q = q.Where(d => (d.ReturnedToAccountDate ?? d.BillDate) <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            var data = await q.OrderByDescending(d => d.ReturnedToAccountDate ?? d.BillDate).ToListAsync();

            var reps = await cache.GetOrCreateAsync("all_returned_account_reps", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await db.OutstandingDebts.AsNoTracking()
                    .Where(d => (d.Status == DebtStatus.ReturnedToAccount || d.ReturnedToDeliveryDate != null || d.ReturnedToAccountDate != null) && d.SalesRep != null && d.SalesRep != "")
                    .Select(d => d.SalesRep)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();
            }) ?? new List<string>();

            ViewBag.SalesReps = reps;
            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.IsAuthorized = IsAuthorizedUserForReturn(User);

            return View(data);
        }

        // ==============================================================
        // 2. ฟังก์ชัน "ส่งคืนไปจัดส่ง" (คืนสถานะเป็นลูกหนี้ค้างชำระปกติ & ซิงค์ยอดกลับ AR Card)
        // ==============================================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToDelivery([FromServices] AppDbContext db, int debtId, DateTime? deliveryDate, string? returnNote)
        {
            // ตรวจสอบสิทธิ์ผู้ใช้งานเฉพาะ: ['nid', 'admin', 'หัวหน้า', 'ผู้บริหาร']
            if (!IsAuthorizedUserForReturn(User))
            {
                TempData["Error"] = "ข้อผิดพลาด: บัญชีผู้ใช้ของคุณไม่มีสิทธิ์ในการดำเนินการส่งคืนบิลไปจัดส่ง กรุณาติดต่อหัวหน้าแผนกบัญชี";
                return RedirectToAction("ReturnedToAccount");
            }

            var debt = await db.OutstandingDebts.FirstOrDefaultAsync(d => d.Id == debtId);
            if (debt == null) return NotFound();

            var dDate = deliveryDate ?? DateTime.Today;
            debt.Status = DebtStatus.Outstanding; // คืนสถานะเป็นลูกหนี้ปกติ เพื่อซิงค์กลับไปยังหน้าการ์ดลูกหนี้ (AR Card)
            debt.DeliveringDate = dDate;
            debt.ReturnedToDeliveryDate = dDate;
            
            string noteDetail = !string.IsNullOrWhiteSpace(returnNote) ? $" (หมายเหตุ: {returnNote.Trim()})" : "";
            debt.Note = $"ส่งคืนไปจัดส่งเมื่อ {dDate:dd/MM/yyyy}{noteDetail}";
            debt.LastEditedDate = DateTime.Now;
            debt.LastEditedBy = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";


            await db.SaveChangesAsync();

            TempData["Success"] = $"ส่งคืนบิลเลขที่ {debt.BillNo} ไปจัดส่งเรียบร้อยแล้ว (สถานะกลับเป็นลูกหนี้ค้างชำระปกติและคงประวัติในตาราง)";
            return RedirectToAction("ReturnedToAccount");
        }

        // ==============================================================
        // 3. EXPORT EXCEL: บิลส่งคืนกลับมาบัญชี (พร้อมคอลัมน์ วันที่ส่งคืนบิลไปจัดส่ง)
        // ==============================================================
        public async Task<IActionResult> ExportReturnedToAccountExcel([FromServices] AppDbContext db, string? search, string? salesRep, DateTime? fromDate, DateTime? toDate)
        {
            var q = db.OutstandingDebts
                .AsNoTracking()
                .Where(d => d.Status != DebtStatus.Cancelled &&
                            (d.Status == DebtStatus.ReturnedToAccount || d.ReturnedToDeliveryDate != null || d.ReturnedToAccountDate != null) &&
                            !(d.RemainingAmount <= 0 && (d.FullyPaidDate != null || d.Status == DebtStatus.PaidCash || d.Status == DebtStatus.PaidTransfer || d.Status == DebtStatus.PaidCheck || d.ReceiptDate != null)))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(d => d.BillNo.Contains(s) || d.CustomerName.Contains(s) || d.CustomerCode.Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(salesRep))
            {
                q = q.Where(d => d.SalesRep == salesRep);
            }
            if (fromDate.HasValue)
            {
                q = q.Where(d => (d.ReturnedToAccountDate ?? d.BillDate) >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                q = q.Where(d => (d.ReturnedToAccountDate ?? d.BillDate) <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            var list = await q.OrderByDescending(d => d.ReturnedToAccountDate ?? d.BillDate).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("ReturnedToAccount");

            ws.Cells["A1:L1"].Merge = true;
            ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:L2"].Merge = true;
            ws.Cells["A2"].Value = $"รายงานบิลส่งคืนกลับมาบัญชี (บิลไม่พร้อมส่ง/ลูกค้ายังไม่เอาของ) — พิมพ์ ณ วันที่ {DateTime.Now:dd/MM/yyyy HH:mm} น.";
            ws.Cells["A2"].Style.Font.Size = 12;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            string[] headers = new[] { "#", "เลขที่บิล", "วันที่บิล", "รหัสลูกค้า", "ชื่อลูกค้า", "อำเภอ", "จังหวัด", "ผู้แทนขาย", "วันที่รับบิลจากจัดส่ง", "เหตุผลที่ส่งคืนบัญชี", "วันที่ส่งคืนบิลไปจัดส่ง", "สถานะบิล", "จำนวนเงินคงค้าง (บาท)" };
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cells[4, i + 1];
                c.Value = headers[i];
                c.Style.Font.Bold = true;
                c.Style.Fill.PatternType = ExcelFillStyle.Solid;
                c.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 119, 6)); // Amber / Warning
                c.Style.Font.Color.SetColor(Color.White);
                c.Style.HorizontalAlignment = (i == 0 || i == 1 || i == 2 || i == 3 || i == 8 || i == 10 || i == 11) ? ExcelHorizontalAlignment.Center : (i == 12 ? ExcelHorizontalAlignment.Right : ExcelHorizontalAlignment.Left);
                c.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int rowIdx = 5;
            for (int idx = 0; idx < list.Count; idx++)
            {
                var d = list[idx];
                ws.Cells[rowIdx, 1].Value = idx + 1;
                ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[rowIdx, 2].Value = d.BillNo;
                ws.Cells[rowIdx, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[rowIdx, 3].Value = d.BillDate.ToString("dd/MM/yyyy");
                ws.Cells[rowIdx, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[rowIdx, 4].Value = d.CustomerCode;
                ws.Cells[rowIdx, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[rowIdx, 5].Value = d.CustomerName;
                ws.Cells[rowIdx, 6].Value = d.District;
                ws.Cells[rowIdx, 7].Value = d.Province;
                ws.Cells[rowIdx, 8].Value = d.SalesRep;

                ws.Cells[rowIdx, 9].Value = d.ReturnedToAccountDate?.ToString("dd/MM/yyyy") ?? "-";
                ws.Cells[rowIdx, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[rowIdx, 10].Value = d.ReturnedToAccountReason ?? d.Note ?? "";

                ws.Cells[rowIdx, 11].Value = d.ReturnedToDeliveryDate?.ToString("dd/MM/yyyy") ?? "-";
                ws.Cells[rowIdx, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[rowIdx, 12].Value = d.ReturnedToDeliveryDate != null ? "บิลอยู่จัดส่ง" : "ส่งคืนกลับมาบัญชี";
                ws.Cells[rowIdx, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[rowIdx, 13].Value = d.RemainingAmount > 0 ? d.RemainingAmount : d.OriginalAmount;
                ws.Cells[rowIdx, 13].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[rowIdx, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                for (int col = 1; col <= 13; col++)
                    ws.Cells[rowIdx, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.LightGray);

                rowIdx++;
            }

            // Total Row
            ws.Cells[rowIdx, 1, rowIdx, 12].Merge = true;
            ws.Cells[rowIdx, 1].Value = $"ยอดรวมทั้งสิ้น ({list.Count:N0} รายการ):";
            ws.Cells[rowIdx, 1].Style.Font.Bold = true;
            ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[rowIdx, 13].Value = list.Sum(x => x.RemainingAmount > 0 ? x.RemainingAmount : x.OriginalAmount);
            ws.Cells[rowIdx, 13].Style.Font.Bold = true;
            ws.Cells[rowIdx, 13].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[rowIdx, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            for (int col = 1; col <= 13; col++)
            {
                ws.Cells[rowIdx, col].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws.Cells[rowIdx, col].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(254, 243, 199)); // Amber 100
            }

            ws.Cells.AutoFitColumns();
            ws.Column(1).Width = 5;
            ws.Column(5).Width = Math.Max(ws.Column(5).Width, 35);
            ws.Column(10).Width = 25;
            ws.Column(13).Width = 18;

            var fileBytes = package.GetAsByteArray();
            string fileName = $"Returned_To_Account_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==============================================================
        // 4. EXPORT PDF: บิลส่งคืนกลับมาบัญชี (พร้อมคอลัมน์ วันที่ส่งคืนบิลไปจัดส่ง)
        // ==============================================================
        public async Task<IActionResult> ExportReturnedToAccountPdf([FromServices] AppDbContext db, string? search, string? salesRep, DateTime? fromDate, DateTime? toDate)
        {
            var q = db.OutstandingDebts
                .AsNoTracking()
                .Where(d => d.Status != DebtStatus.Cancelled &&
                            (d.Status == DebtStatus.ReturnedToAccount || d.ReturnedToDeliveryDate != null || d.ReturnedToAccountDate != null) &&
                            !(d.RemainingAmount <= 0 && (d.FullyPaidDate != null || d.Status == DebtStatus.PaidCash || d.Status == DebtStatus.PaidTransfer || d.Status == DebtStatus.PaidCheck || d.ReceiptDate != null)))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(d => d.BillNo.Contains(s) || d.CustomerName.Contains(s) || d.CustomerCode.Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(salesRep))
            {
                q = q.Where(d => d.SalesRep == salesRep);
            }
            if (fromDate.HasValue)
            {
                q = q.Where(d => (d.ReturnedToAccountDate ?? d.BillDate) >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                q = q.Where(d => (d.ReturnedToAccountDate ?? d.BillDate) <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            var list = await q.OrderByDescending(d => d.ReturnedToAccountDate ?? d.BillDate).ToListAsync();

            ViewBag.Search = search;
            ViewBag.SalesRep = salesRep;
            ViewBag.PrintedBy = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";

            return View("PrintReturnedToAccountPdf", list);
        }

    }
}
