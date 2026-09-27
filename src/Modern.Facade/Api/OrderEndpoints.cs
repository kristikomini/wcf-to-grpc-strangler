using Microsoft.Extensions.Options;
using Modern.Facade.Legacy;
using Modern.Facade.Stock;

namespace Modern.Facade.Api;

/// <summary>
/// The partner-facing REST surface (OpenAPI) — the "external consumers" half of the strangler,
/// alongside the internal gRPC surface. Same gateway, same strangler routing.
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Orders");

        group.MapGet("/orders/{orderNumber}", async (
            string orderNumber, ResilientLegacyGateway legacy, CancellationToken ct) =>
        {
            var order = await legacy.GetOrderAsync(orderNumber, ct);
            return order is null ? Results.NotFound() : Results.Ok(OrderResponse.From(order));
        });

        group.MapGet("/orders", async (
            string customer, ResilientLegacyGateway legacy, CancellationToken ct) =>
        {
            var orders = await legacy.ListOrdersByCustomerAsync(customer, ct);
            return Results.Ok(orders.Select(OrderResponse.From));
        });

        group.MapGet("/stock/{sku}", async (
            string sku, string? warehouse, ResilientLegacyGateway legacy,
            IStockReadModel modernStock, IOptions<StranglerOptions> strangler, CancellationToken ct) =>
        {
            warehouse ??= "MAIN";
            if (strangler.Value.StockMigrated)
            {
                return Results.Ok(StockResponse.From(modernStock.Get(sku, warehouse), "modern"));
            }
            var legacyStock = await legacy.GetStockLevelAsync(sku, warehouse, ct);
            return Results.Ok(StockResponse.From(legacyStock, "legacy-soap"));
        });
    }
}
