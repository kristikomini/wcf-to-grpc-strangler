using CoreWCF;
using Legacy.Contracts;

namespace Legacy.ErpHost;

/// <summary>
/// A stand-in for the customer's real legacy ERP: an in-memory seed of orders and stock, served
/// over SOAP. It deliberately adds a little latency and can be told to fail intermittently, so the
/// modern facade's resilience pipeline (retry / circuit breaker) has something real to handle.
/// </summary>
public sealed class OrderService : IOrderService
{
    private static readonly Dictionary<string, OrderDto> Orders = Seed();

    public OrderDto? GetOrder(string orderNumber)
    {
        SimulateLatency();
        return Orders.TryGetValue(orderNumber, out var order) ? order : null;
    }

    public List<OrderDto> ListOrdersByCustomer(string customerCode)
    {
        SimulateLatency();
        return Orders.Values.Where(o => o.CustomerCode == customerCode).ToList();
    }

    public StockLevelDto GetStockLevel(string sku, string warehouse)
    {
        SimulateLatency();
        // The legacy system throws a SOAP fault for an unknown SKU; the facade must translate it.
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new FaultException("SKU is required");
        }
        int seed = Math.Abs(sku.GetHashCode());
        return new StockLevelDto
        {
            Sku = sku,
            Warehouse = string.IsNullOrWhiteSpace(warehouse) ? "MAIN" : warehouse,
            Available = seed % 500,
            Reserved = seed % 50
        };
    }

    private static void SimulateLatency() => Thread.Sleep(Random.Shared.Next(5, 25));

    private static Dictionary<string, OrderDto> Seed()
    {
        var orders = new List<OrderDto>
        {
            new()
            {
                OrderNumber = "ORD-1001", CustomerCode = "ACME", Status = "SHIPPED", Total = 249.90m,
                PlacedOnUtc = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
                Lines =
                [
                    new OrderLineDto { Sku = "PALLET-A", Description = "Euro pallet", Quantity = 3, UnitPrice = 49.90m },
                    new OrderLineDto { Sku = "WRAP-STD", Description = "Stretch wrap", Quantity = 10, UnitPrice = 10.00m }
                ]
            },
            new()
            {
                OrderNumber = "ORD-1002", CustomerCode = "ACME", Status = "PENDING", Total = 99.00m,
                PlacedOnUtc = new DateTime(2026, 2, 3, 14, 5, 0, DateTimeKind.Utc),
                Lines = [new OrderLineDto { Sku = "LABEL-XL", Description = "XL labels", Quantity = 99, UnitPrice = 1.00m }]
            },
            new()
            {
                OrderNumber = "ORD-2001", CustomerCode = "GLOBEX", Status = "CONFIRMED", Total = 1200.00m,
                PlacedOnUtc = new DateTime(2026, 2, 20, 8, 0, 0, DateTimeKind.Utc),
                Lines = [new OrderLineDto { Sku = "CONVEYOR-1", Description = "Conveyor segment", Quantity = 2, UnitPrice = 600.00m }]
            }
        };
        return orders.ToDictionary(o => o.OrderNumber);
    }
}
