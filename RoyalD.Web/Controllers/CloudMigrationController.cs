using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class CloudMigrationController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CloudMigrationController> _logger;

        public CloudMigrationController(AppDbContext db, ILogger<CloudMigrationController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.TotalBills = _db.SalesBills.Count();
            ViewBag.TotalDebts = _db.OutstandingDebts.Count();
            ViewBag.TotalUsers = _db.Users.Count();
            ViewBag.TotalCustomers = _db.Customers.Count();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MigrateToPostgres(string? connectionString, string? dbHost, int? dbPort, string? dbUser, string? dbPassword, string? dbName)
        {
            string finalConn = "";
            if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbPassword))
            {
                int port = dbPort ?? 5432;
                string user = string.IsNullOrWhiteSpace(dbUser) ? "postgres" : dbUser.Trim();
                string db = string.IsNullOrWhiteSpace(dbName) ? "postgres" : dbName.Trim();
                finalConn = $"Host={dbHost.Trim()};Port={port};Database={db};Username={user};Password={dbPassword.Trim()};Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;";
            }
            else if (!string.IsNullOrWhiteSpace(connectionString))
            {
                finalConn = PostgreSqlConnectionStringParser.Parse(connectionString);
            }
            else
            {
                TempData["Error"] = "กรุณากรอกข้อมูลการเชื่อมต่อ Cloud PostgreSQL";
                return RedirectToAction("Index");
            }

            try
            {
                // สร้าง DbContext สำหรับต่อ Cloud PostgreSQL
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseNpgsql(finalConn, o =>
                {
                    o.CommandTimeout(180);
                    o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });

                using var cloudDb = new AppDbContext(optionsBuilder.Options);
                await SyncJob.Run(_db, cloudDb);

                TempData["Success"] = "🎉 โอนย้ายข้อมูลขึ้น Cloud PostgreSQL สำเร็จสมบูรณ์ 100%!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating to Cloud PostgreSQL");
                TempData["Error"] = "การเชื่อมต่อหรือโอนย้ายข้อมูลล้มเหลว: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
