using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

class Program {
    static void Main() {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;");
        using var db = new AppDbContext(optionsBuilder.Options);
        
        var bill = db.SalesBills.FirstOrDefault(b => b.BillNo == "R148474");
        if (bill != null) {
            Console.WriteLine($"BillNo: {bill.BillNo}, BillDate: {bill.BillDate:O}");
        }
    }
}