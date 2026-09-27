using System.Globalization;
using Legacy.Contracts;
using Modern.Facade.Grpc;

namespace Modern.Facade.Grpc;

/// <summary>Maps the legacy SOAP data contracts to the Protobuf messages. Money is rendered with
/// the invariant culture so "1234.50" is stable across locales; timestamps as round-trip ISO-8601.</summary>
internal static class Mapping
{
    public static Order ToProto(OrderDto dto)
    {
        var order = new Order
        {
            OrderNumber = dto.OrderNumber,
            CustomerCode = dto.CustomerCode,
            Status = dto.Status,
            Total = dto.Total.ToString(CultureInfo.InvariantCulture),
            PlacedOnUtc = dto.PlacedOnUtc.ToString("O", CultureInfo.InvariantCulture)
        };
        foreach (var line in dto.Lines)
        {
            order.Lines.Add(new OrderLine
            {
                Sku = line.Sku,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice.ToString(CultureInfo.InvariantCulture)
            });
        }
        return order;
    }

    public static StockReply ToProto(StockLevelDto dto, string servedBy) => new()
    {
        Sku = dto.Sku,
        Warehouse = dto.Warehouse,
        Available = dto.Available,
        Reserved = dto.Reserved,
        ServedBy = servedBy
    };
}
