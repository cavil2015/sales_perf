using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SalesPerf.Backend.Application.DTOs;
using SalesPerf.Backend.Application.Interfaces;

namespace SalesPerf.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("kpi")]
        [ProducesResponseType(typeof(KpiResponseDto), 200)]
        public async Task<ActionResult<KpiResponseDto>> GetKpis([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
        {
            var result = await _analyticsService.GetKpisAsync(from, to, cancellationToken);
            return Ok(result);
        }

        [HttpGet("managers-rating")]
        [ProducesResponseType(typeof(List<ManagerRatingDto>), 200)]
        public async Task<ActionResult<List<ManagerRatingDto>>> GetManagersRating([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
        {
            var result = await _analyticsService.GetManagersRatingAsync(from, to, cancellationToken);
            return Ok(result);
        }

        [HttpGet("chart")]
        [ProducesResponseType(typeof(List<ChartDataDto>), 200)]
        public async Task<ActionResult<List<ChartDataDto>>> GetChartData([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
        {
            var result = await _analyticsService.GetChartDataAsync(from, to, cancellationToken);
            return Ok(result);
        }

        [HttpGet("recent")]
        [ProducesResponseType(typeof(List<RecentSaleDto>), 200)]
        public async Task<ActionResult<List<RecentSaleDto>>> GetRecentSales([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        {
            var result = await _analyticsService.GetRecentSalesAsync(limit, cancellationToken);
            return Ok(result);
        }
    }
}
