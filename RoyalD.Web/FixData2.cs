using System;
using System.IO;
using ExcelDataReader;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using System.Linq;

namespace RoyalD.Web
{
    public class FixData2
    {
        public static void Run(AppDbContext db)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var path = @"C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\raw_data\รายงานลูกหนี้คงค้าง.XLS";
            if (!File.Exists(path)) return;
            
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var conf = new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } };
            var ds = reader.AsDataSet(conf);
            var tbl = ds.Tables[0];
            
            var debts = db.OutstandingDebts.ToList();
            var custs = db.Customers.Select(c => c.CustomerCode).ToHashSet();
            int updated = 0;
            
            string currentCustCode = "";
            string currentCustName = "";
            string currentDistrict = "";
            string currentProvince = "";
            string currentSalesRep = "";

            for (int r = 0; r < tbl.Rows.Count; r++) {
                var row = tbl.Rows[r];
                var c0 = row[0]?.ToString()?.Trim() ?? "";
                var c7 = tbl.Columns.Count > 7 ? row[7]?.ToString()?.Trim() ?? "" : "";
                
                if (!string.IsNullOrEmpty(c0)) {
                    currentCustCode = c0;
                    currentCustName = tbl.Columns.Count > 1 ? row[1]?.ToString()?.Trim() ?? "" : "";
                    currentDistrict = tbl.Columns.Count > 5 ? row[5]?.ToString()?.Trim() ?? "" : "";
                    currentProvince = tbl.Columns.Count > 6 ? row[6]?.ToString()?.Trim() ?? "" : "";
                    currentSalesRep = tbl.Columns.Count > 12 ? row[12]?.ToString()?.Trim() ?? "" : "";
                    
                    if (!custs.Contains(currentCustCode)) {
                        db.Customers.Add(new Customer {
                            CustomerCode = currentCustCode,
                            Name = currentCustName,
                            District = currentDistrict,
                            Province = currentProvince
                        });
                        custs.Add(currentCustCode);
                    }
                }
                
                if (!string.IsNullOrEmpty(c7)) {
                    var billNo = c7;
                    var match = debts.FirstOrDefault(d => d.BillNo == billNo);
                    if (match != null) {
                        bool changed = false;
                        if (string.IsNullOrEmpty(match.CustomerCode) || match.CustomerCode != currentCustCode) { match.CustomerCode = currentCustCode; changed = true; }
                        if (string.IsNullOrEmpty(match.CustomerName) || match.CustomerName != currentCustName) { match.CustomerName = currentCustName; changed = true; }
                        if (string.IsNullOrEmpty(match.District) || match.District != currentDistrict) { match.District = currentDistrict; changed = true; }
                        if (string.IsNullOrEmpty(match.Province) || match.Province != currentProvince) { match.Province = currentProvince; changed = true; }
                        if (string.IsNullOrEmpty(match.SalesRep) || match.SalesRep != currentSalesRep) { match.SalesRep = currentSalesRep; changed = true; }
                        if (changed) updated++;
                    }
                }
            }
            
            db.SaveChanges();
            Console.WriteLine($"FixData2: Updated {updated} rows from Excel");
            
            var bills = db.SalesBills.Where(b => string.IsNullOrEmpty(b.SalesRep) || string.IsNullOrEmpty(b.CustomerCode)).ToList();
            foreach(var b in bills) {
                var d = debts.FirstOrDefault(x => x.BillNo == b.BillNo);
                if (d != null) {
                    if (string.IsNullOrEmpty(b.SalesRep)) b.SalesRep = d.SalesRep;
                    if (string.IsNullOrEmpty(b.CustomerCode)) b.CustomerCode = d.CustomerCode;
                }
            }
            db.SaveChanges();
        }
    }
}
