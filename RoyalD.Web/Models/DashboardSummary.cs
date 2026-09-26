namespace RoyalD.Web.Models
{
    public class DashboardSummary
    {
        public int TotalDebtors { get; set; }
        public decimal TotalAmount { get; set; } // Total Sales Amount
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalCollectedAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public double CollectionRate => (TotalSalesAmount > 0) ? (double)Math.Round((TotalCollectedAmount / TotalSalesAmount) * 100, 2) : 0;
        
        public RegionSummary Bangkok { get; set; } = new();
        public RegionSummary Upcountry { get; set; } = new();

        // 4 Main Categories for Dashboard Top Cards & Drilldown
        public DebtCategory Cash7Days { get; set; } = new();      // เก็บสด 7 วัน (กทม.&ปริมณฑล)
        public DebtCategory Cash10Days { get; set; } = new();     // เงินสดรวมสายเวลา (กทม.&ปริมณฑล)
        public DebtCategory CashTimelineBkk => Cash10Days;        // Alias for clarity
        public DebtCategory UpcountryDebts { get; set; } = new(); // หนี้ค้างชำระต่างจังหวัดทั้งหมด
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
        public string Category { get; set; } = string.Empty; // cash7, cash10, upcountry, over120, collected
        public string CategoryName { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty; // "1", "2", "3"
        public int Credit { get; set; }
        public DateTime DueDate { get; set; }
        public int AgingDays { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public bool IsBkk { get; set; }
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

    public class ComparisonBoardViewModel
    {
        public DashboardSummary Summary { get; set; } = new();
        public List<DashboardBillItem> AllBills { get; set; } = new();
        public List<DashboardBillItem> Table1_Cash7Bkk { get; set; } = new();
        public List<DashboardBillItem> Table2_CashTimelineBkk { get; set; } = new();
        public List<DashboardBillItem> Table3_Upcountry { get; set; } = new();

        public decimal Table1_TotalAmount => Table1_Cash7Bkk.Sum(x => x.Amount);
        public decimal Table2_TotalAmount => Table2_CashTimelineBkk.Sum(x => x.Amount);
        public decimal Table3_TotalAmount => Table3_Upcountry.Sum(x => x.Amount);
    }
}
