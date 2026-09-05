
using System;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using System.Linq;

namespace DbTest4 {
    class Program {
        static void Main(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql("Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;");

            using (var db = new AppDbContext(optionsBuilder.Options)) {
                var brokenAttachments = db.FileAttachments.Where(f => f.FilePath.StartsWith("/uploads/")).ToList();
                Console.WriteLine("Found " + brokenAttachments.Count + " broken attachments.");
                db.FileAttachments.RemoveRange(brokenAttachments);
                db.SaveChanges();
                Console.WriteLine("Deleted broken attachments.");
            }
        }
    }
}

