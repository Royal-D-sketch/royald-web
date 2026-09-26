namespace RoyalD.Web.Models
{
    public class DashboardSummary
    {
        public int TotalDebtors { get; set; }
        public decimal TotalAmount { get; set; }
        
        public RegionSummary Bangkok { get; set; } = new();
        public RegionSummary Upcountry { get; set; } = new();

        // 4 Main Categories for Dashboard Top Cards & Drilldown
        public DebtCategory Cash7Days { get; set; } = new();      // เก็บสด 7 วัน (กทม.&ปริมณฑล)
        public DebtCategory Cash10Days { get; set; } = new();     // เงินสด 10 วัน (กทม.&ปริมณฑล)
        public DebtCategory Overdue120Days { get; set; } = new(); // ค้างชำระ > 120 วัน
        public DebtCategory Collected { get; set; } = new();      // ยอดเก็บเงินสำเร็จ

        public List<DashboardBillItem> AllDrilldownBills { get; set; } = new();
    }

    public class DashboardBillItem
    {
        public string BillNo { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public string SalesRep { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty; // cash7, cash10, over120, collected
        public string CategoryName { get; set; } = string.Empty;
        public int Credit { get; set; }
        public DateTime DueDate { get; set; }
        public int AgingDays { get; set; }
        public string StatusName { get; set; } = string.Empty;
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
