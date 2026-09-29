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



                public async Task<KpiResponseDto> GetKpisAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
        {
            var (startDate, endDate) = GetDateRange(from, to);
            
            // Previous period calculation
            var duration = endDate - startDate;
            var prevStartDate = startDate - duration;
            var prevEndDate = startDate;

            // Fetch current period
            var currentSales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.Date >= startDate && s.Date < endDate && s.Status == SalesPerf.Backend.Domain.Entities.SaleStatus.Paid)
                .Select(s => new
                {
                    s.ManagerId,
                    s.Manager.Name,
                    Revenue = s.Items.Sum(i => (decimal?)i.SalePrice * i.Quantity) ?? 0,
                    Cost = s.Items.Sum(i => (decimal?)i.CostPrice * i.Quantity) ?? 0
                })
                .ToListAsync(cancellationToken);

            // Fetch previous period
            var prevSales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.Date >= prevStartDate && s.Date < prevEndDate && s.Status == SalesPerf.Backend.Domain.Entities.SaleStatus.Paid)
                .Select(s => new
                {
                    Revenue = s.Items.Sum(i => (decimal?)i.SalePrice * i.Quantity) ?? 0,
                    Cost = s.Items.Sum(i => (decimal?)i.CostPrice * i.Quantity) ?? 0
                })
                .ToListAsync(cancellationToken);

            var rev = currentSales.Sum(x => x.Revenue);
            var cost = currentSales.Sum(x => x.Cost);
            var count = currentSales.Count;
            var gp = rev - cost;
            var margin = rev == 0 ? 0 : gp / rev;
            var avg = count == 0 ? 0 : rev / count;

            var prevRev = prevSales.Sum(x => x.Revenue);
            var prevCost = prevSales.Sum(x => x.Cost);
            var prevCount = prevSales.Count;
            var prevGp = prevRev - prevCost;
            var prevAvg = prevCount == 0 ? 0 : prevRev / prevCount;

            decimal CalcDiff(decimal current, decimal previous) => previous == 0 ? (current > 0 ? 1 : 0) : (current - previous) / previous;

            var topManager = currentSales
                .GroupBy(s => s.Name)
                .OrderByDescending(g => g.Sum(x => x.Revenue))
                .Select(g => g.Key)
                .FirstOrDefault();

            return new KpiResponseDto(
                rev, gp, margin, count, avg, topManager,
                CalcDiff(rev, prevRev),
                CalcDiff(gp, prevGp),
                CalcDiff(count, prevCount),
                CalcDiff(avg, prevAvg)
            );
        }



        public async Task<List<ManagerRatingDto>> GetManagersRatingAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
        {
            var (startDate, endDate) = GetDateRange(from, to);
            var duration = endDate - startDate;
            var prevStartDate = startDate - duration;
            var prevEndDate = startDate;

            var currentSales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.Date >= startDate && s.Date < endDate && s.Status == SalesPerf.Backend.Domain.Entities.SaleStatus.Paid)
                .Select(s => new
                {
                    s.ManagerId,
                    s.Manager.Name,
                    s.Manager.AvatarUrl,
                    Revenue = s.Items.Sum(i => (decimal?)i.SalePrice * i.Quantity) ?? 0,
                    Cost = s.Items.Sum(i => (decimal?)i.CostPrice * i.Quantity) ?? 0
                })
                .ToListAsync(cancellationToken);

            var prevSales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.Date >= prevStartDate && s.Date < prevEndDate && s.Status == SalesPerf.Backend.Domain.Entities.SaleStatus.Paid)
                .Select(s => new
                {
                    s.ManagerId,
                    Revenue = s.Items.Sum(i => (decimal?)i.SalePrice * i.Quantity) ?? 0,
                    Cost = s.Items.Sum(i => (decimal?)i.CostPrice * i.Quantity) ?? 0
                })
                .ToListAsync(cancellationToken);

            var prevManagerStats = prevSales
                .GroupBy(s => s.ManagerId)
                .ToDictionary(g => g.Key, g => new {
                    Rev = g.Sum(x => x.Revenue),
                    Gp = g.Sum(x => x.Revenue - x.Cost),
                    Avg = g.Count() == 0 ? 0 : g.Sum(x => x.Revenue) / g.Count()
                });

            decimal CalcDiff(decimal current, decimal previous) => previous == 0 ? (current > 0 ? 1 : 0) : (current - previous) / previous;

            var managerStats = currentSales
                .GroupBy(s => new { s.ManagerId, s.Name, s.AvatarUrl })
                .Select(g => {
                    var rev = g.Sum(x => x.Revenue);
                    var cost = g.Sum(x => x.Cost);
                    var count = g.Count();
                    var gp = rev - cost;
                    var margin = rev == 0 ? 0 : gp / rev;
                    var avg = count == 0 ? 0 : rev / count;
                    
                    var prevRev = prevManagerStats.ContainsKey(g.Key.ManagerId) ? prevManagerStats[g.Key.ManagerId].Rev : 0;
                    var prevGp = prevManagerStats.ContainsKey(g.Key.ManagerId) ? prevManagerStats[g.Key.ManagerId].Gp : 0;
                    var prevAvg = prevManagerStats.ContainsKey(g.Key.ManagerId) ? prevManagerStats[g.Key.ManagerId].Avg : 0;

                    return new ManagerRatingDto(
                        g.Key.ManagerId,
                        g.Key.Name ?? "Unknown",
                        g.Key.AvatarUrl,
                        rev, gp, count, avg, margin,
                        CalcDiff(gp, prevGp),
                        CalcDiff(avg, prevAvg)
                    );
                })
                .ToList();

            return managerStats;
        }



        public async Task<List<ChartDataDto>> GetChartDataAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
        {
            var (startDate, endDate) = GetDateRange(from, to);

            var sales = await _context.Sales
                //                 .AsNoTracking()
                //  Exclusive upper bound
                .Where(s => s.Date >= startDate && s.Date < endDate && s.Status == SaleStatus.Paid)
                .Select(s => new
                {
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
                    g => new
                    {
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

            var dbRecentSales = await _context.Sales
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .Take(limit)
                .Select(s => new
                {
                    s.Id,
                    s.Date,
                    ManagerName = s.Manager.Name,
                    CustomerName = s.Customer.Name,
                    s.Status,
                    Revenue = s.Items.Sum(i => (decimal?)i.SalePrice * i.Quantity) ?? 0,
                    Cost = s.Items.Sum(i => (decimal?)i.CostPrice * i.Quantity) ?? 0,
                    Products = s.Items.Select(i => i.Product.Name).ToList()
                })
                .ToListAsync(cancellationToken);

            var recentSales = dbRecentSales.Select(s => new RecentSaleDto(
                s.Id,
                s.Date,
                s.ManagerName ?? "Unknown",
                s.CustomerName ?? "Unknown",
                s.Status.ToString(),
                s.Revenue,
                s.Revenue - s.Cost,
                string.Join(", ", s.Products)
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

        public async Task<(List<CategoryAnalyticsDto> Categories, List<TopProductDto> TopProducts)> GetCategoryAnalyticsAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
        {
            var query = _context.Sales
                .AsNoTracking()
                .Where(s => s.Status != SalesPerf.Backend.Domain.Entities.SaleStatus.Cancelled && s.Status != SalesPerf.Backend.Domain.Entities.SaleStatus.Refunded);

            if (from.HasValue) query = query.Where(s => s.Date >= from.Value);
            if (to.HasValue) query = query.Where(s => s.Date <= to.Value);

            var items = await query
                .SelectMany(s => s.Items)
                .Select(i => new {
                    CategoryName = i.Product.Category.Name,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Revenue = i.SalePrice * i.Quantity,
                    GrossProfit = (i.SalePrice - i.CostPrice) * i.Quantity,
                    Quantity = i.Quantity
                })
                .ToListAsync(cancellationToken);

            var categories = items
                .GroupBy(i => i.CategoryName)
                .Select(g => new CategoryAnalyticsDto(
                    g.Key,
                    g.Sum(x => x.Revenue),
                    g.Sum(x => x.GrossProfit),
                    g.Sum(x => x.Quantity)
                ))
                .OrderByDescending(c => c.Revenue)
                .ToList();

            var topProducts = items
                .GroupBy(i => new { i.ProductId, i.ProductName, i.CategoryName })
                .Select(g => new TopProductDto(
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Key.CategoryName,
                    g.Sum(x => x.Revenue),
                    g.Sum(x => x.Quantity)
                ))
                .OrderByDescending(p => p.Revenue)
                .Take(5)
                .ToList();

            return (categories, topProducts);
        }
    }
}





