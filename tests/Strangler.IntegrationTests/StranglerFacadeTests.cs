using System.Net;
using System.Net.Http.Json;
using Legacy.ErpHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Modern.Facade.Api;
using Modern.Facade.Grpc;
using Shouldly;

namespace Strangler.IntegrationTests;

/// <summary>
/// End-to-end: the real CoreWCF SOAP host runs on a live port, and the modern facade is driven
/// against it through its REST surface. Proves the SOAP proxy path, the not-found translation, and
/// the strangler routing switch — over a real socket, no mocks at the SOAP boundary.
/// </summary>
public sealed class StranglerFacadeTests : IAsyncLifetime
{
    private WebApplication _legacy = default!;
    private string _legacyEndpoint = default!;

    public async Task InitializeAsync()
    {
        _legacy = LegacyErpServer.Build(["--urls", "http://127.0.0.1:0"]);
        await _legacy.StartAsync();
        string baseUrl = _legacy.Urls.First();
        _legacyEndpoint = $"{baseUrl}/soap/orders";
    }

    public async Task DisposeAsync() => await _legacy.StopAsync();

    private WebApplicationFactory<OrderApiService> Facade(bool stockMigrated) =>
        new WebApplicationFactory<OrderApiService>().WithWebHostBuilder(b =>
        {
            b.UseSetting("Legacy:Endpoint", _legacyEndpoint);
            b.UseSetting("Strangler:StockMigrated", stockMigrated ? "true" : "false");
        });

    [Fact]
    public async Task GetOrder_is_proxied_from_the_legacy_SOAP_service()
    {
        using var facade = Facade(stockMigrated: false);
        using var client = facade.CreateClient();

        var order = await client.GetFromJsonAsync<OrderResponse>("/api/orders/ORD-1001");

        order.ShouldNotBeNull();
        order.CustomerCode.ShouldBe("ACME");
        order.Status.ShouldBe("SHIPPED");
        order.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Unknown_order_maps_to_404()
    {
        using var facade = Facade(stockMigrated: false);
        using var client = facade.CreateClient();

        var response = await client.GetAsync("/api/orders/NOPE-9999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stock_is_served_by_the_legacy_proxy_when_not_migrated()
    {
        using var facade = Facade(stockMigrated: false);
        using var client = facade.CreateClient();

        var stock = await client.GetFromJsonAsync<StockResponse>("/api/stock/PALLET-A?warehouse=MAIN");

        stock.ShouldNotBeNull();
        stock.ServedBy.ShouldBe("legacy-soap"); // proxied to the old ERP over SOAP
    }

    [Fact]
    public async Task Stock_is_served_by_the_modern_read_model_when_migrated()
    {
        using var facade = Facade(stockMigrated: true);
        using var client = facade.CreateClient();

        var stock = await client.GetFromJsonAsync<StockResponse>("/api/stock/PALLET-A?warehouse=MAIN");

        stock.ShouldNotBeNull();
        stock.ServedBy.ShouldBe("modern"); // same caller, same URL — routing flipped by the switch
    }
}
