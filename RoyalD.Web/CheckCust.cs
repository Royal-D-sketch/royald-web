using System;
using System.Linq;
using RoyalD.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Check
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=app.db").Options;
            using var db = new AppDbContext(options);
            var bills = db.SalesBills.Where(b => b.CustomerName == "" || b.CustomerName == null).Take(10).ToList();
            Console.WriteLine("Empty CustomerName bills: " + bills.Count);
            foreach(var b in bills) {
                Console.WriteLine(b.BillNo + " | " + b.CustomerCode + " | " + b.CustomerName);
            }
        }
    }
}