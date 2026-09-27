using CoreWCF;
using Legacy.Contracts;

namespace Legacy.ErpHost;

/// <summary>
/// The legacy ERP's SOAP service contract, as the old system exposes it. Uses <b>CoreWCF</b>
/// attributes (server side) — the modern facade declares its own System.ServiceModel copy of this
/// interface for the client. The data types come from the shared <c>Legacy.Contracts</c>.
/// </summary>
[ServiceContract(Namespace = "http://legacy-erp.example.com/orders")]
public interface IOrderService
{
    [OperationContract]
    OrderDto? GetOrder(string orderNumber);

    [OperationContract]
    List<OrderDto> ListOrdersByCustomer(string customerCode);

    [OperationContract]
    StockLevelDto GetStockLevel(string sku, string warehouse);
}
