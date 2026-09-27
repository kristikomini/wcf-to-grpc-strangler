using BenchmarkDotNet.Running;
using Serialization.Benchmarks;

// Print the constant payload sizes first (bytes on the wire), then run the timing benchmarks.
var sizes = new SerializationBenchmarks();
sizes.Setup();
Console.WriteLine($"Payload sizes — SOAP/XML: {sizes.XmlBytes} bytes · Protobuf: {sizes.ProtoBytes} bytes");

BenchmarkRunner.Run<SerializationBenchmarks>();
