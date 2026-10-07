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
        /// ตรวจสอบว่าผู้ใช้ปัจจุบันมีสิทธิ์ในการเปลี่ยนสถานะหนี้หรือไม่ (เฉพาะคุณธัญชนก และผู้บริหาร)
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

            // 2. ผู้บริหาร (Position มีคำว่า "ผู้บริหาร", Username "Kullaya", "ART", หรือ Role "admin")
            if (username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase) ||
                username.Equals("ART", StringComparison.OrdinalIgnoreCase) ||
                position.Contains("ผู้บริหาร") ||
                role == "admin" || role == "administrator")
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
        /// ตรวจสอบรหัสผ่านของผู้อนุมัติ: ต้องเป็นรหัสผ่านของ "คุณธัญชนก" หรือ "ผู้บริหาร" เท่านั้น
        /// </summary>
        public static async Task<(bool IsValid, string ApproverName)> VerifyApproverPasswordAsync(AppDbContext db, string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return (false, "");
            string cleanPwd = password.Trim();

            // 1. Master admin password fallback
            if (cleanPwd == "029030445Rd*" || cleanPwd == "029030445")
            {
                return (true, "ผู้ดูแลระบบ (Master Admin)");
            }

            // 2. ดึงเฉพาะบัญชี คุณธัญชนก และ ผู้บริหาร จากฐานข้อมูล
            var approverUsers = await db.Users.AsNoTracking()
                .Where(u => u.IsActive && (
                    u.Username == "admin" ||
                    u.Username == "Kullaya" ||
                    u.Username == "ART" ||
                    (u.Position != null && u.Position.Contains("ผู้บริหาร")) ||
                    (u.FullName != null && u.FullName.Contains("ธัญชนก"))
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
                    string roleDesc = (u.Position != null && u.Position.Contains("ผู้บริหาร")) || u.Username == "Kullaya"
                        ? "ผู้บริหาร"
                        : "คุณธัญชนก";
                    return (true, $"{u.FullName ?? u.Username} ({roleDesc})");
                }
            }

            return (false, "");
        }
    }
}
