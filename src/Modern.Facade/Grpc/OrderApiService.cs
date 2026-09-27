using global::Grpc.Core;
using Legacy.Contracts;
using Microsoft.Extensions.Options;
using Modern.Facade.Legacy;
using Modern.Facade.Stock;
using ServiceModelFault = System.ServiceModel.FaultException;

namespace Modern.Facade.Grpc;

/// <summary>
/// The modern gRPC facade over the legacy ERP. Orders are still proxied to the legacy SOAP service
/// (through the resilience gateway); stock is routed by the strangler switch — modern read model or
/// legacy proxy — and every stock reply says which side served it.
/// </summary>
public sealed class OrderApiService : OrderApi.OrderApiBase
{
    private readonly ResilientLegacyGateway _legacy;
    private readonly IStockReadModel _modernStock;
    private readonly StranglerOptions _strangler;

    public OrderApiService(ResilientLegacyGateway legacy, IStockReadModel modernStock,
                           IOptions<StranglerOptions> strangler)
    {
        _legacy = legacy;
        _modernStock = modernStock;
        _strangler = strangler.Value;
    }

    public override async Task<OrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        OrderDto? order = await _legacy.GetOrderAsync(request.OrderNumber, context.CancellationToken);
        return order is null
            ? new OrderReply { Found = false }
            : new OrderReply { Found = true, Order = Mapping.ToProto(order) };
    }

    public override async Task<OrderList> ListOrdersByCustomer(ListByCustomerRequest request, ServerCallContext context)
    {
        List<OrderDto> orders = await _legacy.ListOrdersByCustomerAsync(request.CustomerCode, context.CancellationToken);
        var reply = new OrderList();
        reply.Orders.AddRange(orders.Select(Mapping.ToProto));
        return reply;
    }

    public override async Task<StockReply> GetStockLevel(StockRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "sku is required"));
        }

        // The strangler decision: serve the migrated capability from the modern side, or proxy it.
        if (_strangler.StockMigrated)
        {
            StockLevelDto modern = _modernStock.Get(request.Sku, request.Warehouse);
            return Mapping.ToProto(modern, servedBy: "modern");
        }

        try
        {
            StockLevelDto legacy = await _legacy.GetStockLevelAsync(request.Sku, request.Warehouse, context.CancellationToken);
            return Mapping.ToProto(legacy, servedBy: "legacy-soap");
        }
        catch (ServiceModelFault fault)
        {
            // Translate the legacy SOAP fault into a clean gRPC status instead of leaking it.
            throw new RpcException(new Status(StatusCode.InvalidArgument, fault.Message));
        }
    }
}
