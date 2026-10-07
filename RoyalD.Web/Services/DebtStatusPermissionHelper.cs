using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

namespace RoyalD.Web.Services
{
    public static class DebtStatusPermissionHelper
    {
        /// <summary>
        /// ตรวจสอบว่าผู้ใช้ปัจจุบันมีสิทธิ์ในการเปลี่ยนสถานะหนี้หรือไม่ (เฉพาะคุณธัญชนก, ตำแหน่งผู้บริหาร, และ Admin)
        /// หมายเหตุ: คุณณรงค์เดช (ART) ตำแหน่งเป็น "พนักงาน" แต่มีบทบาท (Role) เป็น "admin" จึงมีสิทธิ์เข้าถึงในฐานะ admin (ไม่ใช่ตำแหน่งผู้บริหาร)
        /// </summary>
        public static bool CanChangeDebtStatus(AppUser? user, ClaimsPrincipal? principal = null)
        {
            if (user == null && principal == null) return false;

            string username = (user?.Username ?? principal?.Identity?.Name ?? "").Trim();
            string fullName = (user?.FullName ?? principal?.FindFirst("FullName")?.Value ?? "").Trim();
            string position = (user?.Position ?? principal?.FindFirst("Position")?.Value ?? "").Trim();
            string role = (user?.Role ?? principal?.FindFirst(ClaimTypes.Role)?.Value ?? principal?.FindFirst("Role")?.Value ?? "").Trim().ToLower();

            // 1. คุณธัญชนก (Username "admin" หรือ ชื่อ-นามสกุล มีคำว่า "ธัญชนก")
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) || fullName.Contains("ธัญชนก"))
                return true;

            // 2. ผู้ดูแลระบบ (Admin) เช่น ART (ตำแหน่งพนักงาน แต่บทบาทเป็น admin)
            if (role == "admin" || role == "administrator" || username.Equals("ART", StringComparison.OrdinalIgnoreCase))
                return true;

