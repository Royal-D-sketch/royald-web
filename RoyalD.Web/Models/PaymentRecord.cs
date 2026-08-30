using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    public enum PaymentMethod
    {
        Cash = 0,
        Transfer = 1,
        Check = 2
    }

    public enum CheckStatus
    {
        None = 0,
        Pending = 1,    // รอเรียกเก็บ
        Cleared = 2,    // ผ่านแล้ว
        Returned = 3    // เช็คคืน
    }

    public class PaymentRecord
    {
        [Key]
        public int Id { get; set; }

        public int OutstandingDebtId { get; set; }

        public DateTime PaidDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        public PaymentMethod Method { get; set; }

        public CheckStatus CheckStatus { get; set; } = CheckStatus.None;

        [MaxLength(50)]
        public string CheckNumber { get; set; } = string.Empty;

        public DateTime? CheckDate { get; set; }

        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation
        public OutstandingDebt? OutstandingDebt { get; set; }
        public ICollection<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();
    }
}
