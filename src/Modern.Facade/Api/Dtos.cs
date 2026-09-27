using System.Globalization;
using Legacy.Contracts;

namespace Modern.Facade.Api;

// Partner-facing REST DTOs — records, never the legacy data contracts leaked directly.
public record OrderLineResponse(string Sku, string Description, int Quantity, decimal UnitPrice);

public record OrderResponse(
    string OrderNumber, string CustomerCode, string Status, decimal Total,
    DateTime PlacedOnUtc, IReadOnlyList<OrderLineResponse> Lines)
{
    public static OrderResponse From(OrderDto dto) => new(
        dto.OrderNumber, dto.CustomerCode, dto.Status, dto.Total, dto.PlacedOnUtc,
        dto.Lines.Select(l => new OrderLineResponse(l.Sku, l.Description, l.Quantity, l.UnitPrice)).ToList());
}

public record StockResponse(string Sku, string Warehouse, int Available, int Reserved, string ServedBy)
{
    public static StockResponse From(StockLevelDto dto, string servedBy) =>
        new(dto.Sku, dto.Warehouse, dto.Available, dto.Reserved, servedBy);
}
