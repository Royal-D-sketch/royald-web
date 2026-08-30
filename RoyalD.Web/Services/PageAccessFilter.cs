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
                if (user.IsInRole("admin"))
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

                if (role == "admin")
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
                else if (controller == "debtor" && action == "cancelled" && (allowedList.Contains("cancelled") || allowedList.Contains("debtorcancelled"))) isAllowed = true;
                else if (controller == "debtor" && action == "history" && (allowedList.Contains("debtorhistory") || allowedList.Contains("history"))) isAllowed = true;
                else if (controller == "debtor" && action != "history" && action != "cancelled" && allowedList.Contains("debtor")) isAllowed = true;
                else if (controller == "debtorcard" && allowedList.Contains("debtor")) isAllowed = true;
                else if (controller == "report" && action == "waitinggoods" && allowedList.Contains("waitinggoods")) isAllowed = true;
                else if ((controller == "salesreport" || (controller == "report" && action != "waitinggoods")) && (allowedList.Contains("salesreport") || allowedList.Contains("report"))) isAllowed = true;
                else if (controller == "audit" && allowedList.Contains("audit")) isAllowed = true;
                else if (controller == "upload" && allowedList.Contains("upload")) isAllowed = true;

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