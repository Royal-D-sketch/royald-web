
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DbFixTool {
    public class AppDbContext : DbContext {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<SalesBill> SalesBills { get; set; }
    }
    public class Customer {
        [Key] public string CustomerCode { get; set; }
        public string Phone { get; set; }
    }
    public class SalesBill {
        [Key] public string BillNo { get; set; }
        public string CustomerCode { get; set; }
        public string Phone { get; set; }
        public int Credit { get; set; }
    }
    class Program {
        static void Main() {
            var opt = new DbContextOptionsBuilder<AppDbContext>();
            opt.UseNpgsql("Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;");
            using var db = new AppDbContext(opt.Options);
            
            var b = db.SalesBills.FirstOrDefault(x => x.BillNo == "R152858");
            if (b == null) Console.WriteLine("BILL NOT FOUND: R152858");
            else {
                Console.WriteLine($"BILL FOUND: {b.BillNo}, Credit: {b.Credit}, Phone: '{b.Phone}'");
                var c = db.Customers.FirstOrDefault(x => x.CustomerCode == b.CustomerCode);
                if (c == null) Console.WriteLine("CUST NOT FOUND");
                else Console.WriteLine($"CUST FOUND: Phone: '{c.Phone}'");
            }
            
            var b2 = db.SalesBills.FirstOrDefault(x => x.BillNo == "R153152");
            if (b2 != null) {
                Console.WriteLine($"BILL FOUND: {b2.BillNo}, Credit: {b2.Credit}, Phone: '{b2.Phone}'");
                var c2 = db.Customers.FirstOrDefault(x => x.CustomerCode == b2.CustomerCode);
                if (c2 != null) Console.WriteLine($"CUST FOUND: Phone: '{c2.Phone}'");
            }
        }
    }
}

