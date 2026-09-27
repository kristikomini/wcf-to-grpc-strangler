# ADR-001 — Strangle the legacy SOAP ERP behind a modern facade

*Status: accepted · 2026-09-27*

## Context
A business capability (order and stock lookup) lives in a legacy ERP exposed only over **WCF SOAP**,
historically hosted on .NET Framework / IIS. It cannot be rewritten in one step: existing clients
depend on the exact SOAP contract, and the business will not accept a big-bang cutover. New
consumers, meanwhile, want a modern, high-performance API.

## Decision
Apply the **strangler-fig** pattern with three moves:

1. **Keep the SOAP contract alive on modern .NET with CoreWCF.** `Legacy.ErpHost` hosts the original
   `IOrderService` over `BasicHttpBinding` (SOAP 1.1) with published WSDL — no IIS, no .NET
   Framework. Old clients keep working, byte-for-byte, on a supported runtime.
2. **Put a modern facade in front.** `Modern.Facade` exposes the same capabilities twice: **gRPC**
   (Protobuf) for high-speed internal callers, and **ASP.NET Core Minimal APIs** (OpenAPI 3) for
   external partners. Internally it calls the legacy service through a generated-style
   `System.ServiceModel` client.
3. **Move one capability at a time behind a routing switch.** `Strangler:StockMigrated` decides,
   per capability, whether a read is served by the **modern** side or **proxied** to legacy. Callers
   never change; the switch moves the traffic. Every stock reply carries `served_by` so the routing
   is observable.

## Consequences
- **Resilience is mandatory, not optional.** All legacy calls go through one Polly pipeline
  (retry → circuit breaker → timeout); a slow or flapping legacy backend degrades to clean 5xx
  `ProblemDetails` / gRPC statuses instead of taking the facade down.
- **Contracts are duplicated by design.** The service interface exists twice — CoreWCF attributes on
  the server, System.ServiceModel on the client — exactly as if the client were generated from WSDL.
  The `[DataContract]` payloads are shared (`Legacy.Contracts`), because those types are identical on
  both sides.
- **Serialization has a measurable cost.** SOAP/XML vs Protobuf is quantified with BenchmarkDotNet
  (`benchmarks/`), so "gRPC for internal traffic" is a measured decision, not a slogan.
- **Migration is reversible per capability.** If a migrated read misbehaves, flip the switch back to
  the legacy proxy while it is fixed — no redeploy of callers.
