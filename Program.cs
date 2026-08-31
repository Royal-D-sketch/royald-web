using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

class Program {
    static void Main() {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(@"Data Source=C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\RoyalD.Web\app.db");
        using var db = new AppDbContext(optionsBuilder.Options);
        
        var recentBill = db.SalesBills.Include(b => b.Items).OrderByDescending(b => b.CreatedAt).FirstOrDefault();
        if (recentBill != null) {
            Console.WriteLine($"BillNo: {recentBill.BillNo}, TotalAmount: {recentBill.TotalAmount}");
            foreach (var item in recentBill.Items) {
                Console.WriteLine($"  Product: {item.ProductName}, Qty: {item.Qty}, Unit: {item.Unit}, Price: {item.Price}, Amt: {item.Amount}");
            }
        }
    }
}