using Microsoft.EntityFrameworkCore;

namespace RoyalD.Web.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<SalesBill> SalesBills { get; set; } = null!;
        public DbSet<SalesBillItem> SalesBillItems { get; set; } = null!;
        public DbSet<OutstandingDebt> OutstandingDebts { get; set; } = null!;
        public DbSet<PaymentRecord> PaymentRecords { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<FileAttachment> FileAttachments { get; set; } = null!;
        public DbSet<PendingProduct> PendingProducts { get; set; } = null!;
        public DbSet<UserMenuPermission> UserMenuPermissions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SalesBill → SalesBillItems
            modelBuilder.Entity<SalesBillItem>()
                .HasOne(i => i.SalesBill)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.BillNo);

            // Customer → SalesBills
            modelBuilder.Entity<SalesBill>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.SalesBills)
                .HasForeignKey(b => b.CustomerCode)
                .IsRequired(false);

            // Customer → OutstandingDebts
            modelBuilder.Entity<OutstandingDebt>()
                .HasOne(d => d.Customer)
                .WithMany(c => c.OutstandingDebts)
                .HasForeignKey(d => d.CustomerCode)
                .IsRequired(false);

            // OutstandingDebt → PaymentRecords
            modelBuilder.Entity<PaymentRecord>()
                .HasOne(p => p.OutstandingDebt)
                .WithMany(d => d.PaymentRecords)
                .HasForeignKey(p => p.OutstandingDebtId);

            // Indexes
            modelBuilder.Entity<SalesBill>()
                .HasIndex(b => b.SalesRep);
            modelBuilder.Entity<SalesBill>()
                .HasIndex(b => b.BillDate);
            modelBuilder.Entity<SalesBill>()
                .HasIndex(b => b.SourceMonth);
            modelBuilder.Entity<OutstandingDebt>()
                .HasIndex(d => d.Status);
            modelBuilder.Entity<OutstandingDebt>()
                .HasIndex(d => d.SalesRep);
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt);
        }
    }
}



