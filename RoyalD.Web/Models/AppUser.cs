using System.ComponentModel.DataAnnotations;

namespace RoyalD.Web.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }
        
        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Role { get; set; } = "user"; // admin, user

        [MaxLength(100)]
        public string Position { get; set; } = "ผู้แทนขาย"; // ผู้แทนขาย, พนักงาน, หัวหน้างาน, ผู้บริหาร, ผู้ดูแลระบบ

        [MaxLength(2000)]
        public string SalesRepCode { get; set; } = string.Empty; // รหัสผู้แทนขาย (ถ้ามี หรือเลือกหลายคน)

        [MaxLength(500)]
        public string AllowedRegion { get; set; } = string.Empty; // ภาคที่รับผิดชอบ

        [MaxLength(2000)]
        public string AllowedProvinces { get; set; } = string.Empty; // จังหวัดที่รับผิดชอบ (คั่นด้วย comma)

        [MaxLength(5000)]
        public string AllowedDistricts { get; set; } = string.Empty; // เขต/อำเภอที่รับผิดชอบ (คั่นด้วย comma)

        [MaxLength(1000)]
        public string AllowedPages { get; set; } = "Dashboard,SalesBill,Debtor,DebtorHistory,SalesReport,WaitingGoods,PaymentDetails"; // สิทธิ์เข้าดูหน้าจอ (คั่นด้วย comma)

        public bool CanViewPaymentDetails { get; set; } = true; // สิทธิ์ดูรายละเอียดการรับชำระเงิน
        public bool CanChangeDebtStatus { get; set; } = false; // สิทธิ์เปลี่ยนสถานะหนี้รายการอื่นๆ (บิลค้างชำระทั่วไป)
        public bool CanChangePaidBillStatus { get; set; } = false; // สิทธิ์เปลี่ยนสถานะบิลชำระครบแล้ว
        public bool CanCancelBill { get; set; } = false; // สิทธิ์เปลี่ยนเป็นบิลยกเลิก
        public bool CanRestoreCancelledBill { get; set; } = false; // สิทธิ์กู้คืนบิลยกเลิก (เปลี่ยนสถานะบิลยกเลิกกลับเป็นบิลปกติ)
        public bool CanManageReturnedBills { get; set; } = false; // สิทธิ์เปลี่ยนสถานะบิลส่งกลับคืนบัญชี และบิลส่งคืนให้จัดส่ง
        public bool CanDeleteSalesBill { get; set; } = false; // สิทธิ์ลบบิลขาย
        public bool CanDeleteDebtor { get; set; } = false; // สิทธิ์ลบการ์ดลูกหนี้
        public bool CanDownload { get; set; } = false; // สิทธิ์ดาวน์โหลดไฟล์ (Excel, PDF, Export)
        public bool CanScreenCapture { get; set; } = false; // สิทธิ์แคปหน้าจอ (ถ้า false แล้วกดแคป จะจอดำ+เตะออกทันที)

        public bool CanDownloadOrScreenCapture 
        { 
            get => CanDownload || CanScreenCapture; 
            set 
            { 
                CanDownload = value; 
                CanScreenCapture = value; 
            } 
        }

        [MaxLength(100)]
        public string CurrentSessionToken { get; set; } = string.Empty; // ใช้ตรวจจับ Multi-login

        /// <summary>
        /// null = ไม่จำกัดเวลา, 10 = Timeout 10 นาที
        /// </summary>
        public int? SessionTimeoutMinutes { get; set; } = 15;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
    }
}