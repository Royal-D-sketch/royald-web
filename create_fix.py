code = """using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

namespace RoyalD.Web.Controllers
{
    public class FixController : Controller
    {
        private readonly AppDbContext _db;
        public FixController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("Fix/CustomerName")]
        public async Task<IActionResult> CustomerName()
        {
            string code = "740114";
            string correctName = "น.ส.วารินทร์ อภิวัฒนเบญญ";
            
            var bills = await _db.SalesBills.Where(b => b.CustomerCode == code).ToListAsync();
            int bCount = 0;
            foreach(var b in bills) {
                b.CustomerName = correctName;
                bCount++;
            }
            
            var debts = await _db.OutstandingDebts.Where(d => d.CustomerCode == code).ToListAsync();
            int dCount = 0;
            foreach(var d in debts) {
                d.CustomerName = correctName;
                dCount++;
            }
            
            await _db.SaveChangesAsync();
            return Content($"Updated {bCount} SalesBills and {dCount} OutstandingDebts for {code}");
        }
    }
}"""

path = 'RoyalD.Web/Controllers/FixController.cs'
with open(path, 'w', encoding='utf-8') as f:
    f.write(code)
