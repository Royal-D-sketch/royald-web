using System.ComponentModel.DataAnnotations;

namespace RoyalD.Web.Models
{
    public class FileAttachment
    {
        [Key]
        public int Id { get; set; }

        public int? OutstandingDebtId { get; set; }
        public int? PaymentRecordId { get; set; }

        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(50)]
        public string ContentType { get; set; } = string.Empty; // e.g. application/pdf, image/jpeg

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string UploadedBy { get; set; } = string.Empty;

        // Navigation
        public OutstandingDebt? OutstandingDebt { get; set; }
        public PaymentRecord? PaymentRecord { get; set; }
    }
}
