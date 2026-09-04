using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

namespace CheckNames
{
    class Program
    {
        static void Main(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql("Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;");
            
            using (var db = new AppDbContext(optionsBuilder.Options))
            {
                var fixes = new System.Collections.Generic.Dictionary<string, string> {
                    { "บ้านกระปุกย", "บ้านกระปุกยา" }
                };

                int totalFixed = 0;
                foreach(var kvp in fixes)
                {
                    var c = db.Customers.FirstOrDefault(x => x.Name == kvp.Key);
                    if (c != null) { c.Name = kvp.Value; totalFixed++; }

                    var debts = db.OutstandingDebts.Where(d => d.CustomerName == kvp.Key).ToList();
                    foreach(var d in debts) { d.CustomerName = kvp.Value; totalFixed++; }

                    var bills = db.SalesBills.Where(b => b.CustomerName == kvp.Key).ToList();
                    foreach(var b in bills) { b.CustomerName = kvp.Value; totalFixed++; }
                }
                
                db.SaveChanges();
                Console.WriteLine("Fixed " + totalFixed + " records for บ้านกระปุกยา.");
            }
        }
    }
}
