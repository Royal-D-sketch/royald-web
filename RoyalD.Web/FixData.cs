using Microsoft.EntityFrameworkCore;
using System.Linq;
using RoyalD.Web.Models;

namespace RoyalD.Web
{
    public class FixData
    {
        public static void Run(AppDbContext db)
        {
            // Sync from Debts to Bills
            var debts = db.OutstandingDebts.Where(d => !string.IsNullOrEmpty(d.SalesRep)).ToList();
            var bills = db.SalesBills.Where(b => string.IsNullOrEmpty(b.SalesRep)).ToList();
            foreach(var b in bills) {
                var d = debts.FirstOrDefault(x => x.BillNo == b.BillNo);
                if (d != null) b.SalesRep = d.SalesRep;
            }
            
            // Sync from Bills to Debts
            var billsFull = db.SalesBills.Where(b => !string.IsNullOrEmpty(b.SalesRep) || !string.IsNullOrEmpty(b.CustomerCode)).ToList();
            var debtsEmpty = db.OutstandingDebts.Where(d => string.IsNullOrEmpty(d.CustomerCode) || string.IsNullOrEmpty(d.SalesRep)).ToList();
            foreach(var d in debtsEmpty) {
                var b = billsFull.FirstOrDefault(x => x.BillNo == d.BillNo);
                if (b != null) {
                    if (string.IsNullOrEmpty(d.CustomerCode)) d.CustomerCode = b.CustomerCode;
                    if (string.IsNullOrEmpty(d.CustomerName)) d.CustomerName = b.CustomerName;
                    if (string.IsNullOrEmpty(d.District)) d.District = b.District;
                    if (string.IsNullOrEmpty(d.Province)) d.Province = b.Province;
                    if (string.IsNullOrEmpty(d.SalesRep)) d.SalesRep = b.SalesRep;
                }
            }
            db.SaveChanges();
            System.Console.WriteLine("Data Synced.");
        }
    }
}
