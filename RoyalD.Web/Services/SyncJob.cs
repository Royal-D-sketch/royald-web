using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using RoyalD.Web.Models;

public class SyncJob
{
    public static async Task Run(AppDbContext localDb, AppDbContext cloudDb)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        Console.WriteLine("==========================================================");
        Console.WriteLine("🚀 STARTING MIGRATION TO SUPABASE (SESSION POOLER PORT 5432)");
        Console.WriteLine("==========================================================");

        cloudDb.ChangeTracker.AutoDetectChangesEnabled = false;
        cloudDb.Database.SetCommandTimeout(300);

        // 1. Create Schema / Tables
        Console.WriteLine("[1/5] Ensuring Tables Exist in Supabase...");
        try
        {
            var script = cloudDb.Database.GenerateCreateScript();
            if (!string.IsNullOrWhiteSpace(script))
            {
                await cloudDb.Database.ExecuteSqlRawAsync(script);
                Console.WriteLine("? Created all application tables in Supabase!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("?? Schema notice: " + ex.Message);
            try
            {
                var creator = cloudDb.GetService<IRelationalDatabaseCreator>();
                if (creator != null) await creator.CreateTablesAsync();
            }
            catch {}
        }

        // 2. Load Local Data
        Console.WriteLine("\n[2/5] Reading data from local database...");
        var users = await localDb.Users.AsNoTracking().ToListAsync();
        var customers = await localDb.Customers.AsNoTracking().ToListAsync();
        var debts = await localDb.OutstandingDebts.AsNoTracking().ToListAsync();
        var bills = await localDb.SalesBills.AsNoTracking().ToListAsync();
        var billItems = await localDb.SalesBillItems.AsNoTracking().ToListAsync();
        var payments = await localDb.PaymentRecords.AsNoTracking().ToListAsync();
        var auditLogs = await localDb.AuditLogs.AsNoTracking().ToListAsync();

        Console.WriteLine($"   - Users: {users.Count:N0}");
        Console.WriteLine($"   - Customers: {customers.Count:N0}");
        Console.WriteLine($"   - SalesBills: {bills.Count:N0}");
        Console.WriteLine($"   - SalesBillItems: {billItems.Count:N0}");
        Console.WriteLine($"   - OutstandingDebts: {debts.Count:N0}");

        // 3. Sync Users & Customers
        Console.WriteLine("\n[3/5] Syncing Users & Customers...");
        var existingUsernames = new HashSet<string>(await cloudDb.Users.Select(x => x.Username).ToListAsync());
        var newUsers = users.Where(u => !existingUsernames.Contains(u.Username)).Select(u => { u.Id = 0; return u; }).ToList();
        if (newUsers.Any())
        {
            cloudDb.Users.AddRange(newUsers);
            await cloudDb.SaveChangesAsync();
            Console.WriteLine($"   ? Synced {newUsers.Count:N0} Users.");
        }

        var existingCust = new HashSet<string>(await cloudDb.Customers.Select(x => x.CustomerCode).ToListAsync());
        var newCust = customers.Where(c => !existingCust.Contains(c.CustomerCode)).ToList();
        if (newCust.Any())
        {
            cloudDb.Customers.AddRange(newCust);
            await cloudDb.SaveChangesAsync();
            Console.WriteLine($"   ? Synced {newCust.Count:N0} Customers.");
        }

        // 4. Sync SalesBills & Items
        Console.WriteLine("\n[4/5] Syncing SalesBills & Items...");
        var existingBills = new HashSet<string>(await cloudDb.SalesBills.Select(x => x.BillNo).ToListAsync());
        var newBills = bills.Where(b => !existingBills.Contains(b.BillNo)).Select(b => { b.Items = new List<SalesBillItem>(); return b; }).ToList();
        
        int billCount = 0;
        foreach (var chunk in newBills.Chunk(1000))
        {
            cloudDb.SalesBills.AddRange(chunk);
            await cloudDb.SaveChangesAsync();
            billCount += chunk.Length;
            Console.WriteLine($"   -> Transferred {billCount:N0} / {newBills.Count:N0} Sales Bills...");
        }

        var existingItemCount = await cloudDb.SalesBillItems.CountAsync();
        if (existingItemCount == 0 && billItems.Any())
        {
            int itemCount = 0;
            foreach (var chunk in billItems.Select(i => { i.Id = 0; return i; }).Chunk(2000))
            {
                cloudDb.SalesBillItems.AddRange(chunk);
                await cloudDb.SaveChangesAsync();
                itemCount += chunk.Length;
                Console.WriteLine($"   -> Transferred {itemCount:N0} / {billItems.Count:N0} Bill Items...");
            }
        }

        // 5. Sync OutstandingDebts & Payments
        Console.WriteLine("\n[5/5] Syncing Outstanding Debts & Payments...");
        var existingDebts = new HashSet<string>(await cloudDb.OutstandingDebts.Select(x => x.BillNo).ToListAsync());
        var newDebts = debts.Where(d => !existingDebts.Contains(d.BillNo)).Select(d => { d.Id = 0; d.PaymentRecords = new List<PaymentRecord>(); return d; }).ToList();

        int debtCount = 0;
        foreach (var chunk in newDebts.Chunk(1000))
        {
            cloudDb.OutstandingDebts.AddRange(chunk);
            await cloudDb.SaveChangesAsync();
            debtCount += chunk.Length;
            Console.WriteLine($"   -> Transferred {debtCount:N0} / {newDebts.Count:N0} Debts...");
        }

        if (payments.Any() && await cloudDb.PaymentRecords.CountAsync() == 0)
        {
            var localDebtIdToBillNo = debts.ToDictionary(d => d.Id, d => d.BillNo);
            var cloudDebts = await cloudDb.OutstandingDebts.Select(d => new { d.Id, d.BillNo }).ToListAsync();
            var cloudDebtMap = cloudDebts.ToDictionary(d => d.BillNo, d => d.Id);

            var validPayments = new List<PaymentRecord>();
            foreach (var p in payments)
            {
                if (localDebtIdToBillNo.TryGetValue(p.OutstandingDebtId, out var billNo) && cloudDebtMap.TryGetValue(billNo, out var cloudDebtId))
                {
                    p.Id = 0;
                    p.OutstandingDebtId = cloudDebtId;
                    validPayments.Add(p);
                }
            }

            foreach (var chunk in validPayments.Chunk(1000))
            {
                cloudDb.PaymentRecords.AddRange(chunk);
                await cloudDb.SaveChangesAsync();
            }
        }

        if (auditLogs.Any() && await cloudDb.AuditLogs.CountAsync() == 0)
        {
            foreach (var chunk in auditLogs.Select(a => { a.Id = 0; return a; }).Chunk(1000))
            {
                cloudDb.AuditLogs.AddRange(chunk);
                await cloudDb.SaveChangesAsync();
            }
        }

        Console.WriteLine("\n==========================================================");
        Console.WriteLine("???? MIGRATION TO SUPABASE COMPLETED 100% SUCCESSFULLY!");
        Console.WriteLine($"Total Sales Bills in Supabase:   {await cloudDb.SalesBills.CountAsync():N0}");
        Console.WriteLine($"Total Outstanding Debts:        {await cloudDb.OutstandingDebts.CountAsync():N0}");
        Console.WriteLine($"Total Customers:                {await cloudDb.Customers.CountAsync():N0}");
        Console.WriteLine($"Total Users:                    {await cloudDb.Users.CountAsync():N0}");
        Console.WriteLine("==========================================================");
    }
}
