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
            
                Console.WriteLine("All active users OK: " + db.Users.Count(u => u.IsActive));
            }
        }
    }
}
