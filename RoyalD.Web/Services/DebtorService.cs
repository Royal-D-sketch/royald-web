using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RoyalD.Web.Models;
using System.Drawing;

namespace RoyalD.Web.Services
{
    public class DebtorService
    {
        private readonly AppDbContext _db;

        public DebtorService(AppDbContext db) => _db = db;

        public Task<List<OutstandingDebt>> GetOutstandingAsync(
            string? search = null, 
            string? region = null, 
            string? province = null, 
            string? salesRep = null, 
            string? status = null, 
            string? credit = null,
            string? userAllowedRegion = null, 
            string? userAllowedProvinces = null, 
            string? userAllowedDistricts = null)
        {
            return GetDebtorsAsync(search, region, province, salesRep, status, credit, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);
        }

        private async Task<IQueryable<OutstandingDebt>> ApplySalesRepFilterAsync(IQueryable<OutstandingDebt> q, string? salesRep)
        {
            if (string.IsNullOrEmpty(salesRep)) return q;
            var repInputs = salesRep.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            var allDbReps = await _db.OutstandingDebts.AsNoTracking().Select(d => d.SalesRep).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToListAsync();
            var matchedReps = allDbReps.Where(dbRep => 
                repInputs.Any(u => 
                    dbRep.IndexOf(u, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    u.IndexOf(dbRep, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dbRep.Replace(" ", "").IndexOf(u.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) >= 0 ||
                    u.Replace(" ", "").IndexOf(dbRep.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) >= 0
                )
            ).ToList();
            if (matchedReps.Count > 0)
            {
                return q.Where(d => matchedReps.Contains(d.SalesRep));
            }
            return q.Where(d => repInputs.Contains(d.SalesRep) || d.SalesRep == salesRep);
        }

        private IQueryable<OutstandingDebt> ApplyAreaPermissions(IQueryable<OutstandingDebt> q, string? userAllowedRegion, string? userAllowedProvinces, string? userAllowedDistricts)
        {
            if (!string.IsNullOrEmpty(userAllowedRegion) || !string.IsNullOrEmpty(userAllowedProvinces))
            {
                var combinedAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(userAllowedRegion))
                {
                    var regionMatch = RegionHelper.GetMatchingProvinces(userAllowedRegion);
                    foreach (var p in regionMatch) combinedAllowed.Add(p);
                }
                if (!string.IsNullOrEmpty(userAllowedProvinces))
                {
                    var rawList = userAllowedProvinces.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
                    var provMatch = RegionHelper.ExpandProvinceVariants(rawList);
                    foreach (var p in provMatch) combinedAllowed.Add(p);
                }
                if (combinedAllowed.Count > 0)
                {
                    var allowedList = combinedAllowed.ToList();
                    q = q.Where(d => allowedList.Contains(d.Province));
                }
            }

            if (!string.IsNullOrEmpty(userAllowedDistricts))
            {
                var rawDist = userAllowedDistricts.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim());
                var allowedDistList = RegionHelper.ExpandDistrictVariants(rawDist);
                if (allowedDistList != null && allowedDistList.Count > 0)
                {
                    q = q.Where(d => allowedDistList.Contains(d.District));
                }
            }
            return q;
        }

