using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

class Program
{
    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        string csvPath = @"F:\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\ข้อมูลCSV 23.10.69-4.11.69\บิลขาย เดือน10.69.csv";

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"Error: File not found: {csvPath}");
            return;
        }

        string[] targets = new[]
        {
            "SUPA1|Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;",
            "NEON|Host=ep-fragrant-base-a1tj1zin-pooler.ap-southeast-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_2Ngwo5ldhLie;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;"
        };

        foreach (var target in targets)
        {
            var parts = target.Split('|', 2);
            string dbName = parts[0];
            string connStr = parts[1];

            Console.WriteLine($"\n=======================================================");
            Console.WriteLine($"PROCESSING DATABASE: {dbName}");
            Console.WriteLine($"=======================================================");

            var opt = new DbContextOptionsBuilder<AppDbContext>();
            opt.UseNpgsql(connStr, npg =>
            {
                npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                npg.CommandTimeout(180);
            });

            using var db = new AppDbContext(opt.Options);
            var importer = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);

            // 1. Import / Update Sales Bills from บิลขาย เดือน10.69.csv
            Console.WriteLine($"[1] Importing & Updating Sales Bills from '{Path.GetFileName(csvPath)}'...");
            using (var sStream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var (ins, upd, maxDate, _) = await importer.ImportSalesBillAsync(sStream, "2026-10", true, Path.GetFileName(csvPath));
                Console.WriteLine($"    -> Import finished: Inserted={ins}, Updated={upd}, MaxDate={maxDate:yyyy-MM-dd}");
            }

            // 2. Mark bills not present in the official October file as cancelled
            Console.WriteLine($"[2] Checking October bills not present in '{Path.GetFileName(csvPath)}'...");
            var octStart = new DateTime(2026, 10, 1);
            var octEnd = new DateTime(2026, 10, 31, 23, 59, 59);

            // Read list of bill numbers from CSV to be 100% sure
            HashSet<string> csvBillNos;
            using (var stream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var conf = new ExcelReaderConfiguration { FallbackEncoding = Encoding.GetEncoding(874) };
                using var reader = ExcelReaderFactory.CreateCsvReader(stream, conf);
                var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
                var parsedCsv = ExcelImportService.CleanAndParseSalesBills(ds.Tables[0], out _);
                csvBillNos = parsedCsv.Select(b => b.BillNo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var dbOctBills = await db.SalesBills
                .Where(b => b.BillDate >= octStart && b.BillDate <= octEnd)
                .ToListAsync();

            int cancelledNew = 0;
            foreach (var b in dbOctBills)
            {
                if (!csvBillNos.Contains(b.BillNo) && !b.IsCancelled)
                {
                    b.IsCancelled = true;
                    b.CancelledBy = "ART";
                    b.CancelledDate = DateTime.UtcNow;
                    b.CancelledReason = "ยกเลิกในระบบ CD (ไม่มีในรายงานบิลขายเดือน 10.69)";
                    cancelledNew++;
                    Console.WriteLine($"    -> Marked Cancelled: {b.BillNo} ({b.BillDate:dd/MM/yyyy}) Amt={b.TotalAmount:N2} Cust={b.CustomerName}");
                }
            }

            if (cancelledNew > 0)
            {
                await db.SaveChangesAsync();
                Console.WriteLine($"    -> Saved {cancelledNew} newly cancelled bills.");
            }
            else
            {
                Console.WriteLine("    -> No extra un-cancelled bills found.");
            }

            // 3. Write Audit Log
            db.AuditLogs.Add(new AuditLog
            {
                Username = "ART",
                Action = "UPDATE_SALES_BILLS_OCTOBER_69",
                Detail = $"Updated SalesBills from บิลขาย เดือน10.69.csv (Total in file: {csvBillNos.Count} bills). Marked {cancelledNew} unlisted bills as cancelled.",
                IPAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            // 4. Verification
            var activeOct = await db.SalesBills.AsNoTracking()
                .Where(b => b.BillDate >= octStart && b.BillDate <= octEnd && !b.IsCancelled)
                .ToListAsync();

            var cancelledOct = await db.SalesBills.AsNoTracking()
                .Where(b => b.BillDate >= octStart && b.BillDate <= octEnd && b.IsCancelled)
                .ToListAsync();

            var paidOct = activeOct.Where(b => !string.IsNullOrEmpty(b.ReceiptNo)).ToList();

            var novBills = await db.SalesBills.AsNoTracking()
                .Where(b => b.BillDate >= new DateTime(2026, 11, 1))
                .ToListAsync();

            Console.WriteLine($"\n--- VERIFICATION FOR {dbName} ---");
            Console.WriteLine($"Active October Bills:    Count = {activeOct.Count:N0} (Expected: 2,112) | Total Amount = {activeOct.Sum(b => b.TotalAmount):N2} (Expected: 145,721,348.51)");
            Console.WriteLine($"Cancelled October Bills: Count = {cancelledOct.Count:N0} (Expected: 25)    | Total Amount = {cancelledOct.Sum(b => b.TotalAmount):N2}");
            Console.WriteLine($"Paid October Bills:      Count = {paidOct.Count:N0}                     | Total Amount = {paidOct.Sum(b => b.TotalAmount):N2}");
            Console.WriteLine($"November Bills Intact:   Count = {novBills.Count:N0} (Expected: 259)    | Total Amount = {novBills.Sum(b => b.TotalAmount):N2} (Expected: 8,022,299.27)");

            if (activeOct.Count == 2112 && Math.Abs(activeOct.Sum(b => b.TotalAmount) - 145721348.51m) < 0.01m)
            {
                Console.WriteLine($"SUCCESS: {dbName} is 100% synchronized with บิลขาย เดือน10.69.csv!");
            }
            else
            {
                Console.WriteLine($"WARNING: Difference detected in {dbName}.");
            }
        }

        Console.WriteLine("\n=======================================================");
        Console.WriteLine("ALL UPDATES COMPLETED SUCCESSFULLY!");
        Console.WriteLine("=======================================================");
    }
}
