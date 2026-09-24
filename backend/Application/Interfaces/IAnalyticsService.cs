using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesPerf.Backend.Application.DTOs;

namespace SalesPerf.Backend.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task<KpiResponseDto> GetKpisAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
        Task<List<ManagerRatingDto>> GetManagersRatingAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
        Task<List<ChartDataDto>> GetChartDataAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
        Task<List<RecentSaleDto>> GetRecentSalesAsync(int limit, CancellationToken cancellationToken);
    }
}
