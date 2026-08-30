using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    public class SalesBill
    {
        [Key, MaxLength(200)]
        public string BillNo { get; set; } = string.Empty;

        public DateTime BillDate { get; set; }

        [MaxLength(20)]
        public string CustomerCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string District { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Province { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Phone { get; set; } = string.Empty;

        public int Credit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(200)]
        public string SalesRep { get; set; } = string.Empty;

        /// <summary>เนเธเธฅเนเธ•เนเธเธ—เธฒเธ เน€เธเนเธ 2026-01, 2026-08</summary>
        [MaxLength(10)]
        public string SourceMonth { get; set; } = string.Empty;

        /// <summary>เน€เธฅเธเธ—เธตเนเนเธเธชเธฑเนเธเธเธทเนเธญ (PO Number) เธเธฒเธเนเธเธฅเนเธเธดเธฅเธเธฒเธข</summary>
        [MaxLength(200)]
        public string PoNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ReceiptNo { get; set; } = string.Empty;

        public DateTime? ReceiptDate { get; set; }

        public bool IsFullyPaid { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
        public ICollection<SalesBillItem> Items { get; set; } = new List<SalesBillItem>();
    }
}

