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
        public const string MasterPasswordBackup = "029030445Rd*";
        public const string MasterPasswordOld = "029030445";

        /// <summary>
        /// ตรวจสอบสิทธิ์เปลี่ยนสถานะหนี้รายการอื่นๆ ที่ไม่ใช่สถานะครบแล้ว (บิลค้างชำระทั่วไป: ผ่อนชำระ, เลื่อนนัด, รอสินค้า, จัดส่ง, เปลี่ยนสินค้า, รับคืน, ฝากขาย, หนี้สูญ)
        /// ผู้มีสิทธิ์: ผู้ที่ได้รับสิทธิ์ CanChangeDebtStatus, admin, หรือกลุ่มผู้บริหาร/หัวหน้า/คุณธัญชนก/คุณกุลยา
        /// </summary>
        public static bool CanChangeDebtStatus(AppUser? user, ClaimsPrincipal? principal = null)
        {
            if (user == null && principal == null) return false;

            if (user?.CanChangeDebtStatus == true)
                return true;

            var claimChange = principal?.FindFirst("CanChangeDebtStatus")?.Value;
            if (claimChange != null && claimChange.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            string username = (user?.Username ?? principal?.Identity?.Name ?? "").Trim();
            string fullName = (user?.FullName ?? principal?.FindFirst("FullName")?.Value ?? "").Trim();
            string position = (user?.Position ?? principal?.FindFirst("Position")?.Value ?? "").Trim();
            string role = (user?.Role ?? principal?.FindFirst(ClaimTypes.Role)?.Value ?? principal?.FindFirst("Role")?.Value ?? "").Trim().ToLower();

            // 1. admin (Role "admin" รวมถึง Username "admin", "ART", คุณปภาวดี อินจันทร์)
            if (role == "admin" || role == "administrator" || 
                username.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
                username.Equals("ART", StringComparison.OrdinalIgnoreCase) || 
                fullName.Contains("ปภาวดี"))
                return true;

            // 2. คุณธัญชนก
            if (fullName.Contains("ธัญชนก"))
                return true;

            // 3. คุณกุลยา
            if (username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase) || fullName.Contains("กุลยา"))
                return true;

            // 4. หัวหน้า (Position มีคำว่า "หัวหน้า" เช่น หัวหน้างาน, หัวหน้าแผนก)
            if (position.Contains("หัวหน้า"))
                return true;

            // 5. ผู้บริหาร (Position มีคำว่า "ผู้บริหาร")
            if (position.Contains("ผู้บริหาร"))
                return true;

            return false;
        }

        /// <summary>
        /// ตรวจสอบสิทธิ์เปลี่ยนสถานะบิลที่ชำระเงินครบแล้ว (Fully Paid Bills)
        /// ผู้มีสิทธิ์: คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร หรือได้รับสิทธิ์ CanChangePaidBillStatus
        /// หมายเหตุ: ผู้ใช้อื่นที่ไม่มีสิทธิ์ต้องไม่มีสิทธิ์กดเด็ดขาด
        /// </summary>
        public static bool CanChangePaidBillStatus(AppUser? user, ClaimsPrincipal? principal = null)
        {
            if (user == null && principal == null) return false;

            if (user?.CanChangePaidBillStatus == true)
                return true;

            var claimPaid = principal?.FindFirst("CanChangePaidBillStatus")?.Value;
            if (claimPaid != null && claimPaid.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            string username = (user?.Username ?? principal?.Identity?.Name ?? "").Trim();
            string fullName = (user?.FullName ?? principal?.FindFirst("FullName")?.Value ?? "").Trim();
            string position = (user?.Position ?? principal?.FindFirst("Position")?.Value ?? "").Trim();
            string role = (user?.Role ?? principal?.FindFirst(ClaimTypes.Role)?.Value ?? principal?.FindFirst("Role")?.Value ?? "").Trim().ToLower();

            // 1. admin (Role "admin" รวมถึง Username "admin", "ART", คุณปภาวดี อินจันทร์)
            if (role == "admin" || role == "administrator" || 
                username.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
                username.Equals("ART", StringComparison.OrdinalIgnoreCase) || 
                fullName.Contains("ปภาวดี"))
                return true;

            // 2. คุณธัญชนก
            if (fullName.Contains("ธัญชนก"))
                return true;

            // 3. คุณกุลยา
            if (username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase) || fullName.Contains("กุลยา"))
                return true;

            // 4. หัวหน้า (Position มีคำว่า "หัวหน้า" เช่น หัวหน้างาน, หัวหน้าแผนก)
            if (position.Contains("หัวหน้า"))
                return true;

            // 5. ผู้บริหาร (Position มีคำว่า "ผู้บริหาร")
            if (position.Contains("ผู้บริหาร"))
                return true;

            return false;
        }

        /// <summary>
        /// ตรวจสอบสิทธิ์เปลี่ยนสถานะบิลจากค้างปกติเป็นบิลส่งกลับบัญชี (ReturnedToAccount) และบิลส่งไปจัดส่ง (ReturnToDelivery)
        /// ผู้มีสิทธิ์: คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร, คุณวนิดา หรือได้รับสิทธิ์ CanManageReturnedBills
        /// หมายเหตุ: คุณปภาวดี อินจันทร์ (ART) ตำแหน่งพนักงาน มีสิทธิ์ผ่านบทบาท admin
        /// </summary>
        public static bool CanManageReturnedBills(AppUser? user, ClaimsPrincipal? principal = null)
        {
            if (user == null && principal == null) return false;

            if (user?.CanManageReturnedBills == true)
                return true;

            var claimCanManage = principal?.FindFirst("CanManageReturnedBills")?.Value;
            if (claimCanManage != null && claimCanManage.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            string username = (user?.Username ?? principal?.Identity?.Name ?? "").Trim();
            string fullName = (user?.FullName ?? principal?.FindFirst("FullName")?.Value ?? "").Trim();
            string position = (user?.Position ?? principal?.FindFirst("Position")?.Value ?? "").Trim();
            string role = (user?.Role ?? principal?.FindFirst(ClaimTypes.Role)?.Value ?? principal?.FindFirst("Role")?.Value ?? "").Trim().ToLower();

            // 1. admin (Role "admin" รวมถึง Username "admin", "ART", คุณปภาวดี อินจันทร์)
            if (role == "admin" || role == "administrator" || 
                username.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
                username.Equals("ART", StringComparison.OrdinalIgnoreCase) || 
                fullName.Contains("ปภาวดี"))
                return true;

            // 2. คุณธัญชนก
            if (fullName.Contains("ธัญชนก"))
                return true;

            // 3. คุณกุลยา
            if (username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase) || fullName.Contains("กุลยา"))
                return true;

            // 4. หัวหน้า (Position มีคำว่า "หัวหน้า")
            if (position.Contains("หัวหน้า"))
                return true;

            // 5. ผู้บริหาร (Position มีคำว่า "ผู้บริหาร")
            if (position.Contains("ผู้บริหาร"))
                return true;

            // 6. คุณวนิดา (Username "nid" หรือ ชื่อมีคำว่า "วนิดา")
            if (username.Equals("nid", StringComparison.OrdinalIgnoreCase) || fullName.Contains("วนิดา"))
                return true;

            return false;
        }

        /// <summary>
        /// ตรวจสอบสิทธิ์เปลี่ยนสถานะเป็น "บิลยกเลิก" (Cancelled)
        /// ผู้มีสิทธิ์: คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร, คุณวนิดา หรือได้รับสิทธิ์ CanCancelBill
        /// </summary>
        public static bool CanCancelBill(AppUser? user, ClaimsPrincipal? principal = null)
        {
            if (user == null && principal == null) return false;

            if (user?.CanCancelBill == true)
                return true;

            var claimCancel = principal?.FindFirst("CanCancelBill")?.Value;
            if (claimCancel != null && claimCancel.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            // ใช้สิทธิ์กลุ่มเดียวกับบิลส่งคืน (6 กลุ่ม)
            return CanManageReturnedBills(user, principal);
        }

        /// <summary>
        /// สำหรับความเข้ากันได้กับโค้ดเดิม
        /// </summary>
        public static bool CanCancelDebtStatus(AppUser? user, ClaimsPrincipal? principal = null)
        {
            return CanCancelBill(user, principal);
        }

        /// <summary>
        /// ตรวจสอบสิทธิ์กู้คืนจากบิลยกเลิกกลับเป็นบิลค้างชำระปกติ
        /// ผู้มีสิทธิ์: คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร หรือได้รับสิทธิ์ CanChangePaidBillStatus (คุณวนิดา ไม่มีสิทธิ์)
        /// </summary>
        public static bool CanRestoreCancelledBill(AppUser? user, ClaimsPrincipal? principal = null)
        {
            return CanChangePaidBillStatus(user, principal);
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
        /// ตรวจสอบรหัสผ่านสำหรับการเปลี่ยนสถานะบิลที่ชำระแล้ว:
        /// ผู้มีสิทธิ์: คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร
        /// สามารถใช้รหัสผ่านของผู้ใช้เอง หรือรหัสผ่านสำรอง 029030445Rd*
        /// </summary>
        public static async Task<(bool IsValid, string ApproverName)> VerifyPaidBillApproverPasswordAsync(AppDbContext db, string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return (false, "");
            string cleanPwd = password.Trim();

            // 1. รหัสผ่านสำรอง (Backup password)
            if (cleanPwd == MasterPasswordBackup || cleanPwd == MasterPasswordOld)
            {
                return (true, "รหัสผ่านสำรอง (029030445Rd*)");
            }

            // 2. ดึงเฉพาะบัญชี: admin, คุณธัญชนก, คุณกุลยา, หัวหน้า, ผู้บริหาร หรือผู้ได้รับสิทธิ์ CanChangePaidBillStatus
            // (คุณวนิดา ไม่มีสิทธิ์ในบิลที่ชำระแล้ว)
            var approverUsers = await db.Users.AsNoTracking()
                .Where(u => u.IsActive && (
                    (u.Role != null && (u.Role.ToLower() == "admin" || u.Role.ToLower() == "administrator")) ||
                    u.Username == "admin" ||
                    u.Username == "ART" ||
                    u.Username == "Kullaya" ||
                    (u.Position != null && (u.Position.Contains("หัวหน้า") || u.Position.Contains("ผู้บริหาร"))) ||
                    (u.FullName != null && (u.FullName.Contains("ธัญชนก") || u.FullName.Contains("กุลยา") || u.FullName.Contains("ปภาวดี"))) ||
                    u.CanChangePaidBillStatus
                ))
                .ToListAsync();

            return CheckUserPasswordMatch(approverUsers, cleanPwd);
        }

        /// <summary>
        /// ตรวจสอบรหัสผ่านสำหรับการเปลี่ยนสถานะบิลจากค้างปกติเป็นบิลส่งกลับบัญชี และบิลส่งไปจัดส่ง:
        /// ผู้มีสิทธิ์: คุณธัญชนก, คุณกุลยา, admin, หัวหน้า, ผู้บริหาร, คุณวนิดา หรือได้รับสิทธิ์ CanManageReturnedBills / CanCancelBill
        /// สามารถใช้รหัสผ่านของผู้ใช้เอง หรือรหัสผ่านสำรอง 029030445Rd*
        /// </summary>
        public static async Task<(bool IsValid, string ApproverName)> VerifyReturnedBillApproverPasswordAsync(AppDbContext db, string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return (false, "");
            string cleanPwd = password.Trim();

            // 1. รหัสผ่านสำรอง (Backup password)
            if (cleanPwd == MasterPasswordBackup || cleanPwd == MasterPasswordOld)
            {
                return (true, "รหัสผ่านสำรอง (029030445Rd*)");
            }

            // 2. ดึงเฉพาะบัญชี: admin, คุณธัญชนก, คุณกุลยา, หัวหน้า, ผู้บริหาร, คุณวนิดา
            var approverUsers = await db.Users.AsNoTracking()
                .Where(u => u.IsActive && (
                    (u.Role != null && (u.Role.ToLower() == "admin" || u.Role.ToLower() == "administrator")) ||
                    u.Username == "admin" ||
                    u.Username == "ART" ||
                    u.Username == "Kullaya" ||
                    u.Username == "nid" ||
                    (u.Position != null && (u.Position.Contains("หัวหน้า") || u.Position.Contains("ผู้บริหาร"))) ||
                    (u.FullName != null && (u.FullName.Contains("ธัญชนก") || u.FullName.Contains("กุลยา") || u.FullName.Contains("วนิดา") || u.FullName.Contains("ปภาวดี"))) ||
                    u.CanManageReturnedBills ||
                    u.CanCancelBill
                ))
                .ToListAsync();

            return CheckUserPasswordMatch(approverUsers, cleanPwd);
        }

        /// <summary>
        /// เข้ากันได้กับโค้ดเดิม: ตรวจสอบรหัสผ่านผู้อนุมัติ
        /// </summary>
        public static Task<(bool IsValid, string ApproverName)> VerifyApproverPasswordAsync(AppDbContext db, string? password, bool isForReturnedBill = false)
        {
            return isForReturnedBill
                ? VerifyReturnedBillApproverPasswordAsync(db, password)
                : VerifyPaidBillApproverPasswordAsync(db, password);
        }

        private static (bool IsValid, string ApproverName) CheckUserPasswordMatch(System.Collections.Generic.List<AppUser> users, string cleanPwd)
        {
            foreach (var u in users)
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
                        u.Username.Equals("ART", StringComparison.OrdinalIgnoreCase) ||
                        (u.FullName != null && u.FullName.Contains("ปภาวดี")))
                    {
                        roleDesc = (u.FullName != null && u.FullName.Contains("ธัญชนก")) 
                            ? "คุณธัญชนก / ผู้ดูแลระบบ" 
                            : "ผู้ดูแลระบบ (Admin)";
                    }
                    else if (u.Username.Equals("Kullaya", StringComparison.OrdinalIgnoreCase) || 
                             (u.FullName != null && u.FullName.Contains("กุลยา")))
                    {
                        roleDesc = "คุณกุลยา (ผู้บริหาร)";
                    }
                    else if (u.Position != null && u.Position.Contains("ผู้บริหาร"))
                    {
                        roleDesc = "ผู้บริหาร";
                    }
                    else if (u.Position != null && u.Position.Contains("หัวหน้า"))
                    {
                        roleDesc = $"หัวหน้า ({u.Position})";
                    }
                    else if (u.Username.Equals("nid", StringComparison.OrdinalIgnoreCase) || 
                             (u.FullName != null && u.FullName.Contains("วนิดา")))
                    {
                        roleDesc = "คุณวนิดา";
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
