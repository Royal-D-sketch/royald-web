using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

class Program {
    static void Main() {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;");
        using var db = new AppDbContext(optionsBuilder.Options);
        
        var recentBills = db.SalesBills.Include(b => b.Items).OrderByDescending(b => b.BillDate).Take(3).ToList();
        foreach (var recentBill in recentBills) {
            Console.WriteLine($"BillNo: {recentBill.BillNo}, TotalAmount: {recentBill.TotalAmount}");
            foreach (var item in recentBill.Items) {
                Console.WriteLine($"  Product: {item.ProductName}, Qty: {item.Qty}, Unit: {item.Unit}, Price: {item.Price}, Disc: {item.Discount}, Amt: {item.Amount}");
            }
        }
    }
}