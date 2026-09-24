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
        string? TopManager
    );

    public record ManagerRatingDto(
        int ManagerId,
        string ManagerName,
        string? AvatarUrl,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Revenue,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfit,
        int SalesCount,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal AverageCheck,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal Margin
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
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal GrossProfit
    );
}
