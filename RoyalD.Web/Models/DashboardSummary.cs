namespace RoyalD.Web.Models
{
    public class DashboardSummary
    {
        public int TotalDebtors { get; set; }
        public decimal TotalAmount { get; set; }
        
        public RegionSummary Bangkok { get; set; } = new();
        public RegionSummary Upcountry { get; set; } = new();
    }

    public class RegionSummary
    {
        public int BillCount { get; set; }
        public decimal TotalAmount { get; set; }

        public DebtCategory OutstandingTotal { get; set; } = new();
        public DebtCategory LessThan120Days { get; set; } = new();
        public DebtCategory Over120Days { get; set; } = new();
        public DebtCategory Paid { get; set; } = new();
    }

    public class DebtCategory
    {
        public int BillCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
