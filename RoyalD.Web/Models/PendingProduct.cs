using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    public class PendingProduct
    {
        [Key]
        public int Id { get; set; }

        public int OutstandingDebtId { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("OutstandingDebtId")]
        public OutstandingDebt? OutstandingDebt { get; set; }
    }
}
