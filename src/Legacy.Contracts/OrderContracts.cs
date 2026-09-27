using System.Runtime.Serialization;

namespace Legacy.Contracts;

// These are the DATA contracts of the legacy ERP's SOAP service. They are plain
// [DataContract] types (System.Runtime.Serialization), so the SAME types are shared by the
// CoreWCF host (server) and the System.ServiceModel client in the facade — exactly as if the
// client had been generated from the legacy WSDL. Only the [ServiceContract] interface differs
// between the two sides (CoreWCF attributes on the server, System.ServiceModel on the client),
// so that interface is declared separately on each side; the payload shapes below are shared.

[DataContract(Namespace = "http://legacy-erp.example.com/orders")]
public sealed class OrderDto
{
    [DataMember(Order = 0)] public string OrderNumber { get; set; } = "";
    [DataMember(Order = 1)] public string CustomerCode { get; set; } = "";
    [DataMember(Order = 2)] public string Status { get; set; } = "";
    [DataMember(Order = 3)] public decimal Total { get; set; }
    [DataMember(Order = 4)] public DateTime PlacedOnUtc { get; set; }
    [DataMember(Order = 5)] public List<OrderLineDto> Lines { get; set; } = new();
}

[DataContract(Namespace = "http://legacy-erp.example.com/orders")]
public sealed class OrderLineDto
{
    [DataMember(Order = 0)] public string Sku { get; set; } = "";
    [DataMember(Order = 1)] public string Description { get; set; } = "";
    [DataMember(Order = 2)] public int Quantity { get; set; }
    [DataMember(Order = 3)] public decimal UnitPrice { get; set; }
}

[DataContract(Namespace = "http://legacy-erp.example.com/orders")]
public sealed class StockLevelDto
{
    [DataMember(Order = 0)] public string Sku { get; set; } = "";
    [DataMember(Order = 1)] public string Warehouse { get; set; } = "";
    [DataMember(Order = 2)] public int Available { get; set; }
    [DataMember(Order = 3)] public int Reserved { get; set; }
}
