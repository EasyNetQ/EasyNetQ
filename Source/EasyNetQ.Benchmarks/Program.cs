using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;

// Broker-backed benchmarks run only when EASYNETQ_BENCH_RABBIT is set (see EndToEndRabbitBenchmarks)
var rabbitEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EASYNETQ_BENCH_RABBIT"));

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddExporter(JsonExporter.Full)
    .AddFilter(new SimpleFilter(benchmark => rabbitEnabled || !benchmark.Descriptor.Type.Name.Contains("Rabbit")));

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
