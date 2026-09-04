using System;
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
        [Route("Fix/Pending")]
        public async Task<IActionResult> FixPending()
        {
            string billNo = "637/31825";
            var bill = await _db.SalesBills.Include(b => b.Items).FirstOrDefaultAsync(b => b.BillNo == billNo);
            if (bill == null) return Content("Bill not found in SalesBills");
            
            var debt = await _db.OutstandingDebts.Include(d => d.PendingProducts).FirstOrDefaultAsync(d => d.BillNo == billNo);
            if (debt == null) return Content("Debt not found");
            
            if (debt.PendingProducts == null) debt.PendingProducts = new System.Collections.Generic.List<PendingProduct>();
            debt.PendingProducts.Clear();
            
            foreach(var item in bill.Items)
            {
                debt.PendingProducts.Add(new PendingProduct
                {
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Quantity = (int)item.Qty
                });
            }
            
            await _db.SaveChangesAsync();
            return Content($"Added {debt.PendingProducts.Count} products to PendingProducts for {billNo}");
        }
    }
}
