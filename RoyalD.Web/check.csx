using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(""Data Source=app.db"").Options;
using var db = new AppDbContext(options);

var mayBills = db.SalesBills.Where(b => b.SourceMonth == ""2026-05"").Take(5).ToList();
foreach(var b in mayBills) {
    Console.WriteLine($""SalesBill: {b.BillNo}, Amount: {b.TotalAmount}, Date: {b.BillDate}"");
}

var mayDebts = db.OutstandingDebts.Where(d => d.BillDate >= new DateTime(2026, 5, 1) && d.BillDate <= new DateTime(2026, 5, 31)).Take(5).ToList();
foreach(var d in mayDebts) {
    Console.WriteLine($""OutstandingDebt: {d.BillNo}, OriginalAmount: {d.OriginalAmount}"");
}

var bkk = db.OutstandingDebts.Select(d => d.Province).Distinct().ToList();
Console.WriteLine(""Provinces: "" + string.Join("", "", bkk));
