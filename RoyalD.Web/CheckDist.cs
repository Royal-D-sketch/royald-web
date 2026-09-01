using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using System.Linq;
using System;

namespace RoyalD.Web {
    public class CheckDist {
        public static void Run(AppDbContext db) {
            var items = db.OutstandingDebts.Take(5).ToList();
            foreach(var i in items) {
                Console.WriteLine($"Code: {i.CustomerCode}, Name: {i.CustomerName}, District: {i.District}, Province: {i.Province}");
            }
        }
    }
}
