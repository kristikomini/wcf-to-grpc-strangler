using Legacy.ErpHost;

// Hosts the legacy SOAP service on modern .NET with CoreWCF — no IIS, no .NET Framework 4.8.
// The build/wiring lives in LegacyErpServer so tests can start the identical host on a real port.
LegacyErpServer.Build(args).Run();
