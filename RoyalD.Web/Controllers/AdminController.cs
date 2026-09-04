using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;
using System.Linq;

namespace RoyalD.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public AdminController(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Permissions()
        {
            if (!User.IsInRole("admin") && User.Identity?.Name?.ToLower() != "admin")
                return Forbid();

            var users = await _db.Users.OrderBy(u => u.Username).ToListAsync();
            var permissions = await _db.UserMenuPermissions.ToListAsync();

            ViewBag.Permissions = permissions;
            ViewBag.Menus = new Dictionary<string, string>
            {
                { "DebtorList", "หน้ารายการลูกหนี้" },
                { "SalesBillList", "หน้ารายการบิลขาย" },
                { "CustomerSalesReport", "รายงานการขายรายลูกค้า" },
                { "SalesRepReport", "รายงานสรุปยอดขายพนักงาน" },
                { "AnnualPerformance", "รายงานสรุปยอดผู้แทนขายประจำปี" },
                { "WaitingGoodsReport", "รายการสินค้ารอส่งมอบ" },
                { "AuditLogs", "ประวัติการใช้งานระบบ" },
                { "Upload", "อัปโหลดข้อมูล (Excel)" }
            };

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePermissions([FromBody] UpdatePermissionRequest req)
        {
            if (!User.IsInRole("admin") && User.Identity?.Name?.ToLower() != "admin")
                return Unauthorized();

            var existing = await _db.UserMenuPermissions.FirstOrDefaultAsync(p => p.Username == req.Username && p.MenuKey == req.MenuKey);
            if (existing != null)
            {
                existing.IsAllowed = req.IsAllowed;
            }
            else
            {
                _db.UserMenuPermissions.Add(new UserMenuPermission
                {
                    Username = req.Username,
                    MenuKey = req.MenuKey,
                    IsAllowed = req.IsAllowed
                });
            }

            await _db.SaveChangesAsync();
            _cache.Remove("UserPerm_{req.Username}_{req.MenuKey}");

            return Ok(new { success = true });
        }
    }

    public class UpdatePermissionRequest
    {
        public string Username { get; set; } = string.Empty;
        public string MenuKey { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
    }
}
