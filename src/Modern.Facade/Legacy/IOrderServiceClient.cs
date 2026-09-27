using System.ServiceModel;
using Legacy.Contracts;

namespace Modern.Facade.Legacy;

/// <summary>
/// The client-side view of the legacy SOAP contract — the equivalent of what <c>svcutil</c> would
/// generate from the WSDL. It uses <b>System.ServiceModel</b> attributes (client), whereas the host
/// uses CoreWCF attributes (server); <c>[ServiceContract(Name = "IOrderService")]</c> and the
/// per-operation <c>Name</c> keep the SOAP action headers identical to the server's, so the two
/// interoperate. Operations are Task-based so the facade stays fully asynchronous.
/// </summary>
[ServiceContract(Name = "IOrderService", Namespace = "http://legacy-erp.example.com/orders")]
public interface IOrderServiceClient
{
    [OperationContract(Name = "GetOrder")]
    Task<OrderDto?> GetOrderAsync(string orderNumber);

    [OperationContract(Name = "ListOrdersByCustomer")]
    Task<List<OrderDto>> ListOrdersByCustomerAsync(string customerCode);

    [OperationContract(Name = "GetStockLevel")]
    Task<StockLevelDto> GetStockLevelAsync(string sku, string warehouse);
}
