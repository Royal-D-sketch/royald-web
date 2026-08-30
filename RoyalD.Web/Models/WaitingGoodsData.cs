namespace RoyalD.Web.Models
{
    using System;
    using System.Collections.Generic;

    public class WaitingGoodsData
    {
        public string BillNo { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string SalesRep { get; set; } = string.Empty;
        
        public List<SalesBillItem> Products { get; set; } = new List<SalesBillItem>();
    }
}
