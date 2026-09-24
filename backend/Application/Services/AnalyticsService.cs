using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesPerf.Backend.Infrastructure.Data;
using SalesPerf.Backend.Domain.Entities;
using System.Collections.Generic;
using SalesPerf.Backend.Application.DTOs;
using SalesPerf.Backend.Application.Interfaces;

namespace SalesPerf.Backend.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly SalesDbContext _context;

        public AnalyticsService(SalesDbContext context)
        {
            _context = context;
        }

        
        
        public async Task<KpiResponseDto> GetKpisAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
        {
            var (startDate, endDate) = GetDateRange(from, to);

            var salesQuery = _context.Sales
//                 .AsNoTracking()
//  Exclusive upper bounds (< endDate). Prevents fractional second rounding leaks in PostgreSQL/SQL Server.
                .Where(s => s.Date >= startDate && s.Date < endDate && s.Status == SaleStatus.Paid);

//  (Bulletproof): EF Core often crashes on nested Sum() inside GroupBy. 
            // We split into Count (Query 1) and SelectMany Aggregates (Query 2).
            var salesCount = await salesQuery.CountAsync(cancellationToken);

            if (salesCount == 0) 
                return new KpiResponseDto(0, 0, 0, 0, 0, null);

            var itemAggs = await salesQuery
                .SelectMany(s => s.Items)
                .GroupBy(x => 1)
                .Select(g => new {
                    TotalRevenue = g.Sum(i => i.SalePrice * i.Quantity),
                    TotalCost = g.Sum(i => i.CostPrice * i.Quantity)
                })
                .FirstOrDefaultAsync(cancellationToken);

var totalRev = itemAggs?.TotalRevenue ?? 0;
var totalCost = itemAggs?.TotalCost ?? 0;
var grossProfit = totalRev - totalCost;
var margin = totalRev != 0 ? grossProfit / totalRev : 0;
var averageCheck = salesCount != 0 ? totalRev / salesCount : 0;

var topManagerName = await salesQuery
//  Nested Sum Translation Crash!
                // GroupBy followed by a nested g.Sum(x => x.Items.Sum(...)) cannot be translated by EF Core to SQL.
                // It throws InvalidOperationException. We MUST flatten it with SelectMany just like we did in GetManagersRating.
                .SelectMany(s => s.Items, (s, i) => new { s.ManagerId, s.Manager.Name, i.SalePrice, i.Quantity })
                .GroupBy(x => new { x.ManagerId, x.Name })
                .Select(g => new { ManagerId = g.Key.ManagerId, ManagerName = g.Key.Name, Revenue = g.Sum(x => x.SalePrice * x.Quantity) })
                .OrderByDescending(x => x.Revenue)
                .ThenBy(x => x.ManagerName)
                .ThenBy(x => x.ManagerId)
                .Select(x => x.ManagerName)
                .FirstOrDefaultAsync(cancellationToken);

            return new KpiResponseDto(
                totalRev, 
                grossProfit, 
                margin, 
                salesCount, 
                averageCheck, 
                topManagerName
            );
        }

        
        
        public async Task<List<ManagerRatingDto>> GetManagersRatingAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
        {
            var (startDate, endDate) = GetDateRange(from, to);

            // Filtering solely by m.IsActive drops fired/inactive managers from the historical rating.
            // But their sales still exist in the database and contribute to the global KPI TotalRevenue!
            // We MUST include inactive managers IF they have any sales in the requested date range,
            // otherwise the sum of the manager table won't match the global KPI revenue.
            var allManagers = await _context.Managers
                .AsNoTracking()
                .Where(m => m.IsActive || m.Sales.Any(s => s.Date >= startDate && s.Date < endDate && s.Status == SaleStatus.Paid))
                // Fetching full entities (SELECT *) pulls potentially large unused columns (like long Bio or binary data) into RAM.
                // We MUST project only the exact columns needed for the DTO to future-proof against memory leaks.
                .Select(m => new { m.Id, m.Name, m.AvatarUrl })
                .ToListAsync(cancellationToken);

            // 2. Fetch True Sales Count (avoids INNER JOIN drop of empty sales)
            var salesCounts = await _context.Sales
//                 .AsNoTracking()
//  Exclusive upper bound
                .Where(s => s.Date >= startDate && s.Date < endDate && s.Status == SaleStatus.Paid)
                .GroupBy(s => s.ManagerId)
                .Select(g => new { ManagerId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // 3. Fetch Revenue Aggregates via SelectMany
            var aggregates = await _context.Sales
//                 .AsNoTracking()
//  Exclusive upper bound
                .Where(s => s.Date >= startDate && s.Date < endDate && s.Status == SaleStatus.Paid)
                .SelectMany(s => s.Items, (s, i) => new { s.ManagerId, i.SalePrice, i.CostPrice, i.Quantity })
                .GroupBy(x => x.ManagerId)
                .Select(g => new {
                    ManagerId = g.Key,
                    Revenue = g.Sum(x => x.SalePrice * x.Quantity),
                    Cost = g.Sum(x => x.CostPrice * x.Quantity)
                })
                .ToListAsync(cancellationToken);

            // aggregates and salesCounts are lists. Using FirstOrDefault inside a loop over allManagers
            // creates an O(N^2) Cartesian product. For 10,000 managers, this is 100,000,000 operations per request!
            // We MUST use O(1) HashMaps (Dictionaries) to map the data safely.
            var aggDict = aggregates.ToDictionary(a => a.ManagerId);
            var scDict = salesCounts.ToDictionary(a => a.ManagerId);

            // 4. Zip in memory (Now strictly O(N))
            var result = allManagers.Select(m => {
                aggDict.TryGetValue(m.Id, out var agg);
                scDict.TryGetValue(m.Id, out var sc);
                
                var rev = agg?.Revenue ?? 0;
                var cost = agg?.Cost ?? 0;
                var count = sc?.Count ?? 0;
                var gp = rev - cost;
                
                return new ManagerRatingDto(
                    m.Id,
                    m.Name,
                    m.AvatarUrl,
                    rev,
                    gp,
                    count,
                    count != 0 ? rev / count : 0,
                    rev != 0 ? gp / rev : 0
                );
            }).OrderByDescending(m => m.GrossProfit)
              .ThenBy(m => m.ManagerName)
              .ThenBy(m => m.ManagerId)
              .ToList();

            return result;
        }

        
        
        public async Task<List<ChartDataDto>> GetChartDataAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
        {
            var (startDate, endDate) = GetDateRange(from, to);
            
            var sales = await _context.Sales
//                 .AsNoTracking()
//  Exclusive upper bound
                .Where(s => s.Date >= startDate && s.Date < endDate && s.Status == SaleStatus.Paid)
                .Select(s => new {
                    s.Date,
                    Revenue = s.Items.Sum(i => (decimal?)i.SalePrice * i.Quantity) ?? 0,
                    Cost = s.Items.Sum(i => (decimal?)i.CostPrice * i.Quantity) ?? 0
                })
                .ToListAsync(cancellationToken);

            var groupedData = sales
                .GroupBy(s => s.Date.ToOffset(startDate.Offset).Date)
                .ToDictionary(
                    // If the server OS uses a non-Gregorian calendar (e.g. Thai Buddhist), .ToString() outputs year 2569!
                    // This crashes the frontend Recharts parsing. We MUST enforce InvariantCulture.
                    g => g.Key.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    g => new {
                        Revenue = g.Sum(x => x.Revenue),
                        GrossProfit = g.Sum(x => x.Revenue - x.Cost),
                        SalesCount = g.Count()
                    }
                );

//  Fill in missing days!
var chartData = new List<ChartDataDto>();
//  Changed loop condition to < endDate.Date to match exclusive bound logic
            for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
            {
                var dateStr = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                if (groupedData.TryGetValue(dateStr, out var data))
                {
                    chartData.Add(new ChartDataDto(dateStr, data.Revenue, data.GrossProfit, data.SalesCount));
                }
                else
                {
                    chartData.Add(new ChartDataDto(dateStr, 0, 0, 0));
                }
            }

            return chartData;
        }

        
        
        public async Task<List<RecentSaleDto>> GetRecentSalesAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 100);

//  Enum Translation Crash!
            // Calling s.Status.ToString() inside an EF Core projection throws an InvalidOperationException 
            // if the enum is stored as an integer (default), because SQL cannot natively cast int to the C# enum string.
            // We MUST project the raw enum and do the .ToString() conversion in C# memory.
            var dbRecentSales = await _context.Sales
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .ThenByDescending(s => s.Id)
                .Take(limit)
                .Select(s => new {
                    s.Id,
                    s.Date,
                    ManagerName = s.Manager.Name,
                    CustomerName = s.Customer.Name,
                    s.Status, // Raw enum
                    Revenue = s.Items.Sum(i => (decimal?)i.SalePrice * i.Quantity) ?? 0,
                    Cost = s.Items.Sum(i => (decimal?)i.CostPrice * i.Quantity) ?? 0
                })
                .ToListAsync(cancellationToken);

            var recentSales = dbRecentSales.Select(s => new RecentSaleDto(
                s.Id,
                s.Date,
                s.ManagerName ?? "Unknown",
                s.CustomerName ?? "Unknown",
                s.Status.ToString(), // Safe memory translation
                s.Revenue,
                s.Revenue - s.Cost
            )).ToList();

            return recentSales;
        }

        private (DateTimeOffset startDate, DateTimeOffset endDate) GetDateRange(DateTimeOffset? from, DateTimeOffset? to)
        {
//  Fuzzing Crash Prevention (DateTime Boundaries)
            // If a malicious user or fuzzer sends ?to=0001-01-01, endDateRaw.AddDays(-30) throws ArgumentOutOfRangeException (Year < 1).
            // If they send ?to=9999-12-31, .AddDays(1) below throws ArgumentOutOfRangeException (Year > 9999).
            // We MUST clamp the raw inputs to safe calendar boundaries before applying any date math.
            var safeMin = DateTimeOffset.MinValue.AddDays(30);
            var safeMax = DateTimeOffset.MaxValue.AddDays(-1);
            
            var endDateRaw = to ?? DateTimeOffset.UtcNow;
            if (endDateRaw > safeMax) endDateRaw = safeMax;
            if (endDateRaw < safeMin) endDateRaw = safeMin;

            var startDateRaw = from ?? endDateRaw.AddDays(-30);
            if (startDateRaw > safeMax) startDateRaw = safeMax;
            if (startDateRaw < safeMin) startDateRaw = safeMin;
            
            // If the user selects a range where from > to, the loop condition fails and queries return empty data.
            // Defensive programming: automatically swap them.
            if (startDateRaw > endDateRaw)
            {
                (startDateRaw, endDateRaw) = (endDateRaw, startDateRaw);
            }

            var startDate = new DateTimeOffset(startDateRaw.Date, startDateRaw.Offset);
            
//  Cross-DST Boundary Loss
            // If the requested 'from' and 'to' have different timezone offsets (e.g. they span across a Daylight Saving Time shift),
            // calculating endDate.Date on a different offset will cause the calendar loop in GetChartData to mismatch the shifted data.
            // We MUST normalize endDate to have the EXACT same timezone offset as startDate to guarantee a uniform calendar grid.
            var normalizedEndDateRaw = endDateRaw.ToOffset(startDateRaw.Offset);
            var endDate = new DateTimeOffset(normalizedEndDateRaw.Date, startDateRaw.Offset).AddDays(1);

            // GetChartData pulls all sales into C# memory to avoid the Npgsql Timezone bug. 
            // If a user queries a 10-year period, this would crash the server (OutOfMemoryException).
            // We cap the maximum analytics window to 365 days.
            if ((endDate - startDate).TotalDays > 365)
            {
                startDate = endDate.AddDays(-365);
            }

            return (startDate, endDate);
        }
    }
}




