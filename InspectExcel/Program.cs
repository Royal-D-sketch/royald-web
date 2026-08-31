using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

class Program {
    static void Main() {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(@"Data Source=C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\RoyalD.Web\app.db");
        using var db = new AppDbContext(optionsBuilder.Options);
        
        var debts = db.OutstandingDebts.Where(d => d.ReceiptNo == "RD002/2569" || d.CustomerName.Contains("RD002")).ToList();
        foreach(var d in debts) {
            Console.WriteLine($"Bill: {d.BillNo}, Receipt: {d.ReceiptNo}, CustCode: {d.CustomerCode}, CustName: {d.CustomerName}");
        }
    }
}