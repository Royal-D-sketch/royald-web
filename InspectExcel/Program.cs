using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

class Program {
    static async System.Threading.Tasks.Task Main() {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;

        string dbPath = @"C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\royald.db";
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        using var db = new AppDbContext(opt);

        var importer = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);
        string baseDir = @"C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน";

        // 1. Check Clear state
        Console.WriteLine($"Current DB -> Bills: {await db.SalesBills.CountAsync()}, Debts: {await db.OutstandingDebts.CountAsync()}");

        // 2. Test Import Sales Bills
        string billFile = Path.Combine(baseDir, @"บิลขาย 2026\รายละเอียดบิลขาย วันที่ 1-24.9.2026.XLS");
        if (File.Exists(billFile)) {
            using var stream = File.OpenRead(billFile);
            var (ins, upd, maxDate, preview) = await importer.ImportSalesBillAsync(stream, "2026-09", true, Path.GetFileName(billFile));
            Console.WriteLine($"Sales Bills Imported: {ins} new, {upd} updated | Latest Date: {maxDate:dd/MM/yyyy}");
            Console.WriteLine($"Preview items: {preview.Count} rows");
        }

        // 3. Test Import Debtors
        string debtFile = Path.Combine(baseDir, @"ลูกหนี้ค้าชำระ 25.9.2026\ลูกหนี้ค้าชำระ 25.9.2026.XLS");
        if (File.Exists(debtFile)) {
            using var stream = File.OpenRead(debtFile);
            var (cnt, maxDate, preview) = await importer.ImportOutstandingDebtsAsync(stream, Path.GetFileName(debtFile));
            Console.WriteLine($"Debtors Imported: {cnt} (100% overwrite) | Latest Date: {maxDate:dd/MM/yyyy}");
            Console.WriteLine($"Preview rows: {preview.Count} rows");
        }

        // 4. Test Receipts Match
        string receiptFile = Path.Combine(baseDir, @"สรุปการชำระเงินตามบิลใบเสร็จรับเงิน\สรุปการชำระเงินตามบิลใบเสร็จรับเงิน.xlsx");
        if (File.Exists(receiptFile)) {
            using var stream = File.OpenRead(receiptFile);
            var preview = await importer.PreviewReceiptMatchAsync(stream, Path.GetFileName(receiptFile));
            var (matched, notFound, maxDate) = await importer.ConfirmReceiptMatchAsync(preview.PreviewId, updateDuplicates: true);
            Console.WriteLine($"Receipts Matched: {matched} (not found: {notFound}) | Latest Date: {maxDate:dd/MM/yyyy}");
        }

        // 5. Query final DB state and latest dates
        var latestB = await db.SalesBills.OrderByDescending(b => b.BillDate).Select(b => (DateTime?)b.BillDate).FirstOrDefaultAsync();
        var latestD = await db.OutstandingDebts.OrderByDescending(d => d.BillDate).Select(d => (DateTime?)d.BillDate).FirstOrDefaultAsync();
        var latestR = await db.SalesBills.Where(b => b.ReceiptDate != null).OrderByDescending(b => b.ReceiptDate).Select(b => (DateTime?)b.ReceiptDate).FirstOrDefaultAsync();

        Console.WriteLine("\n=== Final Verification ===");
        Console.WriteLine($"Total Sales Bills: {await db.SalesBills.CountAsync()} | ข้อมูลบิลขายล่าสุดในระบบ ณ วันที่: {latestB:dd/MM/yyyy}");
        Console.WriteLine($"Total Debtors: {await db.OutstandingDebts.CountAsync()} | ระบบได้อัปเดตข้อมูลลูกหนี้ค้างชำระล่าสุด ณ วันที่: {latestD:dd/MM/yyyy} เรียบร้อยแล้ว");
        Console.WriteLine($"Total Paid Bills: {await db.SalesBills.CountAsync(b => !string.IsNullOrEmpty(b.ReceiptNo))} | ข้อมูลใบเสร็จรับเงินล่าสุดในระบบ ณ วันที่: {latestR:dd/MM/yyyy}");
    }
}
