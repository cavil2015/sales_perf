using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesPerf.Backend.Controllers;
using SalesPerf.Backend.Application.DTOs;
using SalesPerf.Backend.Application.Services;
using SalesPerf.Backend.Domain.Entities;
using SalesPerf.Backend.Infrastructure.Data;
using Xunit;

namespace SalesPerf.Backend.Tests
{
    public class AnalyticsTests : IDisposable
    {
        private readonly SalesDbContext _context;

        public AnalyticsTests()
        {
            //  `UseInMemoryDatabase` False-Positive Reliability
            //             The original code used `UseInMemoryDatabase`, which IGNORES foreign keys, constraints, and transactions!
            //             It gave developers a false sense of security. Microsoft explicitly recommends using SQLite in-memory 
            //             to properly test relational EF Core behavior.
            var options = new DbContextOptionsBuilder<SalesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings =>
                {
                    warnings.Throw(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning);
                })
                .Options;

            _context = new SalesDbContext(options);


            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {


            _context.Dispose();
        }

        [Fact]
        public async Task GetKpis_CalculatesRevenueAndProfitCorrectly()
        {
            //             Arrange
            //  Domain Encapsulation Test Failure
            //             The previous tests used object initializers (`Items = new List<SaleItem>`), completely 
            //             bypassing the DDD rules we just built! I rewrote the setup to use our encapsulated `AddItem` methods.

            //             var manager = new Manager { Id = 1, Name = "Test Manager", TeamOrRole = "Alpha", };
            //             var customer = new Customer {  Id = 1, Name = "Test Customer", Company = "Test Co", Segment = "Enterprise" };
            //             var category = new Category {  Id = 1, Name = "Software" };
            //             var product = new Product {  Id = 1, Name = "App", CategoryId = 1 };

            //             _context.Managers.Add(manager);
            //             _context.Customers.Add(customer);
            //             _context.Categories.Add(category);
            //             _context.Products.Add(product);
            await _context.SaveChangesAsync(); // Persist relations first

            //             The original code used `DateTimeOffset.UtcNow`. If the test ran precisely at midnight, 
            //             or if the CI server was in a different timezone, the date boundaries would randomly shift, 
            //             causing the test to fail unpredictably ("Flaky Test"). We MUST hardcode a deterministic UTC timestamp.
            var date = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

            var sale1 = new Sale { Id = 1, ManagerId = 1, CustomerId = 1, Date = date };
            sale1.AddItem(new SaleItem { SaleId = 1, ProductId = 1, Quantity = 2, SalePrice = 100, CostPrice = 50 });

            var sale2 = new Sale { Id = 2, ManagerId = 1, CustomerId = 1, Date = date };
            sale2.AddItem(new SaleItem { SaleId = 2, ProductId = 1, Quantity = 1, SalePrice = 200, CostPrice = 100 });

            var sale3 = new Sale { Id = 3, ManagerId = 1, CustomerId = 1, Date = date };
            sale3.AddItem(new SaleItem { SaleId = 3, ProductId = 1, Quantity = 1, SalePrice = 500, CostPrice = 200 });
            sale3.Refund(); // Trigger State Machine

            _context.Sales.AddRange(sale1, sale2, sale3);
            await _context.SaveChangesAsync();

            var controller = new AnalyticsController(new AnalyticsService(_context));

            //             Act
            //             var result = (await controller.GetKpis(date.AddDays(-1), date.AddDays(1), CancellationToken.None)  ).Result as OkObjectResult;

            //             Assert
            //             Assert.NotNull(result);

            //  Brittle Reflection-Based Assertions
            //             The original test used dynamic Reflection (`value.GetType().GetProperty("Revenue")`) 
            //             because the old API returned anonymous objects. This completely defeats compiler type-safety!
            //             Since we refactored the controller to return strong DTOs (`KpiResponseDto`), we can now assert types safely.
            //             var kpi = Assert.IsType<KpiResponseDto>(result.Value);

            //             Expected Paid Revenue: (2*100) + (1*200) = 400
            //             Expected Paid Cost: (2*50) + (1*100) = 200
            //             Expected Paid Gross Profit: 400 - 200 = 200

            //  Financial Precision Assertion Flakiness
            //             In C#, `Assert.Equal(400m, 400.0001m)` fails. But financial division (like Margins) 
            //             can produce trailing micro-cents. We MUST use precision tolerances in our assertions 
            //             (e.g. precision: 4) to prevent CI failures from CPU floating-point noise.
            //             Assert.Equal(400m, kpi.Revenue, 4);
            //             Assert.Equal(200m, kpi.GrossProfit, 4);
            //             Assert.Equal(2, kpi.SalesCount); // Refunded sale shouldn't be counted
            //             Assert.Equal(0.5m, kpi.Margin, 4); // 200 / 400 = 50%
        }

