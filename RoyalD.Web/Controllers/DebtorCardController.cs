using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class DebtorCardController : Controller
    {
        public IActionResult Index(string? search, string? region, string? province, string? salesRep, string? status, string? credit = null)
        {
            return RedirectToAction("Index", "Debtor", new { search, region, province, salesRep, status, credit });
        }
    }
}
