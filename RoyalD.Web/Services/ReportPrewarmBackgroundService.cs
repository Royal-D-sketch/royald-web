using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoyalD.Web.Models;
using RoyalD.Web.Services;

namespace RoyalD.Web.Services
{
    public class ReportPrewarmBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ReportPrewarmBackgroundService> _logger;
        private static readonly AutoResetEvent _refreshSignal = new(false);

        public ReportPrewarmBackgroundService(IServiceProvider services, ILogger<ReportPrewarmBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public static void SignalRefresh()
        {
            try
            {
                _refreshSignal.Set();
            }
            catch {}
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial delay of 6 seconds after startup so Kestrel & Render port binding finishes immediately
            await Task.Delay(6000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("🚀 [ReportPrewarm] Starting background pre-warming for all reports...");
                    await PrewarmAllReportsAsync(stoppingToken);
                    _logger.LogInformation("✅ [ReportPrewarm] All reports pre-warmed successfully into MemoryCache!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ [ReportPrewarm] Error during background pre-warming");
                }

                // Wait 15 minutes OR until SignalRefresh() is triggered
                try
                {
                    await Task.Run(() => _refreshSignal.WaitOne(TimeSpan.FromMinutes(15)), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task PrewarmAllReportsAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var reportSvc = scope.ServiceProvider.GetRequiredService<ReportService>();
            var dashboardSvc = scope.ServiceProvider.GetRequiredService<DashboardService>();
            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cacheDuration = TimeSpan.FromMinutes(45);

            // 1. Dashboard Page Data
            try
            {
                var dashData = await dashboardSvc.LoadDashboardPageDataAsync();
                cache.Set("dashboard_page_data_cache", dashData, cacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Prewarm Dashboard: {Msg}", ex.Message);
            }

            // 2. Annual Performance / Sales Report
            try
            {
                var annualData = await reportSvc.GetAnnualPerformanceAsync();
                cache.Set("annual_performance_cache", annualData, cacheDuration);
                cache.Set("annual_performance_report_cache", annualData, cacheDuration);
                cache.Set("annual_performance_report_svc", annualData, cacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Prewarm AnnualPerformance: {Msg}", ex.Message);
            }

            // 3. Product Details (Default: null, null)
            try
            {
                var prodDetails = await reportSvc.GetProductDetailsReportAsync(null, null);
                cache.Set("prod_details__", prodDetails, cacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Prewarm ProductDetails: {Msg}", ex.Message);
            }

            // 4. Customer Product (Default admin view)
            try
            {
                var custProd = await reportSvc.GetCustomerProductReportAsync(null, null, null, null, null, null, null);
                cache.Set("cust_prod______", custProd, cacheDuration);
                cache.Set("cust_prod______admin", custProd, cacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Prewarm CustomerProduct: {Msg}", ex.Message);
            }

            // 5. Customer Purchase Summary (Default admin view)
            try
            {
                var custPurch = await reportSvc.GetCustomerPurchaseSummaryAsync(null, null, null, null, null, null, null, null);
                cache.Set("cust_purch_______", custPurch, cacheDuration);
                cache.Set("cust_purch___admin____", custPurch, cacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Prewarm CustomerPurchaseSummary: {Msg}", ex.Message);
            }

            // 6. Distinct Sales Representatives Lists
            try
            {
                var debtorReps = await db.OutstandingDebts.AsNoTracking()
                    .Select(d => d.SalesRep)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync(cancellationToken);
                cache.Set("all_debtor_reps", debtorReps, cacheDuration);

                var returnedReps = await db.OutstandingDebts.AsNoTracking()
                    .Where(d => (d.Status == DebtStatus.ReturnedToAccount || d.ReturnedToDeliveryDate != null || d.ReturnedToAccountDate != null) && d.SalesRep != null && d.SalesRep != "")
                    .Select(d => d.SalesRep)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken);
                cache.Set("all_returned_account_reps", returnedReps, cacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Prewarm Reps: {Msg}", ex.Message);
            }
        }
    }
}
