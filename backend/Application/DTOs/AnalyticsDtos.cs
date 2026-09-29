using System;
using System.Text.Json.Serialization;

namespace SalesPerf.Backend.Application.DTOs
{
    public record KpiResponseDto(
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Revenue,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfit,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Margin,
        int SalesCount,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal AverageCheck,
        string? TopManager,
        
        // Diff properties (percentage change)
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal RevenueDiff,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfitDiff,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal SalesCountDiff,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal AverageCheckDiff
    );

    public record ManagerRatingDto(
        int ManagerId,
        string ManagerName,
        string? AvatarUrl,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Revenue,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfit,
        int SalesCount,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal AverageCheck,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Margin,
        
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfitDiff,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal AverageCheckDiff
    );

    public record ChartDataDto(
        string Date,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Revenue,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfit,
        int SalesCount
    );

    public record RecentSaleDto(
        int Id,
        DateTimeOffset Date,
        string ManagerName,
        string CustomerName,
        string Status,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Revenue,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfit,
        string Products
    );

    public record CategoryAnalyticsDto(
        string CategoryName,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Revenue,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfit,
        int SalesCount
    );

    public record TopProductDto(
        int ProductId,
        string ProductName,
        string CategoryName,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Revenue,
        int SalesCount
    );
}
