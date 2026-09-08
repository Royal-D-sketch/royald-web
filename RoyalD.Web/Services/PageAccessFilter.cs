using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;

namespace RoyalD.Web.Services
{
    public class PageAccessFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public PageAccessFilter(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                if (user.IsInRole("admin") || user.IsInRole("Admin") || (user.Identity?.Name?.ToLower() == "admin") || ((user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "").ToLower() == "admin"))
                {
                    await next();
                    return;
                }

                var controller = context.RouteData.Values["controller"]?.ToString()?.ToLower() ?? "";
                var action = context.RouteData.Values["action"]?.ToString()?.ToLower() ?? "";

                // ข้าม Account controller (Login, Logout, AccessDenied, ForceLogout) และ Home
                if (controller == "account" || controller == "home")
                {
                    await next();
                    return;
                }

                // Cache lookup for 30 seconds to make page clicks blazing fast while reflecting changes quickly
                var username = user.Identity.Name ?? "";
                var (role, allowedPages) = await _cache.GetOrCreateAsync($"user_perm_{username}", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                    var dbUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
                    return (dbUser?.Role ?? "user", dbUser?.AllowedPages ?? "");
                });

                if (role?.ToLower() == "admin" || username.ToLower() == "admin")
                {
                    await next();
                    return;
                }

                var allowedList = (allowedPages ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(p => p.Trim().ToLower())
                                               .ToList();

                bool isAllowed = false;
                if (controller == "dashboard" && allowedList.Contains("dashboard")) isAllowed = true;
                else if (controller == "salesbill" && allowedList.Contains("salesbill")) isAllowed = true;
                else if (controller == "salesreport")
                {
                    if (action == "customerproduct" || action == "exportcustomerproductexcel" || action == "exportcustomerproductcsv")
                    {
                        var pos = user.FindFirst("Position")?.Value ?? "";
                        if (allowedList.Contains("customerproduct") || allowedList.Contains("salesbill") || pos.Contains("ผู้แทน") || pos.Contains("พนักงานขาย"))
                        {
                            isAllowed = true;
                        }
                    }
                    else if (action == "customerpurchasesummary")
                    {
                        if (allowedList.Contains("customerpurchasesummary")) isAllowed = true;
                    }
                    else if (allowedList.Contains("salesreport") || allowedList.Contains("report"))
                    {
                        isAllowed = true;
                    }
                }
                else if (controller == "report")
                {
                    if (action == "waitinggoods" && (allowedList.Contains("waitinggoods") || allowedList.Contains("waitinggoodsreport"))) isAllowed = true;
                    else if (action == "returnnotes" && allowedList.Contains("returnnotes")) isAllowed = true;
                    else if (allowedList.Contains("report") || allowedList.Contains("salesreport")) isAllowed = true;
                }
                else if (controller == "debtor")
                {
                    if (action == "cancelled" && (allowedList.Contains("cancelled") || allowedList.Contains("debtorcancelled"))) isAllowed = true;
                    else if (action == "history" && (allowedList.Contains("debtorhistory") || allowedList.Contains("history"))) isAllowed = true;
                    else if (action == "baddebt" && allowedList.Contains("baddebt")) isAllowed = true;
                    else if (action == "installment" && allowedList.Contains("installment")) isAllowed = true;
                    else if (allowedList.Contains("debtor")) isAllowed = true;
                }
                else if (controller == "debtorcard" && allowedList.Contains("debtor")) isAllowed = true;
                else if (controller == "audit" && allowedList.Contains("audit")) isAllowed = true;
                else if (controller == "upload" && allowedList.Contains("upload")) isAllowed = true;
                else if ((controller == "users" || controller == "usermanagement" || (controller == "account" && (action == "users" || action == "createuser" || action == "edituser"))) && allowedList.Contains("users")) isAllowed = true;

                if (!isAllowed)
                {
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }

            await next();
        }
    }
}