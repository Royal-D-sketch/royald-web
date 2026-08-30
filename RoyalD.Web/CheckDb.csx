using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(""Data Source=royal-d-debtor.db"").Options;
using var db = new AppDbContext(options);

var debt = db.OutstandingDebts.Find(6848);
if (debt != null) {
    Console.WriteLine(""BillNo: "" + debt.BillNo);
    var products = db.SalesBillItems.Where(i => i.BillNo == debt.BillNo).ToList();
    Console.WriteLine(""Products count: "" + products.Count);
    foreach(var p in products) {
        Console.WriteLine(p.ProductCode + "" - "" + p.ProductName);
    }
}
