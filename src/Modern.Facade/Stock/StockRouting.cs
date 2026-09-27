using Legacy.Contracts;

namespace Modern.Facade.Stock;

/// <summary>
/// The strangler routing switch. When <see cref="StockMigrated"/> is true, stock reads are served
/// by the modern read model; when false, they still proxy to the legacy SOAP ERP. Flipping this
/// flag is how a single capability is moved off the legacy system without touching callers — the
/// essence of the strangler-fig pattern.
/// </summary>
public sealed class StranglerOptions
{
    public const string Section = "Strangler";
    public bool StockMigrated { get; set; }
}

/// <summary>The modern side of the migrated capability. In this repo it is an in-memory read model
/// (persistence is out of scope here — the batch/order repos cover EF Core); the point is the
/// <i>routing</i>, not the storage.</summary>
public interface IStockReadModel
{
    StockLevelDto Get(string sku, string warehouse);
}

public sealed class ModernStockReadModel : IStockReadModel
{
    public StockLevelDto Get(string sku, string warehouse)
    {
        int seed = Math.Abs(string.GetHashCode(sku, StringComparison.Ordinal));
        return new StockLevelDto
        {
            Sku = sku,
            Warehouse = string.IsNullOrWhiteSpace(warehouse) ? "MAIN" : warehouse,
            // A deliberately different formula from the legacy service, so a test can prove WHICH
            // side answered — the modern read model, or the legacy proxy.
            Available = seed % 400,
            Reserved = seed % 40
        };
    }
}
