using System.ServiceModel;
using Modern.Facade;
using Modern.Facade.Api;
using Modern.Facade.Grpc;
using Modern.Facade.Legacy;
using Modern.Facade.Stock;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<StranglerOptions>(builder.Configuration.GetSection(StranglerOptions.Section));
builder.Services.AddSingleton<LegacyOrderClient>();
builder.Services.AddSingleton<ResilientLegacyGateway>();
builder.Services.AddSingleton<IStockReadModel, ModernStockReadModel>();

// The resilience pipeline that guards every call to the legacy SOAP backend.
// Order matters: retry wraps circuit-breaker wraps timeout — a per-attempt timeout, transient
// faults retried with jittered backoff, and the breaker trips if the backend is genuinely down.
builder.Services.AddResiliencePipeline(ResilientLegacyGateway.PipelineKey, pipeline =>
{
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<CommunicationException>()
                .Handle<TimeoutRejectedException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<CommunicationException>()
                .Handle<TimeoutRejectedException>(),
            FailureRatio = 0.5,
            MinimumThroughput = 8,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(15)
        })
        .AddTimeout(TimeSpan.FromSeconds(5));
});

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();                       // OpenAPI 3 doc at /openapi/v1.json (external partners)
app.MapGrpcService<OrderApiService>();  // high-speed internal contract
app.MapOrderEndpoints();                // partner-facing REST
app.MapGet("/", () =>
    "WCF→gRPC strangler facade. gRPC service: orders.OrderApi · REST: /api/* · OpenAPI: /openapi/v1.json");

app.Run();

// Exposed so the integration tests can spin the facade up with WebApplicationFactory.
public partial class Program;
