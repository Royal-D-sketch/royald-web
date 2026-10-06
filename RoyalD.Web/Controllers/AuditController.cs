using System.Drawing;
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
    public class AuditController : Controller
    {
        private readonly AppDbContext _db;
        public AuditController(AppDbContext db) => _db = db;

        private async Task<bool> CanAccessAuditAsync()
        {
            if (User.IsInRole("admin") || (User.Identity?.Name?.ToLower() == "admin")) return true;
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return false;
            if (user.Role == "admin") return true;
            var allowed = (user.AllowedPages ?? "").ToLower();
            return allowed.Contains("audit") || allowed.Contains("all");
        }

        private async Task<IQueryable<AuditLog>> BuildQueryAsync(string? search, string? salesRep, DateTime? startDate, DateTime? endDate)
        {
            var q = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim();
                q = q.Where(a => a.Username.Contains(s) || a.Action.Contains(s) || a.Detail.Contains(s) || a.Area.Contains(s) || a.IPAddress.Contains(s));
            }

            if (!string.IsNullOrEmpty(salesRep))
            {
                string rep = salesRep.Trim();
                var allUsers = await _db.Users.AsNoTracking().ToListAsync();
                var matchedUsernames = allUsers
                    .Where(u => u.Username.Equals(rep, StringComparison.OrdinalIgnoreCase)
                             || u.FullName.Equals(rep, StringComparison.OrdinalIgnoreCase)
                             || (!string.IsNullOrEmpty(u.SalesRepCode) && u.SalesRepCode.Equals(rep, StringComparison.OrdinalIgnoreCase)))
                    .Select(u => u.Username)
                    .ToList();

                if (matchedUsernames.Any())
                {
                    q = q.Where(a => matchedUsernames.Contains(a.Username));
                }
                else
                {
                    q = q.Where(a => a.Username.Contains(rep) || a.Detail.Contains(rep));
                }
            }

            if (startDate.HasValue)
            {
                var startUtc = startDate.Value.Date.AddHours(-7);
                q = q.Where(a => a.CreatedAt >= startUtc);
            }

            if (endDate.HasValue)
            {
                var endUtc = endDate.Value.Date.AddDays(1).AddHours(-7);
                q = q.Where(a => a.CreatedAt < endUtc);
            }

            return q;
        }

        public async Task<IActionResult> Index(
            string? search, 
            string? salesRep, 
            DateTime? startDate, 
            DateTime? endDate, 
            int page = 1, 
            int pageSize = 50)
        {
            if (!await CanAccessAuditAsync()) return Forbid();

            var q = await BuildQueryAsync(search, salesRep, startDate, endDate);

            var total = await q.CountAsync();
            int effectivePageSize = pageSize > 0 ? pageSize : 50;
            int totalPages = (int)Math.Ceiling(total / (double)effectivePageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var logs = await q.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * effectivePageSize).Take(effectivePageSize)
                .ToListAsync();

            var users = await _db.Users.AsNoTracking().ToDictionaryAsync(u => u.Username, u => u);
            var allUsersList = await _db.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();

            ViewBag.UserDict = users;
            ViewBag.UsersList = allUsersList;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedSalesRep = salesRep;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = effectivePageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = total;

            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? search, string? salesRep, DateTime? startDate, DateTime? endDate)
        {
            if (!await CanAccessAuditAsync()) return Forbid();

            var q = await BuildQueryAsync(search, salesRep, startDate, endDate);
            var logs = await q.OrderByDescending(a => a.CreatedAt).Take(5000).ToListAsync();

            var users = await _db.Users.AsNoTracking().ToDictionaryAsync(u => u.Username, u => u);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("ประวัติการใช้งาน");

            // Title Header
            ws.Cells["A1:J1"].Merge = true;
            ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด - รายงานประวัติการใช้งานระบบ (Audit Logs)";
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:J2"].Merge = true;
            string dateRangeTxt = (startDate.HasValue ? $"ตั้งแต่วันที่ {startDate:dd/MM/yyyy} " : "") + (endDate.HasValue ? $"ถึงวันที่ {endDate:dd/MM/yyyy}" : "");
            if (string.IsNullOrEmpty(dateRangeTxt)) dateRangeTxt = "ทั้งหมด";
            ws.Cells["A2"].Value = $"ช่วงวันที่: {dateRangeTxt} | ผู้แทน/ผู้ใช้งาน: {(string.IsNullOrEmpty(salesRep) ? "ทั้งหมด" : salesRep)} | พิมพ์ ณ {DateTime.Now:dd/MM/yyyy HH:mm} น.";
            ws.Cells["A2"].Style.Font.Size = 10;
            ws.Cells["A2"].Style.Font.Italic = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            string[] cols = new[] {
                "#",
                "วัน-เวลา (ไทย)",
                "ชื่อ-นามสกุล",
                "ชื่อบัญชี (Username)",
                "ตำแหน่งงาน",
                "กิจกรรม (Action)",
                "เวลาใช้งาน (นาที)",
                "พิกัดและพื้นที่ (Location)",
                "รายละเอียด (Detail)",
                "IP Address"
            };

            var headerBg = Color.FromArgb(15, 118, 110);
            for (int c = 0; c < cols.Length; c++)
            {
                var cell = ws.Cells[4, c + 1];
                cell.Value = cols[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(headerBg);
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int row = 5;
            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                users.TryGetValue(log.Username, out var usr);
                string fullName = usr != null && !string.IsNullOrEmpty(usr.FullName) ? usr.FullName : (log.Username == "admin" ? "ผู้ดูแลระบบ (Admin)" : "-");
                string pos = usr != null && !string.IsNullOrEmpty(usr.Position) ? usr.Position : (log.Username == "admin" ? "ผู้ดูแลระบบ" : "-");
                string loc = GeoLocationHelper.ReverseGeocode(log.Latitude, log.Longitude, log.Area);
                if ((string.IsNullOrEmpty(loc) || loc == "ไม่ระบุตำแหน่ง" || loc == "-") && !string.IsNullOrEmpty(log.Area)) loc = log.Area;

                ws.Cells[row, 1].Value = i + 1;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 2].Value = log.CreatedAtThai.ToString("dd/MM/yyyy HH:mm:ss");
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 3].Value = fullName;
                ws.Cells[row, 4].Value = log.Username;
                ws.Cells[row, 5].Value = pos;
                ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 6].Value = log.Action;
                ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 7].Value = log.DurationMinutes.HasValue ? log.DurationMinutes.Value : "-";
                ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 8].Value = loc;
                ws.Cells[row, 9].Value = log.Detail;
                ws.Cells[row, 10].Value = log.IPAddress;

                for (int c = 1; c <= cols.Length; c++)
                    ws.Cells[row, c].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.LightGray);

                row++;
            }

            ws.Cells.AutoFitColumns();
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 20;
            ws.Column(3).Width = 24;
            ws.Column(8).Width = 32;
            ws.Column(9).Width = 40;

            var fileBytes = package.GetAsByteArray();
            string fileName = $"Audit_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string? search, string? salesRep, DateTime? startDate, DateTime? endDate)
        {
            if (!await CanAccessAuditAsync()) return Forbid();

            var q = await BuildQueryAsync(search, salesRep, startDate, endDate);
            var logs = await q.OrderByDescending(a => a.CreatedAt).Take(2000).ToListAsync();

            var users = await _db.Users.AsNoTracking().ToDictionaryAsync(u => u.Username, u => u);
            ViewBag.UserDict = users;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedSalesRep = salesRep;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.PrintedBy = User.Identity?.Name ?? "Admin";

            return View("PrintPdf", logs);
        }
    }
}
