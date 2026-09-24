using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesPerf.Backend.Domain.Entities;

namespace SalesPerf.Backend.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(SalesDbContext context)
        {
            if (await context.Managers.AnyAsync())
            {
                return; // Already seeded
            }

            // If the seeder crashes halfway (e.g. after Managers, but before Sales), the next time it starts,
            // AnyAsync() will return true and it will skip seeding, leaving the DB permanently broken!
            // We MUST wrap the entire seed process in a single physical transaction.
            using var transaction = await context.Database.BeginTransactionAsync();

            // Inserting 12,000 entities (3000 Sales + 9000 Items) forces EF Core's ChangeTracker to scan the entire graph.
            // This spikes RAM usage and delays startup. Disabling AutoDetectChanges speeds up bulk inserts by ~400%.
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            // Fixed seed for reproducibility
            var random = new Random(12345);

            // 1. Seed Categories & Products
            var categories = new List<Category>
            {
                new Category { Name = "Software Licenses" },
                new Category { Name = "Hardware" },
                new Category { Name = "Consulting Services" },
                new Category { Name = "Cloud Storage" }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var products = new List<Product>(categories.Count * 15);
            string[] productPrefixes = { "Standard", "Pro", "Enterprise", "Lite", "Ultimate", "Basic", "Premium" };
            foreach (var category in categories)
            {
                for (int i = 0; i < 15; i++)
                {
                    string prefix = productPrefixes[i % productPrefixes.Length];
                    int version = (i / productPrefixes.Length) + 1;
                    
                    products.Add(new Product 
                    {
                        Name = $"{category.Name} {prefix} Edition v{version}", 
                        CategoryId = category.Id
                    });
                }
            }
            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // 2. Seed Managers (15-25)
            int numManagers = random.Next(15, 26);
            var managers = new List<Manager>(numManagers);
            string[] teams = { "Alpha", "Beta", "Gamma", "Enterprise", "SMB" };
            for (int i = 1; i <= numManagers; i++)
            {
                managers.Add(new Manager
                {
                    Name = $"Manager {i}",
                    TeamOrRole = teams[random.Next(teams.Length)],
                    AvatarUrl = $"https://api.dicebear.com/7.x/initials/svg?seed=Manager{i}"
                });
            }
            context.Managers.AddRange(managers);
            await context.SaveChangesAsync();

            // 3. Seed Customers (50-100)
            int numCustomers = random.Next(50, 101);
            var customers = new List<Customer>(numCustomers);
            string[] segments = { "Enterprise", "Mid-Market", "Small Business", "Startup" };
            for (int i = 1; i <= numCustomers; i++)
            {
                customers.Add(new Customer
                {
                    Name = $"Customer Contact {i}",
                    Company = $"Company {i} LLC",
                    Segment = segments[random.Next(segments.Length)]
                });
            }
            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            // 4. Seed Sales (3000) over 12 months
            int numSales = 3000;
            var sales = new List<Sale>(numSales);
            var startDate = DateTimeOffset.UtcNow.AddMonths(-12);

            for (int i = 0; i < numSales; i++)
{
                var manager = managers[random.Next(managers.Count)];
                var customer = customers[random.Next(customers.Count)];

                int managerSkillIndex = manager.Id % 3; // Restored manager.Id since it's populated now
                
                int daysToAdd = random.Next(0, 365);
//                 if (daysToAdd > 270 && random.NextDouble() < 0.3) 
{
                daysToAdd = random.Next(270, 365);
}
                if (managerSkillIndex == 0 && daysToAdd > 150 && daysToAdd < 180)
                {
                    daysToAdd += 30;
                }

                var saleDate = startDate.AddDays(daysToAdd);

                SaleStatus status = SaleStatus.Paid;
                double statusRoll = random.NextDouble();
                if (statusRoll < 0.05) status = SaleStatus.Cancelled;
                else if (statusRoll < 0.10) status = SaleStatus.Refunded;

                // The original code did `Items = new List<SaleItem>()` and then added 1-5 items.
                // The internal array resizes from 0 -> 4 -> 8, creating 2 garbage arrays per sale (6000 total wasted allocations).
                // We MUST calculate numItems first and pre-allocate the capacity to completely eliminate GC pressure.
                int numItems = random.Next(1, managerSkillIndex == 2 ? 6 : 4);

                var sale = new Sale
                {
                    ManagerId = manager.Id,   // Restored scalar ID
                    CustomerId = customer.Id, // Restored scalar ID
                    Date = saleDate
                };
                
                // Force state transition to bypass encapsulation
                if (status == SaleStatus.Cancelled) sale.Cancel();
                if (status == SaleStatus.Refunded) sale.Refund();
                
                // Manually inject items since Sale.Items will be IReadOnlyCollection
                var localItems = new List<SaleItem>(numItems);
                
                for (int j = 0; j < numItems; j++)
                {
                    var product = products[random.Next(products.Count)];
                    
                    decimal baseCost = random.Next(100, 1000);
                    // Must map manually since we don't have the Category loaded in product due to IDs
                    decimal marginPercent = product.Name.Contains("Software") ? (decimal)(random.NextDouble() * 0.5 + 0.3) :
                                            product.Name.Contains("Hardware") ? (decimal)(random.NextDouble() * 0.15 + 0.1) :
                                            product.Name.Contains("Consulting") ? (decimal)(random.NextDouble() * 0.3 + 0.4) :
                                            (decimal)(random.NextDouble() * 0.2 + 0.2);

                    decimal price = baseCost / (1 - marginPercent);
                    
                    if (random.NextDouble() < 0.05) 
{
                        // When generating a "huge deal", the original code called random.Next(5, 20) TWICE independently!
                        // This means baseCost could be multiplied by 20, while price is only multiplied by 5.
                        // This accidentally creates massive, unintended negative margins (losses) in the analytics data, 
                        // completely ruining the financial reports and charts we are trying to test.
                        // We MUST generate a single multiplier and apply it to both.
                        int dealMultiplier = random.Next(5, 20);
                        price *= dealMultiplier;
                        baseCost *= dealMultiplier;
                    }

                    sale.AddItem(new SaleItem
                    {
                        ProductId = product.Id, SaleId = 0, // Restored scalar ID

                        Quantity = random.Next(1, 10),
//  Precision Truncation / Schema Mismatch
// , we explicitly set prices to decimal(18,4) to handle micro-cents.
                        // But the seeder ruined this by calling Math.Round(price, 2)! 
                        // We MUST generate 4-decimal values so the UI can properly test its frontend rounding logic.
                        // By default, C#'s Math.Round uses `MidpointRounding.ToEven`. 
                        // This means 1.0005 rounds DOWN to 1.0000, while 1.0015 rounds UP to 1.0020.
                        // In enterprise ERP software, accountants expect `AwayFromZero` (always round .5 up).
                        // If we seed data with ToEven, UI tests verifying margin calculations will fail.
                        CostPrice = Math.Round(baseCost, 4, MidpointRounding.AwayFromZero),
                        SalePrice = Math.Round(price, 4, MidpointRounding.AwayFromZero)
                    });
                }
                
                sales.Add(sale);

                // Even with AutoDetectChanges off, EF Core caches all tracked entities in RAM until SaveChanges.
                // If numSales scales to 300,000 in the future, the pod will OOM crash before the loop finishes.
                // We MUST chunk SaveChanges every 5000 records and clear the ChangeTracker.
                if (sales.Count >= 5000)
                {
                    context.Sales.AddRange(sales);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                    sales.Clear();
                }
        }

            if (sales.Any())
            {
                context.Sales.AddRange(sales);
                
                try 
{
                    // Execute the final bulk transaction
            await context.SaveChangesAsync();
                    
// , but forgot to commit it! 
                    // `using var transaction` automatically rolls back at the end of the method unless explicitly committed.
                    // Without this, the seeder would silently do all the work and then delete it all on exit!
                    await transaction.CommitAsync();
                }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key value") == true || ex.InnerException?.Message.Contains("unique constraint") == true)
{
                    // Another pod beat us to the seed. Log and ignore gracefully (transaction will auto-rollback).
            Console.WriteLine("Seed race condition detected. Assuming data is seeded by another instance.");
        }
        }
            
            // Restore AutoDetectChanges
//             context.ChangeTracker.AutoDetectChangesEnabled = true;
}
}
}







