using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

class ProgramCheck {
    static void Main() {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=royal_d.db").Options;
        using var db = new AppDbContext(options);
        
        var d = db.OutstandingDebts.Where(x => x.ReceiptDate != null).Select(x => x.ReceiptDate).ToList();
        
        var groups = d.GroupBy(x => x.Value.ToString("yyyy-MM")).Select(g => new { M = g.Key, C = g.Count() }).OrderBy(x => x.M).ToList();
        foreach (var g in groups) {
            Console.WriteLine($"{g.M}: {g.C}");
        }
    }
}
