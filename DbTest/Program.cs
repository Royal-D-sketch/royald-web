using System;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using System.Linq;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(""Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;"");

using (var db = new AppDbContext(optionsBuilder.Options))
{
    var debts = db.OutstandingDebts.Where(d => d.BillNo == ""R143965"").ToList();
    Console.WriteLine($""Debts found: {debts.Count}"");
    foreach (var d in debts)
    {
         Console.WriteLine($""Debt ID: {d.Id}, Original: {d.OriginalAmount}, Remaining: {d.RemainingAmount}, Status: {(int)d.Status}"");
    }

    var bill = db.SalesBills.FirstOrDefault(b => b.BillNo == ""R143965"");
    if (bill != null)
    {
         Console.WriteLine($""Bill TotalAmount: {bill.TotalAmount}"");
    }
}
