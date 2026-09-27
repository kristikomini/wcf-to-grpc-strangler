using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Legacy.ErpHost;

// Hosts the legacy SOAP service on modern .NET with CoreWCF — no IIS, no .NET Framework 4.8.
// This is the "we cannot rewrite it yet" side of the strangler: the old contract stays available
// to old clients, byte-for-byte, while the modern facade is built in front of it.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

var app = builder.Build();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<OrderService>(options =>
        options.DebugBehavior.IncludeExceptionDetailInFaults = true);

    // BasicHttpBinding = SOAP 1.1, the lowest common denominator an old ERP client speaks.
    serviceBuilder.AddServiceEndpoint<OrderService, IOrderService>(
        new BasicHttpBinding(), "/soap/orders");

    // Publish WSDL at /soap/orders?wsdl so a client (or svcutil) can still generate a proxy.
    var metadata = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    metadata.HttpGetEnabled = true;
});

app.Run();
