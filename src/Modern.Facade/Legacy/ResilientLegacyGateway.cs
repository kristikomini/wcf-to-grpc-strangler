using Legacy.Contracts;
using Polly.Registry;

namespace Modern.Facade.Legacy;

/// <summary>
/// The single choke point through which the facade calls the legacy ERP. Every call runs inside the
/// named <c>"legacy-soap"</c> resilience pipeline (retry → circuit breaker → timeout, configured in
/// <c>Program.cs</c>), so a slow or flapping legacy backend degrades gracefully instead of taking
/// the facade down with it.
/// </summary>
public sealed class ResilientLegacyGateway
{
    public const string PipelineKey = "legacy-soap";

    private readonly LegacyOrderClient _client;
    private readonly ResiliencePipelineProvider<string> _pipelines;

    public ResilientLegacyGateway(LegacyOrderClient client, ResiliencePipelineProvider<string> pipelines)
    {
        _client = client;
        _pipelines = pipelines;
    }

    public async Task<OrderDto?> GetOrderAsync(string orderNumber, CancellationToken ct)
    {
        var pipeline = _pipelines.GetPipeline(PipelineKey);
        return await pipeline.ExecuteAsync(
            async token => await _client.GetOrderAsync(orderNumber), ct);
    }

    public async Task<List<OrderDto>> ListOrdersByCustomerAsync(string customerCode, CancellationToken ct)
    {
        var pipeline = _pipelines.GetPipeline(PipelineKey);
        return await pipeline.ExecuteAsync(
            async token => await _client.ListOrdersByCustomerAsync(customerCode), ct);
    }

    public async Task<StockLevelDto> GetStockLevelAsync(string sku, string warehouse, CancellationToken ct)
    {
        var pipeline = _pipelines.GetPipeline(PipelineKey);
        return await pipeline.ExecuteAsync(
            async token => await _client.GetStockLevelAsync(sku, warehouse), ct);
    }
}
