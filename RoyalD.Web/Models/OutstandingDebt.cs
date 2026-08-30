using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    /// <summary>
    /// เธชเธ–เธฒเธเธฐเธฅเธนเธเธซเธเธตเน
    /// </summary>
    public enum DebtStatus
    {
        Outstanding = 0,    // เธเนเธฒเธเธเธณเธฃเธฐ
        PaidCash = 1,       // เธเธณเธฃเธฐเธ”เนเธงเธขเน€เธเธดเธเธชเธ”
        PaidTransfer = 2,   // เธเธณเธฃเธฐเธ”เนเธงเธขเนเธญเธเน€เธเธดเธ
        PaidCheck = 3,      // เธเธณเธฃเธฐเธ”เนเธงเธขเน€เธเนเธ
        Installment = 4,    // เธเนเธญเธเธเธณเธฃเธฐ
        Postponed = 5,      // เน€เธฅเธทเนเธญเธเธเธฑเธ”เธเธณเธฃเธฐ
        BadDebt = 6,        // เธซเธเธตเนเธชเธนเธ
        CheckReturned = 7,  // เน€เธเนเธเธเธทเธ
        Consignment = 8,    // เธชเธดเธเธเนเธฒเธเธฒเธเธเธฒเธข
        ReturnIssued = 9,   // เธฃเธฑเธเธเธทเธเธชเธดเธเธเนเธฒ (เธญเธญเธเนเธเธฅเธ”เธซเธเธตเนเนเธฅเนเธง)
        ReturnPending = 10, // เธฃเธฑเธเธเธทเธเธชเธดเธเธเนเธฒ (เธฃเธญเธญเธญเธเนเธเธฅเธ”เธซเธเธตเน)
        ChangeProduct = 11, // เน€เธเธฅเธตเนเธขเธเธชเธดเธเธเนเธฒ
        Delivering = 12,    // เธเธดเธฅเธญเธขเธนเนเธเธฑเธ”เธชเนเธ
        WaitingGoods = 13,  // เธฃเธญเธชเธดเธเธเนเธฒ
        Cancelled = 14      // เธเธดเธฅเธขเธเน€เธฅเธดเธ
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

        /// <summary>เธงเธฑเธเธ—เธตเนเธเธณเธฃเธฐเธเธฃเธ (เนเธเนเธเธฑเธ 120 เธงเธฑเธ)</summary>
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

        // เธเธดเธฅเธ”เนเธชเธณเธซเธฃเธฑเธเธเธดเธฅเธขเธเน€เธฅเธดเธ (เน€เธเนเธเธเธดเธฅ 40 เธงเธฑเธ)
        public DateTime? CancelledDate { get; set; }
        [MaxLength(200)]
        public string? CancelledBy { get; set; }
        [MaxLength(500)]
        public string? CancelReason { get; set; }
        
        public bool IsLocked { get; set; } = false; // เธชเธณเธซเธฃเธฑเธเธเนเธญเธเธเธฑเธเนเธเนเนเธเธเธดเธฅเธ—เธตเนเธเธณเธฃเธฐเธเธฃเธเนเธฅเนเธง

        [MaxLength(200)]
        public string Note { get; set; } = string.Empty;

        /// <summary>เน€เธฅเธเธ—เธตเนเนเธเน€เธชเธฃเนเธ เธเธฒเธเธเธฒเธฃเธเธฑเธเธเธนเนเนเธเธฅเนเธชเธฃเธธเธเธฃเธฑเธเน€เธเธดเธ</summary>
        [MaxLength(200)]
        public string ReceiptNo { get; set; } = string.Empty;

        /// <summary>เธงเธฑเธเธ—เธตเนเธฃเธฑเธเธเธณเธฃเธฐเน€เธเธดเธ เธเธฒเธเนเธเธฅเนเธชเธฃเธธเธเธฃเธฑเธเน€เธเธดเธ</summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>เน€เธฅเธเธ—เธตเนเนเธเธชเธฑเนเธเธเธทเนเธญ (PO Number)</summary>
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

