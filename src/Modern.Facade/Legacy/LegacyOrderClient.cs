using System.ServiceModel;
using Legacy.Contracts;

namespace Modern.Facade.Legacy;

/// <summary>
/// Talks to the legacy SOAP ERP. The <see cref="ChannelFactory{T}"/> is created once (it is
/// thread-safe and expensive to build) and a lightweight channel is opened per call and closed
/// deterministically — the standard WCF client lifetime, avoiding the classic "reuse a faulted
/// channel" leak. Resilience (retry / circuit breaker / timeout) is applied around these calls by
/// the pipeline in <c>Program.cs</c>, not baked in here.
/// </summary>
public sealed class LegacyOrderClient
{
    private readonly ChannelFactory<IOrderServiceClient> _factory;

    public LegacyOrderClient(IConfiguration configuration)
    {
        string endpoint = configuration["Legacy:Endpoint"] ?? "http://localhost:5080/soap/orders";
        var binding = new BasicHttpBinding { MaxReceivedMessageSize = 4 * 1024 * 1024 };
        _factory = new ChannelFactory<IOrderServiceClient>(binding, new EndpointAddress(endpoint));
    }

    public Task<OrderDto?> GetOrderAsync(string orderNumber) =>
        CallAsync(channel => channel.GetOrderAsync(orderNumber));

    public Task<List<OrderDto>> ListOrdersByCustomerAsync(string customerCode) =>
        CallAsync(channel => channel.ListOrdersByCustomerAsync(customerCode));

    public Task<StockLevelDto> GetStockLevelAsync(string sku, string warehouse) =>
        CallAsync(channel => channel.GetStockLevelAsync(sku, warehouse));

    private async Task<T> CallAsync<T>(Func<IOrderServiceClient, Task<T>> call)
    {
        var channel = _factory.CreateChannel();
        var clientChannel = (IClientChannel)channel;
        try
        {
            T result = await call(channel).ConfigureAwait(false);
            clientChannel.Close();
            return result;
        }
        catch
        {
            clientChannel.Abort(); // never return a faulted channel to the (non-pooled) GC
            throw;
        }
    }
}
