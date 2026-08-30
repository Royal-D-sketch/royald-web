using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class AuditController : Controller
    {
        private readonly AppDbContext _db;
        public AuditController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 50;
            var q = _db.AuditLogs.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(a => a.Username.Contains(search) || a.Action.Contains(search) || a.Detail.Contains(search) || a.Area.Contains(search));

            var total = await q.CountAsync();
            var logs = await q.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            // Load Users dictionary for FullName and Role
            var users = await _db.Users.ToDictionaryAsync(u => u.Username, u => u);
            ViewBag.UserDict = users;

            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount = total;
            return View(logs);
        }
    }
}