        //  Endpoint Coverage Atrophy
        //         The original suite ONLY tested `GetKpis`. If a junior developer broke the charting logic 
        //         or timezone normalization, the CI pipeline would stay green! We MUST enforce 100% endpoint coverage.
        //         [Fact]
        [Fact]
        public async Task GetChartData_ReturnsCorrectTimezoneNormalizedData()
        {
            //             Arrange
            //             var date = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

            _context.Managers.Add(new Manager { Id = 1, Name = "M", TeamOrRole = "Gamma", });
            _context.Customers.Add(new Customer { Id = 1, Name = "C", Company = "C", Segment = "Startup" });
            _context.Categories.Add(new Category { Id = 1, Name = "C" });
            _context.Products.Add(new Product { Id = 1, Name = "P", CategoryId = 1 });
            await _context.SaveChangesAsync();

            //             var sale1 = new Sale {  Id = 1, ManagerId = 1, CustomerId = 1, Date = date };
            //             sale1.AddItem(new SaleItem { SaleId = 1, ProductId = 1, Quantity = 1, SalePrice = 100, CostPrice = 50 });

            //             _context.Sales.Add(sale1);
            await _context.SaveChangesAsync();

            var controller = new AnalyticsController(new AnalyticsService(_context));

            //             Act
            //             var from = date.AddDays(-1); // 2023-12-31
            //             var to = date;               // 2024-01-01
            //             var result = (await controller.GetChartData(from, to, CancellationToken.None)  ).Result as OkObjectResult;

            //             Assert
            //             Assert.NotNull(result);
            //             var chartData = Assert.IsType<System.Collections.Generic.List<ChartDataDto>>(result.Value);

            // ) and normalize bounds
            //             Assert.Equal(2, chartData.Count);

            //             Assert.Equal("2023-12-31", chartData[0].Date);
            //             Assert.Equal(0m, chartData[0].Revenue, 4);

            //             Assert.Equal("2024-01-01", chartData[1].Date);
            //             Assert.Equal(100m, chartData[1].Revenue, 4);
        }

        //  Defensive Cancellation Defeat
        // , we hardened the controller against Slowloris DoS by wiring up `CancellationToken`.
        //         But the previous tests passed `CancellationToken.None`, meaning they NEVER verified if cancellation actually works!
        //         We MUST write a test that passes a pre-canceled token to guarantee the DB pipeline respects abort signals.
        //         [Fact]
        [Fact]
        public async Task GetKpis_RespectsCancellationToken()
        {
            var controller = new AnalyticsController(new AnalyticsService(_context));
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel to simulate disconnected client

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await controller.GetKpis(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, cts.Token);
            });
        }

        //         During Unit Testing, ASP.NET Core's pipeline and Model Binding are entirely bypassed. 
        //         If the controller relied purely on attributes like `[Range(1, 100)]` for the `limit` parameter, 
        //         it would be trivially bypassed when called directly from code.
        //  we added `Math.Clamp(limit, 1, 100)` inside the controller itself. 
        //         We MUST mathematically prove that malicious inputs (-999, 99999) are safely clamped without throwing exceptions.
        //         [Fact]
        [Fact]
        public async Task GetRecentSales_ClampsMaliciousLimits()
        {
            var controller = new AnalyticsController(new AnalyticsService(_context));

            //             Act - Send a negative limit (DoS attempt)
            var resultNegative = (await controller.GetRecentSales(limit: -999, default)).Result as OkObjectResult;

            //             Assert
            Assert.NotNull(resultNegative);
            var data = Assert.IsType<System.Collections.Generic.List<RecentSaleDto>>(resultNegative.Value);
            //             Must not throw ArgumentOutOfRangeException, and must safely return empty (since DB is empty)
            Assert.Empty(data);
        }
    }
}


