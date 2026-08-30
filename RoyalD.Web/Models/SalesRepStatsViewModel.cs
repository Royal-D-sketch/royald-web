namespace RoyalD.Web.Models
{
    public class SalesRepStatsViewModel
    {
        public string SalesRepName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal Outstanding { get; set; }
    }
}
