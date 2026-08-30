using System.ComponentModel.DataAnnotations;

namespace RoyalD.Web.Models
{
    public class Customer
    {
        [Key, MaxLength(200)]
        public string CustomerCode { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string District { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Province { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Phone { get; set; } = string.Empty;

        // Navigation
        public ICollection<SalesBill> SalesBills { get; set; } = new List<SalesBill>();
        public ICollection<OutstandingDebt> OutstandingDebts { get; set; } = new List<OutstandingDebt>();
    }
}

