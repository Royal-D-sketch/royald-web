
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DbFixTool {
    public class AppDbContext : DbContext {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<SalesBill> SalesBills { get; set; }
        public DbSet<OutstandingDebt> OutstandingDebts { get; set; }
    }
    public class SalesBill {
        [Key] public string BillNo { get; set; }
        public string CustomerCode { get; set; }
        public int Credit { get; set; }
    }
    public class OutstandingDebt {
        [Key] public string BillNo { get; set; }
        public string CustomerCode { get; set; }
        public int Credit { get; set; }
    }
    class Program {
        static void Main() {
            var opt = new DbContextOptionsBuilder<AppDbContext>();
            opt.UseNpgsql("Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;");
            using var db = new AppDbContext(opt.Options);
            
            var map = new Dictionary<string, int> {
                {"R146558", 7}, {"R146559", 7}, {"R146560", 7}, {"R152793", 7},
                {"R140984", 7}, {"R140985", 45}, {"R140986", 45}, {"R153150", 7},
                {"R153151", 45}, {"R143275", 7}, {"R145801", 7}, {"R145802", 7},
                {"R145803", 7}, {"R145804", 7}, {"R149664", 7}, {"R153152", 45}
            };
            
            foreach (var kvp in map) {
                var b = db.SalesBills.FirstOrDefault(x => x.BillNo == kvp.Key);
                if (b != null) b.Credit = kvp.Value;
                
                var d = db.OutstandingDebts.FirstOrDefault(x => x.BillNo == kvp.Key);
                if (d != null) d.Credit = kvp.Value;
            }
            db.SaveChanges();
            Console.WriteLine("RESTORED CORRECT CREDITS");
        }
    }
}

