using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;

namespace Legacy.ErpHost;

/// <summary>
/// Builds the legacy CoreWCF SOAP host. Extracted from <c>Program.cs</c> so the integration tests
/// can start the very same host on a real port and drive the facade against it — the SOAP path is
/// exercised over a real socket, not mocked.
/// </summary>
public static class LegacyErpServer
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddServiceModelServices();
        builder.Services.AddServiceModelMetadata();

        var app = builder.Build();
        app.UseServiceModel(serviceBuilder =>
        {
            serviceBuilder.AddService<OrderService>(options =>
                options.DebugBehavior.IncludeExceptionDetailInFaults = true);
            serviceBuilder.AddServiceEndpoint<OrderService, IOrderService>(
                new BasicHttpBinding(), "/soap/orders");
            var metadata = app.Services.GetRequiredService<ServiceMetadataBehavior>();
            metadata.HttpGetEnabled = true;
        });
        return app;
    }
}