        public async Task<List<OutstandingDebt>> GetDebtorsAsync(
            string? search = null, 
            string? region = null, 
            string? province = null, 
            string? salesRep = null, 
            string? status = null, 
            string? credit = null,
            string? userAllowedRegion = null, 
            string? userAllowedProvinces = null, 
            string? userAllowedDistricts = null)
        {
            var today = DateTime.Today;
            var q = _db.OutstandingDebts
                .AsNoTracking()
                .Include(d => d.PaymentRecords)
                .Include(d => d.Customer)
                .AsQueryable();

            // Filtering by status
            if (string.IsNullOrEmpty(status) || status == "All" || status == "AllUnpaid")
            {
                q = q.Where(d => d.Status != DebtStatus.PaidCash 
                              && d.Status != DebtStatus.PaidTransfer 
                              && d.Status != DebtStatus.PaidCheck 
                              && d.Status != DebtStatus.Cancelled);
            }
            else if (status == "NotDue")
            {
                q = q.Where(d => d.Status == DebtStatus.Outstanding && d.DueDate >= today);
            }
            else if (status == "Overdue")
            {
                q = q.Where(d => d.Status == DebtStatus.Outstanding && d.DueDate < today);
            }
            else if (status == "Overdue120")
            {
                var cutoff120 = today.AddDays(-120);
                q = q.Where(d => d.Status == DebtStatus.Outstanding && d.DueDate < cutoff120);
            }
            else if (Enum.TryParse<DebtStatus>(status, out var debtSt))
            {
                q = q.Where(d => d.Status == debtSt);
            }

            // Area permissions
            q = ApplyAreaPermissions(q, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);

            // UI Region & Province filters
            if (!string.IsNullOrEmpty(province))
            {
                var filterVariants = RegionHelper.ExpandProvinceVariants(new[] { province });
                q = q.Where(d => filterVariants.Contains(d.Province) || d.Province.Contains(province));
            }
            else if (!string.IsNullOrEmpty(region))
            {
                if (region == "กรุงเทพฯ" || region == "กรุงเทพ" || region == "กทม" || region.Equals("bkk", StringComparison.OrdinalIgnoreCase))
                {
                    var matchProvs = RegionHelper.GetMatchingProvinces(region);
                    q = q.Where(d => matchProvs.Contains(d.Province) || d.Province.Contains("กรุงเทพ") || d.Province.Contains("กทม"));
                }
                else if (region == "ต่างจังหวัด" || region.Equals("upcountry", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(d => !d.Province.Contains("กรุงเทพ") && !d.Province.Contains("กทม") && !d.Province.ToLower().Contains("bangkok"));
                }
                else
                {
                    var matchProvs = RegionHelper.GetMatchingProvinces(region);
                    if (matchProvs != null && matchProvs.Count > 0)
                    {
                        q = q.Where(d => matchProvs.Contains(d.Province));
                    }
                }
            }

            if (!string.IsNullOrEmpty(salesRep))
            {
                q = await ApplySalesRepFilterAsync(q, salesRep);
            }

            if (!string.IsNullOrEmpty(search))
                q = q.Where(d => d.CustomerName.Contains(search) || d.BillNo.Contains(search) || d.CustomerCode.Contains(search));

            if (!string.IsNullOrEmpty(credit))
            {
                if (credit == "0" || credit == "7" || credit == "0_7" || credit == "cash")
                {
                    q = q.Where(d => d.Credit == 0 || d.Credit == 7);
                }
                else if (int.TryParse(credit, out var cVal))
                {
                    if (cVal == 0 || cVal == 7)
                        q = q.Where(d => d.Credit == 0 || d.Credit == 7);
                    else
                        q = q.Where(d => d.Credit == cVal);
                }
            }

            return await q.OrderBy(d => d.DueDate).ToListAsync();
        }

        public async Task<List<OutstandingDebt>> GetPaidHistoryAsync(string? search = null, string? salesRep = null, string? userAllowedRegion = null, string? userAllowedProvinces = null, string? userAllowedDistricts = null)
        {
            var q = _db.OutstandingDebts
                .Include(d => d.PaymentRecords)
                .Include(d => d.Customer)
                .Where(d => d.Status == DebtStatus.PaidCash 
                         || d.Status == DebtStatus.PaidTransfer 
                         || d.Status == DebtStatus.PaidCheck);

            q = ApplyAreaPermissions(q, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);

            if (!string.IsNullOrEmpty(salesRep))
            {
                q = await ApplySalesRepFilterAsync(q, salesRep);
            }

            if (!string.IsNullOrEmpty(search))
                q = q.Where(d => d.CustomerName.Contains(search) || d.BillNo.Contains(search) || d.CustomerCode.Contains(search));

            var list = await q.OrderByDescending(d => d.FullyPaidDate ?? d.BillDate).ToListAsync();
            
            bool needsSave = false;
            foreach (var d in list.Where(x => string.IsNullOrEmpty(x.CustomerCode) || string.IsNullOrEmpty(x.CustomerName)))
            {
                var sb = await _db.SalesBills.FirstOrDefaultAsync(s => s.BillNo == d.BillNo);
                if (sb != null)
                {
                    if (string.IsNullOrEmpty(d.CustomerCode)) d.CustomerCode = sb.CustomerCode;
                    if (string.IsNullOrEmpty(d.CustomerName)) d.CustomerName = sb.CustomerName;
                    if (string.IsNullOrEmpty(d.District)) d.District = sb.District;
                    if (string.IsNullOrEmpty(d.Province)) d.Province = sb.Province;
                    if (string.IsNullOrEmpty(d.SalesRep)) d.SalesRep = sb.SalesRep;
                    needsSave = true;
                }
            }
            if (needsSave) await _db.SaveChangesAsync();
            
            return list;
        }

        public async Task<List<OutstandingDebt>> GetCancelledDebtsAsync(string? search = null, string? salesRep = null, string? userAllowedRegion = null, string? userAllowedProvinces = null, string? userAllowedDistricts = null)
        {
            var cutoffDate = DateTime.Today.AddDays(-30);
            var q = _db.OutstandingDebts
                .Include(d => d.Customer)
                .Where(d => d.Status == DebtStatus.Cancelled && (d.CancelledDate == null || d.CancelledDate >= cutoffDate));

            q = ApplyAreaPermissions(q, userAllowedRegion, userAllowedProvinces, userAllowedDistricts);

            if (!string.IsNullOrEmpty(salesRep))
            {
                q = await ApplySalesRepFilterAsync(q, salesRep);
            }

            if (!string.IsNullOrEmpty(search))
                q = q.Where(d => d.CustomerName.Contains(search) || d.BillNo.Contains(search) || d.CustomerCode.Contains(search));

            var list = await q.OrderByDescending(d => d.CancelledDate ?? d.BillDate).ToListAsync();
            
            bool needsSave = false;
            foreach (var d in list.Where(x => string.IsNullOrEmpty(x.CustomerCode) || string.IsNullOrEmpty(x.CustomerName)))
            {
                var sb = await _db.SalesBills.FirstOrDefaultAsync(s => s.BillNo == d.BillNo);
                if (sb != null)
                {
                    if (string.IsNullOrEmpty(d.CustomerCode)) d.CustomerCode = sb.CustomerCode;
                    if (string.IsNullOrEmpty(d.CustomerName)) d.CustomerName = sb.CustomerName;
                    if (string.IsNullOrEmpty(d.District)) d.District = sb.District;
                    if (string.IsNullOrEmpty(d.Province)) d.Province = sb.Province;
                    if (string.IsNullOrEmpty(d.SalesRep)) d.SalesRep = sb.SalesRep;
                    needsSave = true;
                }
            }
            if (needsSave) await _db.SaveChangesAsync();
            
            return list;
        }

        public async Task<byte[]> ExportDebtorsExcelAsync(List<OutstandingDebt> debts)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("การ์ดลูกหนี้คงค้าง");

            // Title Headers
            ws.Cells["A1"].Value = "บริษัท รอแยล-ดี (ไทยแลนด์) จำกัด";
            ws.Cells["A1:M1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 15;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2"].Value = "รายงานการ์ดลูกหนี้คงค้างและการติดตามการชำระเงิน";
            ws.Cells["A2:M2"].Merge = true;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.Font.Size = 12;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A3"].Value = $"ข้อมูล ณ วันที่: {DateTime.Now:dd/MM/yyyy HH:mm} | รวมทั้งหมด {debts.Count:N0} รายการ";
            ws.Cells["A3:M3"].Merge = true;
            ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells["A3"].Style.Font.Color.SetColor(Color.FromArgb(100, 116, 139));

            int row = 5;
            string[] headers = {
                "ลำดับ", "เลขที่บิล", "วันที่บิล", "PO Number", "วันครบกำหนด", "เครดิต (วัน)",
                "รหัสลูกค้า", "ชื่อลูกค้า", "จังหวัด", "ผู้แทนขาย",
                "ยอดบิลรวม (บาท)", "ยอดค้างชำระ (บาท)", "สถานะหนี้"
            };

            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cells[row, c + 1].Value = headers[c];
                ws.Cells[row, c + 1].Style.Font.Bold = true;
                ws.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110)); // #0f766e
                ws.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[row, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int idx = 1;
            var today = DateTime.Today;
            foreach (var d in debts)
            {
                row++;
                string statusTh = d.Status switch
                {
                    DebtStatus.Outstanding => (today - d.DueDate).TotalDays > 120 ? "เกิน 120 วัน" : (d.DueDate < today ? "เกินกำหนด" : "ยังไม่ถึงกำหนด"),
                    DebtStatus.Installment => "ผ่อนชำระ",
                    DebtStatus.Postponed => "เลื่อนนัดชำระ",
                    DebtStatus.BadDebt => "หนี้สูญ",
                    DebtStatus.CheckReturned => "เช็คคืน",
                    DebtStatus.Consignment => "ฝากขาย",
                    DebtStatus.ReturnIssued => "รับคืน(ออกใบลดหนี้)",
                    DebtStatus.ReturnPending => "รับคืน(รอออกใบลดหนี้)",
                    DebtStatus.ChangeProduct => "เปลี่ยนสินค้า",
                    DebtStatus.Delivering => "บิลอยู่จัดส่ง",
                    DebtStatus.WaitingGoods => "รอสินค้า",
                    DebtStatus.PaidCash => "ชำระเงินสดครบ",
                    DebtStatus.PaidTransfer => "โอนเงินครบ",
                    DebtStatus.PaidCheck => "ชำระเช็คครบ",
                    DebtStatus.Cancelled => "ยกเลิกบิล",
                    _ => d.Status.ToString()
                };

                ws.Cells[row, 1].Value = idx++;
                ws.Cells[row, 2].Value = d.BillNo;
                ws.Cells[row, 3].Value = d.BillDate.ToString("dd/MM/yyyy");
                ws.Cells[row, 4].Value = d.PoNumber;
                ws.Cells[row, 5].Value = d.DueDate.ToString("dd/MM/yyyy");
                ws.Cells[row, 6].Value = d.Credit;
                ws.Cells[row, 7].Value = d.CustomerCode;
                ws.Cells[row, 8].Value = d.CustomerName;
                ws.Cells[row, 9].Value = d.Province ?? "-";
                ws.Cells[row, 10].Value = d.SalesRep ?? "-";
                ws.Cells[row, 11].Value = d.OriginalAmount;
                ws.Cells[row, 12].Value = d.RemainingAmount;
                ws.Cells[row, 13].Value = statusTh;

                // Center specific columns
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Format numbers
                ws.Cells[row, 11].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 12].Style.Numberformat.Format = "#,##0.00";

                // Highlight Overdue 120+
                if (d.Status == DebtStatus.Outstanding && (today - d.DueDate).TotalDays > 120)
                {
                    ws.Cells[row, 1, row, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 13].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(254, 226, 226)); // Soft light red
                    ws.Cells[row, 12].Style.Font.Color.SetColor(Color.FromArgb(185, 28, 28));
                    ws.Cells[row, 12].Style.Font.Bold = true;
                }
            }

            // Summary Row
            row++;
            ws.Cells[row, 1].Value = "รวมทั้งหมด";
            ws.Cells[row, 1, row, 10].Merge = true;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws.Cells[row, 11].Formula = $"SUM(K6:K{row - 1})";
            ws.Cells[row, 12].Formula = $"SUM(L6:L{row - 1})";
            ws.Cells[row, 11].Style.Font.Bold = true;
            ws.Cells[row, 12].Style.Font.Bold = true;
            ws.Cells[row, 11].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 12].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 1, row, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 13].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));

            ws.Cells.AutoFitColumns();
            return await pkg.GetAsByteArrayAsync();
        }
    }
}