            // 3. ตำแหน่งผู้บริหาร (Position มีคำว่า "ผู้บริหาร", Username "Kullaya")
            if (username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase) ||
                position.Contains("ผู้บริหาร"))
                return true;

            return false;
        }

        /// <summary>
        /// ตรวจสอบว่าผู้ใช้มีสิทธิ์เข้าถึง/เปลี่ยนสถานะ "บิลส่งคืนกลับมาบัญชี" (ReturnedToAccount) หรือไม่
        /// (เฉพาะคุณธัญชนก, ตำแหน่งผู้บริหาร, คุณวนิดา, admin เท่านั้น)
        /// หมายเหตุ: คุณณรงค์เดช (ART) ตำแหน่งพนักงาน มีสิทธิ์ผ่านบทบาท admin
        /// </summary>
        public static bool CanManageReturnedBills(AppUser? user, ClaimsPrincipal? principal = null)
        {
            if (user == null && principal == null) return false;

            string username = (user?.Username ?? principal?.Identity?.Name ?? "").Trim();
            string fullName = (user?.FullName ?? principal?.FindFirst("FullName")?.Value ?? "").Trim();
            string position = (user?.Position ?? principal?.FindFirst("Position")?.Value ?? "").Trim();
            string role = (user?.Role ?? principal?.FindFirst(ClaimTypes.Role)?.Value ?? principal?.FindFirst("Role")?.Value ?? "").Trim().ToLower();

            // 1. admin (Role "admin" รวมถึง Username "admin", "ART")
            if (role == "admin" || role == "administrator" || 
                username.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
                username.Equals("ART", StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. คุณธัญชนก (Username "admin" หรือ ชื่อ-นามสกุล มีคำว่า "ธัญชนก")
            if (fullName.Contains("ธัญชนก"))
                return true;

            // 3. ตำแหน่งผู้บริหาร (Position มีคำว่า "ผู้บริหาร", Username "Kullaya")
            if (username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase) ||
                position.Contains("ผู้บริหาร"))
                return true;

            // 4. คุณวนิดา (Username "nid" หรือ ชื่อ-นามสกุล มีคำว่า "วนิดา")
            if (username.Equals("nid", StringComparison.OrdinalIgnoreCase) || fullName.Contains("วนิดา"))
                return true;

            // 5. Flag สิทธิ์ CanManageReturnedBills ที่กำหนดเจาะจง
            if (user?.CanManageReturnedBills == true)
                return true;

            var claimCanManage = principal?.FindFirst("CanManageReturnedBills")?.Value;
            if (claimCanManage != null && claimCanManage.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// ตรวจสอบว่าบิลนี้เป็นบิลที่ชำระเงินครบแล้วหรือไม่
        /// </summary>
        public static bool IsBillPaid(SalesBill? bill, OutstandingDebt? debt)
        {
            if (bill != null && bill.IsFullyPaid) return true;
            if (debt != null)
            {
                if (debt.Status == DebtStatus.PaidCash || 
                    debt.Status == DebtStatus.PaidTransfer || 
                    debt.Status == DebtStatus.PaidCheck)
                    return true;

                if (debt.FullyPaidDate.HasValue) return true;
                if (debt.RemainingAmount <= 0 && debt.Status != DebtStatus.Cancelled && debt.Status != DebtStatus.BadDebt) return true;
            }
            return false;
        }

        /// <summary>
        /// ตรวจสอบรหัสผ่านของผู้อนุมัติ:
        /// ผู้ใช้ที่มีสิทธิ์เข้าถึง (คุณธัญชนก, ตำแหน่งผู้บริหาร, คุณวนิดา, Admin เช่น ART) 
        /// สามารถใช้รหัสผ่านเข้าสู่ระบบของผู้ใช้เอง หรือใส่รหัสผ่าน 029030445Rd* ได้
        /// </summary>
        public static async Task<(bool IsValid, string ApproverName)> VerifyApproverPasswordAsync(AppDbContext db, string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return (false, "");
            string cleanPwd = password.Trim();

            // 1. รหัสผ่านส่วนกลาง (Master password)
            if (cleanPwd == "029030445Rd*" || cleanPwd == "029030445")
            {
                return (true, "รหัสผ่านส่วนกลาง (Master Password 029030445Rd*)");
            }

            // 2. ดึงบัญชีผู้ใช้ที่มีสิทธิ์เข้าถึงจากฐานข้อมูล:
            // - admin (Role == admin, Username == "admin", Username == "ART" ซึ่งตำแหน่งเป็นพนักงานแต่บทบาทเป็น admin)
            // - คุณธัญชนก
            // - ตำแหน่งผู้บริหาร (Username == "Kullaya", Position มีคำว่า "ผู้บริหาร")
            // - คุณวนิดา (Username == "nid", FullName มีคำว่า "วนิดา")
            // - ผู้ที่มี Flag CanManageReturnedBills เป็น true
            var approverUsers = await db.Users.AsNoTracking()
                .Where(u => u.IsActive && (
                    (u.Role != null && (u.Role.ToLower() == "admin" || u.Role.ToLower() == "administrator")) ||
                    u.Username == "admin" ||
                    u.Username == "ART" ||
                    u.Username == "Kullaya" ||
                    u.Username == "nid" ||
                    (u.Position != null && u.Position.Contains("ผู้บริหาร")) ||
                    (u.FullName != null && (u.FullName.Contains("ธัญชนก") || u.FullName.Contains("วนิดา"))) ||
                    u.CanManageReturnedBills
                ))
                .ToListAsync();

            foreach (var u in approverUsers)
            {
                if (string.IsNullOrEmpty(u.PasswordHash)) continue;

                bool isMatch = false;
                try
                {
                    isMatch = BCrypt.Net.BCrypt.Verify(cleanPwd, u.PasswordHash);
                }
                catch { }

                if (!isMatch)
                {
                    try
                    {
                        var sha = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(cleanPwd)));
                        if (sha == u.PasswordHash || cleanPwd == u.PasswordHash) isMatch = true;
                    }
                    catch { }
                }

                if (isMatch)
                {
                    string roleDesc;
                    string lowerRole = (u.Role ?? "").Trim().ToLower();
                    if (lowerRole == "admin" || lowerRole == "administrator" || 
                        u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
                        u.Username.Equals("ART", StringComparison.OrdinalIgnoreCase))
                    {
                        roleDesc = (u.FullName != null && u.FullName.Contains("ธัญชนก")) 
                            ? "คุณธัญชนก / ผู้ดูแลระบบ" 
                            : "ผู้ดูแลระบบ (Admin)";
                    }
                    else if ((u.Position != null && u.Position.Contains("ผู้บริหาร")) || 
                             u.Username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase))
                    {
                        roleDesc = "ผู้บริหาร";
                    }
                    else if (u.Username.Equals("nid", StringComparison.OrdinalIgnoreCase) || 
                             (u.FullName != null && u.FullName.Contains("วนิดา")))
                    {
                        roleDesc = "คุณวนิดา (บัญชี)";
                    }
                    else if (u.FullName != null && u.FullName.Contains("ธัญชนก"))
                    {
                        roleDesc = "คุณธัญชนก";
                    }
                    else
                    {
                        roleDesc = "ผู้มีสิทธิ์";
                    }

                    return (true, $"{u.FullName ?? u.Username} ({roleDesc})");
                }
            }

            return (false, "");
        }
    }
}
