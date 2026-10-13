using RoyalD.Web;
using RoyalD.Web.Models;
using RoyalD.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Npgsql.EntityFrameworkCore.PostgreSQL;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100MB
});

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ========== Services ==========
Console.OutputEncoding = System.Text.Encoding.UTF8;

builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
builder.Services.AddSingleton(JavaScriptEncoder.Create(UnicodeRanges.All));
builder.Services.AddSingleton(UrlEncoder.Create(UnicodeRanges.All));
builder.Services.AddMemoryCache();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(x =>
{
    x.ValueLengthLimit = 104857600;
    x.MultipartBodyLengthLimit = 104857600;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<PageAccessFilter>();
})
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        o.JsonSerializerOptions.Converters.Add(new GlobalDateConverter());
    });

// ========== Database Provider Selection (PostgreSQL on Cloud / SQLite for local) ==========
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SQLite";
var pgConn = Environment.GetEnvironmentVariable("DATABASE_URL") 
             ?? Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION")
             ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(pgConn))
{
    pgConn = PostgreSqlConnectionStringParser.Parse(pgConn);
}

bool usePostgres = dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) 
                  || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))
                  || Environment.GetEnvironmentVariable("USE_POSTGRES") == "true";

if (usePostgres && !string.IsNullOrEmpty(pgConn))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(pgConn, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(60);
        }));
}
else
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "..", "royald.db");
    if (!File.Exists(dbPath))
    {
        dbPath = Path.Combine(builder.Environment.ContentRootPath, "royald.db");
    }
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}

// DI Services
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<DebtorService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<RoyalD.Web.Services.PermissionService>();
builder.Services.AddHostedService<ReportPrewarmBackgroundService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<RoyalD.Web.Services.SupabaseStorageService>();

// Authentication Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.MaxAge = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ========== Middleware ==========
app.UseDeveloperExceptionPage();
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto });

