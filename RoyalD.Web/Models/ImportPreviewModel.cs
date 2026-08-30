using System;
using System.Collections.Generic;

namespace RoyalD.Web.Models
{
    public class ImportPreviewResult
    {
        public string PreviewId { get; set; } = Guid.NewGuid().ToString("N");
        public string FileType { get; set; } = "";
        public string FileName { get; set; } = "";
        public bool IsCurrentMonth { get; set; }
        public int TotalRows { get; set; }
        public int NewCount { get; set; }
        public int DuplicateChangedCount { get; set; }
        public int DuplicateSameCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal NewAmount { get; set; }
        public decimal DuplicateAmount { get; set; }
        public List<BillPreviewItem> Items { get; set; } = new();
    }

    public class BillPreviewItem
    {
        public string BillNo { get; set; } = "";
        public DateTime BillDate { get; set; }
        public string CustomerCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string PoNumber { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public string Phone { get; set; } = "";
        public string SalesRep { get; set; } = "";
        public int Credit { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ExistingAmount { get; set; }
        public string StatusType { get; set; } = "NEW"; // NEW, CHANGED, SAME
        public int ItemCount { get; set; }
        public List<SalesBillItem> Items { get; set; } = new();
    }
}
