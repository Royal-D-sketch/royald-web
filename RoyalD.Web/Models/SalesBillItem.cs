using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    public class SalesBillItem
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string BillNo { get; set; } = string.Empty;

        [MaxLength(30)]
        public string ProductCode { get; set; } = string.Empty;

        [MaxLength(300)]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Qty { get; set; }

        [MaxLength(30)]
        public string Unit { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Navigation
        public SalesBill? SalesBill { get; set; }
    }
}
