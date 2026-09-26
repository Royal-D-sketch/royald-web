using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    /// <summary>
    /// สถานะลูกหนี้
    /// </summary>
    public enum DebtStatus
    {
        Outstanding = 0,    // ค้างชำระ
        PaidCash = 1,       // ชำระด้วยเงินสด
        PaidTransfer = 2,   // ชำระด้วยโอนเงิน
        PaidCheck = 3,      // ชำระด้วยเช็ค
        Installment = 4,    // ผ่อนชำระ
        Postponed = 5,      // เลื่อนนัดชำระ
        BadDebt = 6,        // หนี้สูญ
        CheckReturned = 7,  // เช็คคืน
        Consignment = 8,    // สินค้าฝากขาย
        ReturnIssued = 9,   // รับคืนสินค้า (ออกใบลดหนี้แล้ว)
        ReturnPending = 10, // รับคืนสินค้า (รอออกใบลดหนี้)
        ChangeProduct = 11, // เปลี่ยนสินค้า
        Delivering = 12,    // บิลอยู่จัดส่ง
        WaitingGoods = 13,  // รอสินค้า
        Cancelled = 14,     // บิลยกเลิก
        ReturnedToAccount = 15 // บิลส่งคืนกลับมาบัญชี (บิลไม่พร้อมส่ง/ลูกค้ายังไม่เอาของ)
    }

    public class OutstandingDebt
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(20)]
        public string CustomerCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string District { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Province { get; set; } = string.Empty;

        [MaxLength(200)]
        public string BillNo { get; set; } = string.Empty;

        public DateTime BillDate { get; set; }

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public int Credit { get; set; }

        [MaxLength(200)]
        public string SalesRep { get; set; } = string.Empty;

        public DebtStatus Status { get; set; } = DebtStatus.Outstanding;

        public DateTime? PaidDate { get; set; }

        /// <summary>วันที่ชำระครบ (ใช้นับ 120 วัน)</summary>
        public DateTime? FullyPaidDate { get; set; }

        public DateTime? PostponedDate { get; set; }
        
        public DateTime? BadDebtDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BadDebtAmount { get; set; }

        public DateTime? DeliveringDate { get; set; }
        
        public DateTime? WaitingGoodsDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ReturnAmount { get; set; }
        public bool IsReturnCutFromBill { get; set; }

        // ฟิลด์สำหรับบิลส่งคืนกลับมาบัญชี (บิลไม่พร้อมส่ง/ลูกค้ายังไม่เอาของ)
        public DateTime? ReturnedToAccountDate { get; set; }
        [MaxLength(500)]
        public string? ReturnedToAccountReason { get; set; }
        public DateTime? ReturnedToDeliveryDate { get; set; }

        // ฟิลด์สำหรับบิลยกเลิก (เก็บบิล 40 วัน)
        public DateTime? CancelledDate { get; set; }
        [MaxLength(200)]
        public string? CancelledBy { get; set; }
        [MaxLength(500)]
        public string? CancelReason { get; set; }
        
        public bool IsLocked { get; set; } = false; // สำหรับป้องกันแก้ไขบิลที่ชำระครบแล้ว

        [MaxLength(200)]
        public string Note { get; set; } = string.Empty;

        /// <summary>เลขที่ใบเสร็จ จากการจับคู่ไฟล์สรุปรับเงิน</summary>
        [MaxLength(200)]
        public string ReceiptNo { get; set; } = string.Empty;

        /// <summary>วันที่รับชำระเงิน จากไฟล์สรุปรับเงิน</summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>เลขที่ใบสั่งซื้อ (PO Number)</summary>
        [MaxLength(200)]
        public string PoNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string LastEditedBy { get; set; } = string.Empty;
        
        public DateTime? LastEditedDate { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
        public ICollection<PaymentRecord> PaymentRecords { get; set; } = new List<PaymentRecord>();
        public ICollection<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();
        public ICollection<PendingProduct> PendingProducts { get; set; } = new List<PendingProduct>();
    }
}
