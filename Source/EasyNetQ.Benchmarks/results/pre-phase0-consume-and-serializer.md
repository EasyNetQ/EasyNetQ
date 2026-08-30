# EasyNetQ Benchmark Results

``` ini
BenchmarkDotNet v0.14.0, macOS 26.3 (25D125) [Darwin 25.3.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 8.0.24 (8.0.2426.7010), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.24 (8.0.2426.7010), Arm64 RyuJIT AdvSIMD
```

## Consume Pipeline

Full end-to-end consume hot path: error strategy middleware &rarr; interceptor middleware &rarr; deserialization (TypeNameSerializer + SystemTextJsonV2 + MessageFactory) &rarr; handler dispatch.

| Method         | Mean        | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|--------------- |------------:|----------:|----------:|-------:|-------:|----------:|
| Consume_Small  |    383.5 ns |   1.67 ns |   1.56 ns | 0.0334 |      - |     280 B |
| Consume_Medium |  1,257.8 ns |   8.36 ns |   7.82 ns | 0.2193 |      - |   1,848 B |
| Consume_Large  | 17,041.9 ns | 112.89 ns | 105.60 ns | 1.6174 | 0.0610 |  13,736 B |

### Payload sizes

- **Small** (2 fields): ~29 bytes JSON
- **Medium** (7 fields incl. list + dictionary): ~300 bytes JSON
- **Large** (7 fields + 50-item collection): ~5 KB JSON

### Key observations

- Small message consume takes **~384 ns** with **280 B** allocated per message
- Cost scales roughly linearly with payload: dominated by JSON deserialization
- Large messages trigger Gen1 GC collections due to 13.7 KB allocations

## Serializer Comparison

Comparing SystemTextJson (v1), SystemTextJson V2 (default), and Newtonsoft.Json across three payload sizes.

| Method                       | Size   | Mean        | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|----------------------------- |------- |------------:|---------:|---------:|-------:|-------:|----------:|
| SystemTextJson_Serialize     | Small  |    151.0 ns |  0.80 ns |  0.75 ns | 0.0238 |      - |     200 B |
| SystemTextJsonV2_Serialize   | Small  |    151.0 ns |  0.68 ns |  0.53 ns | 0.0238 |      - |     200 B |
| Newtonsoft_Serialize         | Small  |    351.8 ns |  1.80 ns |  1.69 ns | 0.3133 | 0.0010 |   2,624 B |
| SystemTextJson_Deserialize   | Small  |    148.1 ns |  0.38 ns |  0.36 ns | 0.0076 |      - |      64 B |
| SystemTextJsonV2_Deserialize | Small  |    148.1 ns |  0.49 ns |  0.46 ns | 0.0076 |      - |      64 B |
| Newtonsoft_Deserialize       | Small  |    429.2 ns |  4.72 ns |  4.41 ns | 0.4358 | 0.0019 |   3,648 B |
| SystemTextJson_Serialize     | Medium |    600.6 ns |  3.40 ns |  3.18 ns | 0.0610 |      - |     512 B |
| SystemTextJsonV2_Serialize   | Medium |    609.3 ns | 10.17 ns |  9.52 ns | 0.0610 |      - |     512 B |
| Newtonsoft_Serialize         | Medium |  1,117.6 ns |  5.61 ns |  5.25 ns | 0.3452 | 0.0019 |   2,896 B |
| SystemTextJson_Deserialize   | Medium |    980.3 ns |  6.47 ns |  5.73 ns | 0.1945 |      - |   1,632 B |
| SystemTextJsonV2_Deserialize | Medium |    986.6 ns |  8.66 ns |  8.10 ns | 0.1945 |      - |   1,632 B |
| Newtonsoft_Deserialize       | Medium |  1,790.0 ns |  4.59 ns |  4.07 ns | 0.5798 | 0.0038 |   4,856 B |
| SystemTextJson_Serialize     | Large  |  9,113.0 ns | 47.82 ns | 44.73 ns | 0.0610 |      - |     512 B |
| SystemTextJsonV2_Serialize   | Large  |  9,205.5 ns | 53.04 ns | 47.02 ns | 0.0610 |      - |     512 B |
| Newtonsoft_Serialize         | Large  | 18,564.7 ns | 48.38 ns | 42.89 ns | 1.2207 |      - |  11,640 B |
| SystemTextJson_Deserialize   | Large  | 16,806.9 ns | 98.04 ns | 81.87 ns | 1.5869 | 0.0610 |  13,520 B |
| SystemTextJsonV2_Deserialize | Large  | 16,732.1 ns | 73.65 ns | 68.89 ns | 1.5869 | 0.0610 |  13,520 B |
| Newtonsoft_Deserialize       | Large  | 31,849.6 ns | 66.68 ns | 62.37 ns | 2.4414 | 0.0610 |  20,424 B |

### Key observations

- **SystemTextJson V1 and V2 perform identically** across all payload sizes
- **Newtonsoft is ~2x slower** than SystemTextJson for both serialization and deserialization
- **Serialization allocations**: SystemTextJson allocates a fixed 200-512 B (pooled buffer) regardless of payload growth; Newtonsoft allocates 2.6-11.6 KB scaling with size
- **Deserialization allocations**: SystemTextJson allocates 64 B - 13.5 KB (dominated by the deserialized object); Newtonsoft adds 3.6-6.9 KB overhead on top
- For small messages, Newtonsoft allocates **57x more** on serialize (2,624 B vs 200 B)

## Running benchmarks

```bash
# Full run
dotnet run --project Source/EasyNetQ.Benchmarks -c Release -- --filter '*'

# Consume pipeline only
dotnet run --project Source/EasyNetQ.Benchmarks -c Release -- --filter '*ConsumePipelineBenchmarks*'

# Serializer comparison only
dotnet run --project Source/EasyNetQ.Benchmarks -c Release -- --filter '*SerializerBenchmarks*'

# Quick smoke test (dry run)
dotnet run --project Source/EasyNetQ.Benchmarks -c Release -- --filter '*' --job dry
```
