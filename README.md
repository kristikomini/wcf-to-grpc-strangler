# WCF SOAP → gRPC / REST — a strangler-fig migration on .NET

Thousands of Italian enterprises run critical business logic on **WCF SOAP** services. You are not
paid to rewrite them overnight — you are paid to **keep the old contract alive** while exposing
modern, high-performance endpoints to new consumers, and to move capabilities across **one at a
time, reversibly**. This repo is exactly that, built on .NET 10.

Target market: consultancies (Reply, Avanade), banking / insurance, and industrial-ERP integration
(Milano, Bologna, Modena).

> Follows the shared [engineering standards](../ENGINEERING-STANDARDS.md). The architecture decision
> is written down in [`docs/adr/ADR-001-strangler-migration.md`](docs/adr/ADR-001-strangler-migration.md).

## The shape

```
old SOAP clients ─┐
                  ▼
        Legacy.ErpHost (CoreWCF)  ── SOAP 1.1 / WSDL, on .NET 10, no IIS
                  ▲
                  │  System.ServiceModel client, wrapped in a Polly pipeline
                  │  (retry → circuit breaker → timeout)
        Modern.Facade ───────────────┬──────────────► gRPC (Protobuf)   — internal, high-speed
                  │                   └──────────────► Minimal API (OpenAPI 3) — external partners
                  │
        Strangler switch: stock reads → modern read model OR legacy proxy (per capability)
```

| Project | Role |
|---------|------|
| `Legacy.Contracts` | Shared `[DataContract]` SOAP payloads (identical on both sides). |
| `Legacy.ErpHost` | The old ERP, kept alive over SOAP with **CoreWCF** — no IIS, no .NET Framework. |
| `Modern.Facade` | gRPC + REST facade; calls legacy via a resilient client; routes migrated capabilities. |
| `benchmarks/` | **BenchmarkDotNet**: SOAP/XML vs Protobuf serialization. |
| `tests/` | Integration tests: real legacy host + facade, proxy / routing / fault translation. |

## What it demonstrates (CV bullets)

- Kept a legacy **WCF SOAP** contract running on **.NET 10 via CoreWCF** (no IIS / .NET Framework)
  and put a **gRPC + OpenAPI** facade in front — the strangler-fig pattern, with capabilities moved
  across one at a time behind a config switch, reversibly and without changing callers.
- Guarded every call to the legacy backend with a **Polly v8** pipeline (retry → circuit breaker →
  timeout); translated SOAP faults, open circuits and timeouts into **RFC 7807** responses / clean
  gRPC statuses instead of leaking failures.
- Quantified the migration's payoff with **BenchmarkDotNet** (SOAP/XML vs Protobuf serialization),
  so "gRPC internally" is a measured decision.

## Run it

```bash
# terminal 1 — the legacy SOAP ERP (CoreWCF), on :5080
dotnet run --project src/Legacy.ErpHost --urls http://localhost:5080

# terminal 2 — the modern facade (gRPC + REST + OpenAPI)
dotnet run --project src/Modern.Facade

# REST (proxied to legacy):
curl http://localhost:5199/api/orders/ORD-1001
curl "http://localhost:5199/api/stock/PALLET-A?warehouse=MAIN"   # served_by: legacy-soap
# flip Strangler:StockMigrated=true → same call now served_by: modern
```

## Course / market notes

Senior C# embedded here: CoreWCF hosting, `System.ServiceModel` clients and channel lifetime,
Protobuf vs XML, Polly resilience pipelines, `IExceptionHandler` + `ProblemDetails`, OpenAPI, and
Central Package Management with a strict (warnings-as-errors) build.
