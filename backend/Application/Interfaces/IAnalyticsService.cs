using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesPerf.Backend.Application.DTOs;

namespace SalesPerf.Backend.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task<KpiResponseDto> GetKpisAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
        Task<List<ManagerRatingDto>> GetManagersRatingAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
        Task<List<ChartDataDto>> GetChartDataAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
        Task<List<RecentSaleDto>> GetRecentSalesAsync(int limit = 10, CancellationToken cancellationToken = default);
        Task<(List<CategoryAnalyticsDto> Categories, List<TopProductDto> TopProducts)> GetCategoryAnalyticsAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
    }
}
