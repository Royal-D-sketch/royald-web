using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

class Program {
    static void Main() {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(@"Data Source=C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\RoyalD.Web\app.db");
        using var db = new AppDbContext(optionsBuilder.Options);
        
        var bills = db.SalesBills.Include(b => b.Items).OrderByDescending(b => b.BillDate).Take(2).ToList();
        foreach (var b in bills) {
            Console.WriteLine($"Bill: {b.BillNo}, Items count: {b.Items.Count}");
            foreach (var i in b.Items) {
                Console.WriteLine($" - {i.ProductName} Qty:{i.Qty} Price:{i.Price} Amt:{i.Amount}");
            }
        }
    }
}