using System.Globalization;
using System.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using Google.Protobuf;
using Legacy.Contracts;
using Modern.Facade.Grpc;

namespace Serialization.Benchmarks;

/// <summary>
/// Quantifies the migration's payoff: serializing the same order as legacy SOAP/XML
/// (<see cref="DataContractSerializer"/>) vs Protobuf. Measures both wall-time and bytes on the
/// wire, so "gRPC for internal traffic" is a measured decision, not a slogan.
/// </summary>
[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private readonly DataContractSerializer _xml = new(typeof(OrderDto));
    private OrderDto _dto = default!;
    private Order _proto = default!;
    private byte[] _xmlBytes = default!;
    private byte[] _protoBytes = default!;

    [GlobalSetup]
    public void Setup()
    {
        _dto = new OrderDto
        {
            OrderNumber = "ORD-1001", CustomerCode = "ACME", Status = "SHIPPED",
            Total = 249.90m, PlacedOnUtc = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            Lines = Enumerable.Range(0, 10).Select(i => new OrderLineDto
            {
                Sku = $"SKU-{i}", Description = $"Item number {i}", Quantity = i + 1, UnitPrice = 9.99m + i
            }).ToList()
        };

        _proto = new Order
        {
            OrderNumber = _dto.OrderNumber, CustomerCode = _dto.CustomerCode, Status = _dto.Status,
            Total = _dto.Total.ToString(CultureInfo.InvariantCulture),
            PlacedOnUtc = _dto.PlacedOnUtc.ToString("O", CultureInfo.InvariantCulture)
        };
        foreach (var l in _dto.Lines)
        {
            _proto.Lines.Add(new OrderLine
            {
                Sku = l.Sku, Description = l.Description, Quantity = l.Quantity,
                UnitPrice = l.UnitPrice.ToString(CultureInfo.InvariantCulture)
            });
        }

        _xmlBytes = XmlSerialize();
        _protoBytes = _proto.ToByteArray();
    }

    // Payload sizes are reported once (they are constant), alongside the timing table.
    [Benchmark(Description = "SOAP/XML serialize")]
    public byte[] XmlSerialize()
    {
        using var ms = new MemoryStream();
        _xml.WriteObject(ms, _dto);
        return ms.ToArray();
    }

    [Benchmark(Description = "Protobuf serialize")]
    public byte[] ProtobufSerialize() => _proto.ToByteArray();

    [Benchmark(Description = "SOAP/XML deserialize")]
    public OrderDto XmlDeserialize()
    {
        using var ms = new MemoryStream(_xmlBytes);
        return (OrderDto)_xml.ReadObject(ms)!;
    }

    [Benchmark(Description = "Protobuf deserialize")]
    public Order ProtobufDeserialize() => Order.Parser.ParseFrom(_protoBytes);

    public int XmlBytes => _xmlBytes.Length;
    public int ProtoBytes => _protoBytes.Length;
}
