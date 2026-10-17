using System;
using System.Collections.Generic;

namespace RoyalD.Web.Models
{
    public class ProductItemReconcileRow
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal SalesQty { get; set; }
        public decimal SalesAmount { get; set; }
        
        // Credit Note Deduction
        public bool HasCreditNote => CreditAmount > 0 || ReturnQty > 0;
        public List<string> CreditNoteNos { get; set; } = new();
        public decimal ReturnQty { get; set; }
        public decimal CreditAmount { get; set; }

        // Net Result
        public decimal NetSalesAmount => SalesAmount - CreditAmount;
        public decimal NetQty => SalesQty - ReturnQty;
    }

    public class ProductGroupSummaryItem
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public decimal GroupSalesQty { get; set; }
        public decimal GroupSalesAmount { get; set; }
        public decimal GroupReturnQty { get; set; }
        public decimal GroupCreditAmount { get; set; }
        public decimal GroupNetSalesAmount => GroupSalesAmount - GroupCreditAmount;
        public decimal GroupNetQty => GroupSalesQty - GroupReturnQty;
        public List<ProductItemReconcileRow> Items { get; set; } = new();
    }

    public class Month25Option
    {
        public string Key { get; set; } = string.Empty; // e.g. "2026-10"
        public string Label { get; set; } = string.Empty; // e.g. "รอบ 25 ต.ค. 69 (26/09/26 - 25/10/26)"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ProductGroupSalesReportViewModel
    {
        public List<string> SalesReps { get; set; } = new();
        public string SelectedSalesRep { get; set; } = string.Empty;
        public bool IsRestrictedRep { get; set; }
        public string SelectedMonth25 { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Month25Option> AvailableMonths25 { get; set; } = new();

        // Grand Totals
        public decimal TotalGrossSales { get; set; }
        public decimal TotalCreditNotes { get; set; }
        public decimal TotalNetSales => TotalGrossSales - TotalCreditNotes;
        public decimal TotalSalesQty { get; set; }
        public decimal TotalReturnQty { get; set; }
        public decimal TotalNetQty => TotalSalesQty - TotalReturnQty;
        public int TotalCreditNoteCount { get; set; }

        public List<ProductGroupSummaryItem> Groups { get; set; } = new();
    }
}