// Force Thai locale dd/MM/yyyy globally
var thCulture = new System.Globalization.CultureInfo("th-TH");
thCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
thCulture.DateTimeFormat.LongDatePattern = "dd MMMM yyyy";
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = thCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = thCulture;

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "users",
    pattern: "Users/{action=Users}/{id?}",
    defaults: new { controller = "Account", action = "Users" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ========== DB Init & Schema Migration ==========
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (creator != null)
        {
            try { creator.CreateTables(); } catch { }
        }
        else
        {
            db.Database.EnsureCreated();
        }
    }
    catch { try { db.Database.EnsureCreated(); } catch { } }

    // Helper: check/add columns for SQLite backward compat
    if (db.Database.IsSqlite())
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();

        bool ColumnExists(string table, string column)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var r = cmd.ExecuteReader();
            while (r.Read()) if (r.GetString(1) == column) return true;
            return false;
        }

        void AddColumnIfMissing(string table, string column, string colDef)
        {
            if (!ColumnExists(table, column))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {colDef}";
                cmd.ExecuteNonQuery();
            }
        }

                AddColumnIfMissing("Users", "Position", "TEXT NOT NULL DEFAULT 'เธเธนเนเนเธ—เธเธเธฒเธข'");
        AddColumnIfMissing("Users", "CanViewPaymentDetails", "INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing("Users", "CanChangeDebtStatus", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Users", "CanChangePaidBillStatus", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Users", "CanCancelBill", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Users", "CanManageReturnedBills", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Users", "CanDeleteSalesBill", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Users", "CanDeleteDebtor", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Users", "SalesRepCode", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing("Users", "SessionTimeoutMinutes", "INTEGER NULL DEFAULT 10");
        AddColumnIfMissing("Users", "AllowedPages", "TEXT NOT NULL DEFAULT 'Dashboard,SalesBill,Debtor,DebtorHistory,SalesReport'");
        AddColumnIfMissing("Users", "CanDownload", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("Users", "CanScreenCapture", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("SalesBills", "PoNumber", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing("OutstandingDebts", "ReceiptNo", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing("OutstandingDebts", "ReceiptDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "PoNumber", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing("OutstandingDebts", "FullyPaidDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "BadDebtAmount", "decimal(18,2) NULL");
        AddColumnIfMissing("OutstandingDebts", "BadDebtDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "DeliveringDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "IsLocked", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("OutstandingDebts", "IsReturnCutFromBill", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("OutstandingDebts", "PostponedDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "ReturnAmount", "decimal(18,2) NULL");
        AddColumnIfMissing("OutstandingDebts", "WaitingGoodsDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "CancelledDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "CancelledBy", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "CancelReason", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "LastEditedBy", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing("OutstandingDebts", "LastEditedDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "ReturnedToAccountDate", "TEXT NULL");
        AddColumnIfMissing("OutstandingDebts", "ReturnedToAccountReason", "TEXT NULL DEFAULT ''");
        AddColumnIfMissing("OutstandingDebts", "ReturnedToDeliveryDate", "TEXT NULL");
        AddColumnIfMissing("SalesBills", "IsCancelled", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("SalesBills", "CancelledDate", "TEXT NULL");
        AddColumnIfMissing("SalesBills", "CancelledBy", "TEXT NULL DEFAULT ''");
        AddColumnIfMissing("SalesBills", "CancelledReason", "TEXT NULL DEFAULT ''");

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_OutstandingDebts_BillNo ON OutstandingDebts (BillNo);
                CREATE INDEX IF NOT EXISTS IX_OutstandingDebts_CustomerCode ON OutstandingDebts (CustomerCode);
                CREATE INDEX IF NOT EXISTS IX_OutstandingDebts_DueDate ON OutstandingDebts (DueDate);
                CREATE INDEX IF NOT EXISTS IX_SalesBills_CustomerCode ON SalesBills (CustomerCode);
                CREATE INDEX IF NOT EXISTS IX_SalesBills_IsFullyPaid ON SalesBills (IsFullyPaid);
                CREATE INDEX IF NOT EXISTS IX_SalesBills_IsCancelled ON SalesBills (IsCancelled);
            ";
            cmd.ExecuteNonQuery();
        }
    }

    if (db.Database.IsNpgsql())
    {
        try
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();

            string[] alterStatements = new[]
            {
                @"ALTER TABLE ""OutstandingDebts"" DROP CONSTRAINT IF EXISTS ""FK_OutstandingDebts_Customers_CustomerCode""",
                @"ALTER TABLE ""SalesBills"" DROP CONSTRAINT IF EXISTS ""FK_SalesBills_Customers_CustomerCode""",
                @"ALTER TABLE ""PaymentRecords"" DROP CONSTRAINT IF EXISTS ""FK_PaymentRecords_OutstandingDebts_OutstandingDebtId""",
                @"ALTER TABLE ""PendingProducts"" DROP CONSTRAINT IF EXISTS ""FK_PendingProducts_OutstandingDebts_OutstandingDebtId""",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Position"" varchar(100) DEFAULT 'ผู้แทนขาย'",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CanViewPaymentDetails"" boolean DEFAULT true",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CanChangeDebtStatus"" boolean DEFAULT false",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CanChangePaidBillStatus"" boolean DEFAULT false",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CanCancelBill"" boolean DEFAULT false",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CanManageReturnedBills"" boolean DEFAULT false",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CanDeleteSalesBill"" boolean DEFAULT false",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CanDeleteDebtor"" boolean DEFAULT false",
                @"ALTER TABLE ""Users"" ALTER COLUMN ""Position"" TYPE varchar(100)",
                @"ALTER TABLE ""Users"" ALTER COLUMN ""SalesRepCode"" TYPE varchar(2000)",
                @"ALTER TABLE ""Users"" ALTER COLUMN ""AllowedRegion"" TYPE varchar(500)",
                @"ALTER TABLE ""Users"" ALTER COLUMN ""AllowedProvinces"" TYPE varchar(2000)",
                @"ALTER TABLE ""Users"" ALTER COLUMN ""AllowedDistricts"" TYPE varchar(5000)",
                @"ALTER TABLE ""Users"" ALTER COLUMN ""AllowedPages"" TYPE varchar(1000)",
                @"ALTER TABLE ""AuditLogs"" ALTER COLUMN ""IPAddress"" TYPE varchar(200)",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""PoNumber"" varchar(50) DEFAULT ''",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""ReceiptNo"" varchar(50) DEFAULT ''",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""ReceiptDate"" timestamp with time zone NULL",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""IsFullyPaid"" boolean DEFAULT false",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""IsCancelled"" boolean DEFAULT false",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""CancelledDate"" timestamp with time zone NULL",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""CancelledBy"" varchar(100) NULL",
                @"ALTER TABLE ""SalesBills"" ADD COLUMN IF NOT EXISTS ""CancelledReason"" varchar(500) NULL",
                @"CREATE INDEX IF NOT EXISTS ""IX_SalesBills_IsCancelled"" ON ""SalesBills"" (""IsCancelled"")",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""ReceiptNo"" varchar(50) DEFAULT ''",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""ReceiptDate"" timestamp with time zone NULL",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""PoNumber"" varchar(50) DEFAULT ''",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""CancelledDate"" timestamp with time zone NULL",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""CancelledBy"" varchar(100) NULL",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""CancelReason"" varchar(500) NULL",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""LastEditedBy"" varchar(100) DEFAULT ''",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""LastEditedDate"" timestamp with time zone NULL",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""ReturnedToAccountDate"" timestamp with time zone NULL",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""ReturnedToAccountReason"" varchar(500) NULL",
                @"ALTER TABLE ""OutstandingDebts"" ADD COLUMN IF NOT EXISTS ""ReturnedToDeliveryDate"" timestamp with time zone NULL",
                @"CREATE INDEX IF NOT EXISTS ""IX_OutstandingDebts_BillNo"" ON ""OutstandingDebts"" (""BillNo"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_OutstandingDebts_CustomerCode"" ON ""OutstandingDebts"" (""CustomerCode"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_OutstandingDebts_DueDate"" ON ""OutstandingDebts"" (""DueDate"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_OutstandingDebts_BillDate"" ON ""OutstandingDebts"" (""BillDate"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_OutstandingDebts_Province"" ON ""OutstandingDebts"" (""Province"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_SalesBills_CustomerCode"" ON ""SalesBills"" (""CustomerCode"")",
                @"CREATE INDEX IF NOT EXISTS ""IX_SalesBills_IsFullyPaid"" ON ""SalesBills"" (""IsFullyPaid"")"
            };

            foreach (var stmt in alterStatements)
            {
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = stmt;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception sqlEx)
                {
                    Console.WriteLine($"Notice applying migration: {stmt} -> {sqlEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Notice applying postgres migrations: " + ex.Message);
        }
    }

    // Ensure admin user exists with full privileges
    var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
    if (admin == null)
    {
        db.Users.Add(new AppUser
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin1234"),
            FullName = "เธเธนเนเธ”เธนเนเธฅเธฃเธฐเธเธ",
            Role = "admin",
            IsActive = true,
            SessionTimeoutMinutes = null,
            CanDownload = true,
            CanScreenCapture = true,
            CanChangeDebtStatus = true,
            CanChangePaidBillStatus = true,
            CanCancelBill = true,
            CanManageReturnedBills = true,
            CanDeleteSalesBill = true,
            CanDeleteDebtor = true,
            AllowedPages = "Dashboard,SalesBill,Debtor,DebtorCancelled,DebtorHistory,SalesReport,Audit,Users,Upload"
        });
        db.SaveChanges();
    }
    else
    {
        admin.CanDownload = true;
        admin.CanScreenCapture = true;
        admin.CanChangeDebtStatus = true;
        admin.CanChangePaidBillStatus = true;
        admin.CanCancelBill = true;
        admin.CanManageReturnedBills = true;
        admin.CanDeleteSalesBill = true;
        admin.CanDeleteDebtor = true;
        admin.AllowedPages = "Dashboard,SalesBill,Debtor,DebtorCancelled,DebtorHistory,SalesReport,Audit,Users,Upload";
        admin.SessionTimeoutMinutes = null;
        db.SaveChanges();
    }

    // Auto import removed to maintain clean slate database

    // Auto-fix / sync Credit from OutstandingDebt to SalesBills if SalesBills has 0 or shifted credit
    try
    {
        var billsToFix = db.SalesBills.Where(b => b.Credit == 0 || (b.Phone != null && b.Phone.Length <= 3)).ToList();
        if (billsToFix.Any())
        {
            var debtCredits = db.OutstandingDebts.Where(d => d.Credit > 0)
                                                 .GroupBy(d => d.BillNo)
                                                 .ToDictionary(g => g.Key, g => g.First().Credit);
            foreach (var b in billsToFix)
            {
                if (debtCredits.TryGetValue(b.BillNo, out int cred) && cred > 0)
                {
                    b.Credit = cred;
                }
                if (!string.IsNullOrEmpty(b.Phone) && int.TryParse(b.Phone, out int fakePh) && fakePh <= 365 && b.Phone.Length <= 3)
                {
                    if (b.Credit == 0) b.Credit = fakePh;
                    b.Phone = "";
                }
            }
            db.SaveChanges();
        }
    }
    catch { }

    // Auto-sync IsFullyPaid and IsCancelled on SalesBills based on Cut-off and Receipt rules
    try
    {
        if (db.Database.IsNpgsql())
        {
            string syncSql = @"
                -- 1. บิลที่การ์ดลูกหนี้ระบุเป็นยกเลิก (Status = 14) ให้ปรับบิลขายเป็น IsCancelled = true และ IsFullyPaid = false
                UPDATE ""SalesBills"" b
                SET ""IsCancelled"" = true, ""IsFullyPaid"" = false
                WHERE EXISTS (
                    SELECT 1 FROM ""OutstandingDebts"" d
                    WHERE d.""BillNo"" = b.""BillNo""
                    AND d.""Status"" = 14
                );

                -- 2. บิลที่ถูกยกเลิก ต้องไม่เป็น IsFullyPaid
                UPDATE ""SalesBills""
                SET ""IsFullyPaid"" = false
                WHERE ""IsCancelled"" = true AND ""IsFullyPaid"" = true;

                -- 3. บิลเก่าปี 2568 และก่อนหน้า (< 2026-01-01): 
                -- ยึดยอดยกมาในอดีต ถ้าไม่มียอดค้างใน OutstandingDebts และไม่ยกเลิก ให้ถือว่ารับชำระแล้ว
                UPDATE ""SalesBills"" b
                SET ""IsFullyPaid"" = true
                WHERE ""IsFullyPaid"" = false
                AND ""IsCancelled"" = false
                AND b.""BillDate"" < '2026-01-01'
                AND NOT EXISTS (
                    SELECT 1 FROM ""OutstandingDebts"" d
                    WHERE d.""BillNo"" = b.""BillNo""
                    AND d.""Status"" NOT IN (1, 2, 3, 14)
                    AND (d.""RemainingAmount"" > 0 OR d.""Status"" = 4 OR d.""Status"" = 100)
                    AND d.""FullyPaidDate"" IS NULL
                );

                -- 4. บิลปัจจุบันปี 2569 เป็นต้นไป (>= 2026-01-01):
                -- ต้องมีเลขที่ใบเสร็จรับเงิน หรือมีการ์ดลูกหนี้ที่มีสถานะชำระแล้ว (1, 2, 3) เท่านั้น จึงจะถือว่า IsFullyPaid = true
                UPDATE ""SalesBills"" b
                SET ""IsFullyPaid"" = true
                WHERE ""IsFullyPaid"" = false
                AND ""IsCancelled"" = false
                AND b.""BillDate"" >= '2026-01-01'
                AND (
                    (b.""ReceiptNo"" IS NOT NULL AND b.""ReceiptNo"" != '')
                    OR EXISTS (
                        SELECT 1 FROM ""OutstandingDebts"" d
                        WHERE d.""BillNo"" = b.""BillNo""
                        AND (d.""Status"" IN (1, 2, 3) OR (d.""RemainingAmount"" <= 0 AND d.""FullyPaidDate"" IS NOT NULL))
                    )
                );

                -- 5. บิลปี 2569 ที่เคยถูกปรับ IsFullyPaid = true อัตโนมัติผิดพลาด (ไม่มีเลขใบเสร็จ และไม่มีลูกหนี้รับชำระ)
                -- ให้ปลดออกจากการรับเงิน เพื่อรอการตรวจสอบหรือลงบิลยกเลิก
                UPDATE ""SalesBills"" b
                SET ""IsFullyPaid"" = false
                WHERE ""IsFullyPaid"" = true
                AND b.""BillDate"" >= '2026-01-01'
                AND (b.""ReceiptNo"" IS NULL OR b.""ReceiptNo"" = '')
                AND NOT EXISTS (
                    SELECT 1 FROM ""OutstandingDebts"" d
                    WHERE d.""BillNo"" = b.""BillNo""
                    AND (d.""Status"" IN (1, 2, 3) OR (d.""RemainingAmount"" <= 0 AND d.""FullyPaidDate"" IS NOT NULL))
                );

                -- 6. หากมีลูกหนี้คงเหลือที่ยังค้างอยู่ ต้องปลด IsFullyPaid = false เสมอ
                UPDATE ""SalesBills"" b
                SET ""IsFullyPaid"" = false
                WHERE ""IsFullyPaid"" = true
                AND EXISTS (
                    SELECT 1 FROM ""OutstandingDebts"" d
                    WHERE d.""BillNo"" = b.""BillNo""
                    AND d.""Status"" NOT IN (1, 2, 3, 14)
                    AND (d.""RemainingAmount"" > 0 OR d.""Status"" = 4 OR d.""Status"" = 100)
                    AND d.""FullyPaidDate"" IS NULL
                );
            ";
            db.Database.ExecuteSqlRaw(syncSql);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Notice syncing IsFullyPaid: " + ex.Message);
    }
}


app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();


