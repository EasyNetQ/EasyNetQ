# Phase 0 — Baseline (8.x pipeline, before any redesign)

Measured on the unmodified 8.x hot paths after the Phase 0 hygiene changes (netstandard2.0 dropped, .NET 10 SDK,
Nullable on). This is the reference every later phase is compared against. Earlier numbers taken on .NET 8 with a
`Task.FromResult`-allocating handler are in `pre-phase0-consume-and-serializer.md`; they are not directly comparable
(different runtime, handler allocated per call).

``` ini
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a
```

Run with `dotnet run --project Source/EasyNetQ.Benchmarks -c Release -f net10.0 -- --filter '*'`
(`EASYNETQ_BENCH_RABBIT=host=localhost:5673` against an isolated `masstransit/rabbitmq` container for the end-to-end suite).

## Allocation gates (`Source/EasyNetQ.AllocationTests`, `Ceilings.cs`)

| Scenario | B / iteration |
|---|---:|
| Consume small / medium / large | 208 / 1776 / 13664 |
| Publish (advanced path) small | 408 |
| Publish (PubSub path) small | 464 |
| EventBus publish, 0 / 1 subscribers | 0 / 0 |

## Consume pipeline

Error-strategy middleware → interceptor middleware → `DefaultTypeNameSerializer` + `SystemTextJsonSerializerV2` +
`MessageFactory` → `HandlerCollection.GetHandler` → no-op handler returning a cached task.

| Method         | Mean       | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|--------------- |-----------:|---------:|---------:|-------:|-------:|----------:|
| Consume_Small  |   237.2 ns |  1.58 ns |  1.32 ns | 0.0248 |      - |     208 B |
| Consume_Medium |   702.6 ns |  4.44 ns |  3.94 ns | 0.2117 | 0.0010 |    1776 B |
| Consume_Large  | 9,739.4 ns | 58.11 ns | 51.51 ns | 1.6327 | 0.0610 |   13664 B |

Breakdown for the small message: STJ deserialize is ~96 ns / 64 B (see serializer table), `Message<T>` is 144 B
(see MessageFactory table). Everything else in the pipeline is currently allocation-free on .NET 10
(`Enumerable.ToArray()` over an empty interceptor set returns `Array.Empty`, synchronous async completions don't box).
**Framework overhead to remove: ~140 ns and 144 B per consumed message.**

## Publish pipeline (to the transport boundary)

Serialization strategy → interceptor middleware → `MessageProperties.CopyTo(new BasicProperties())`. The PubSub path adds
conventions (attribute lookup), delivery-mode strategy and `PublishConfiguration`.

| Method          | Mean       | Error    | StdDev   | Gen0   | Allocated |
|---------------- |-----------:|---------:|---------:|-------:|----------:|
| Advanced_Small  |   414.7 ns |  1.33 ns |  1.11 ns | 0.0486 |     408 B |
| Advanced_Medium |   671.1 ns |  4.28 ns |  3.80 ns | 0.0858 |     720 B |
| Advanced_Large  | 5,494.1 ns | 29.70 ns | 26.33 ns | 0.0839 |     720 B |
| PubSub_Small    |   431.7 ns |  1.88 ns |  1.75 ns | 0.0486 |     408 B |
| PubSub_Medium   |   676.8 ns |  3.55 ns |  3.32 ns | 0.0858 |     720 B |
| PubSub_Large    | 5,492.1 ns | 23.59 ns | 22.06 ns | 0.0839 |     720 B |

The 408 B for a small publish = `Message<T>` (144 B) + `ArrayPooledMemoryStream` object + `BasicProperties` +
correlation-id `Guid.ToString()`; serialization itself is 48 B. The PubSub layer costs ~17 ns and 0 B on top
(attribute lookups are cached).

## Serializers

| Method                       | Size   | Mean         | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|----------------------------- |------- |-------------:|----------:|----------:|-------:|-------:|----------:|
| SystemTextJson_Serialize     | Small  |     82.25 ns |  0.237 ns |  0.210 ns | 0.0057 |      - |      48 B |
| SystemTextJsonV2_Serialize   | Small  |     83.16 ns |  0.287 ns |  0.268 ns | 0.0057 |      - |      48 B |
| Newtonsoft_Serialize         | Small  |    230.34 ns |  0.843 ns |  0.747 ns | 0.3135 | 0.0026 |    2624 B |
| SystemTextJson_Deserialize   | Small  |    100.46 ns |  0.894 ns |  0.836 ns | 0.0076 |      - |      64 B |
| SystemTextJsonV2_Deserialize | Small  |     96.05 ns |  0.806 ns |  0.754 ns | 0.0076 |      - |      64 B |
| Newtonsoft_Deserialize       | Small  |    273.28 ns |  1.765 ns |  1.651 ns | 0.4358 | 0.0052 |    3648 B |
| SystemTextJson_Serialize     | Medium |    324.29 ns |  1.884 ns |  1.762 ns | 0.0429 |      - |     360 B |
| SystemTextJsonV2_Serialize   | Medium |    324.26 ns |  1.847 ns |  1.727 ns | 0.0429 |      - |     360 B |
| Newtonsoft_Serialize         | Medium |    733.96 ns |  3.146 ns |  2.943 ns | 0.3462 | 0.0029 |    2896 B |
| SystemTextJson_Deserialize   | Medium |    563.44 ns |  3.717 ns |  3.295 ns | 0.1945 | 0.0010 |    1632 B |
| SystemTextJsonV2_Deserialize | Medium |    561.31 ns |  2.779 ns |  2.321 ns | 0.1945 | 0.0010 |    1632 B |
| Newtonsoft_Deserialize       | Medium |  1,114.35 ns |  4.940 ns |  4.379 ns | 0.5798 | 0.0076 |    4856 B |
| SystemTextJson_Serialize     | Large  |  5,350.24 ns | 20.049 ns | 18.754 ns | 0.0381 |      - |     360 B |
| SystemTextJsonV2_Serialize   | Large  |  5,212.85 ns | 23.557 ns | 22.036 ns | 0.0381 |      - |     360 B |
| Newtonsoft_Serialize         | Large  | 12,237.87 ns | 23.289 ns | 20.645 ns | 1.3885 | 0.0153 |   11640 B |
| SystemTextJson_Deserialize   | Large  |  9,519.27 ns | 36.846 ns | 34.466 ns | 1.6022 | 0.0610 |   13520 B |
| SystemTextJsonV2_Deserialize | Large  |  9,479.64 ns | 35.774 ns | 31.713 ns | 1.6022 | 0.0610 |   13520 B |
| Newtonsoft_Deserialize       | Large  | 18,688.12 ns | 99.775 ns | 93.330 ns | 2.4414 | 0.0916 |   20424 B |

## Type-name serializers (cached, steady state)

| Method                      | Mean      | Error     | StdDev    | Allocated |
|---------------------------- |----------:|----------:|----------:|----------:|
| Default_Serialize           |  2.657 ns | 0.0145 ns | 0.0136 ns |         - |
| Default_Deserialize         |  9.651 ns | 0.0237 ns | 0.0221 ns |         - |
| Default_Deserialize_Generic | 20.450 ns | 0.0507 ns | 0.0424 ns |         - |
| Legacy_Serialize            |  2.656 ns | 0.0117 ns | 0.0109 ns |         - |
| Legacy_Deserialize          |  9.342 ns | 0.0187 ns | 0.0166 ns |         - |

Cheap once cached — the problem with these is AOT/trim safety (`Assembly.Load`, `Type.GetType`, `MakeGenericType`),
not speed. The Phase 2 registry must match ~10 ns per lookup.

## MessageFactory

| Method                        | Mean     | Error    | StdDev   | Ratio | Gen0   | Allocated |
|------------------------------ |---------:|---------:|---------:|------:|-------:|----------:|
| Direct_New                    | 13.51 ns | 0.118 ns | 0.110 ns |  1.00 | 0.0172 |     144 B |
| MessageFactory_CreateInstance | 18.71 ns | 0.315 ns | 0.279 ns |  1.38 | 0.0172 |     144 B |

`Message<T>` is a 144 B object because it embeds `MessageProperties` by value; the reflection-compiled delegate adds
~5 ns. Both go away when the message becomes a slot on the pooled context.

## EventBus

| Method                | Mean     | Error    | StdDev   | Allocated |
|---------------------- |---------:|---------:|---------:|----------:|
| Publish_NoSubscribers | 16.43 ns | 0.067 ns | 0.063 ns |         - |
| Publish_OneSubscriber | 37.51 ns | 0.141 ns | 0.132 ns |         - |

Allocation-free on .NET 10 (synchronous completion); ~50–90 ns per delivered message across the 2–3 publishes.

## End to end (real broker, `masstransit/rabbitmq` in Docker, no publisher confirms)

| Method            | Mean       | Error     | StdDev     | Median     | Gen0   | Allocated |
|------------------ |-----------:|----------:|-----------:|-----------:|-------:|----------:|
| Publish           |   3.528 μs | 0.1485 μs |  0.4354 μs |   3.306 μs | 0.1144 |     985 B |
| PublishAndConsume | 478.547 μs | 8.7142 μs | 12.2161 μs | 479.705 μs |      - |    6992 B |

Whole-stack allocation per published message is ~1 KB (EasyNetQ + RabbitMQ.Client framing); a full round trip
allocates ~7 KB, dominated by RabbitMQ.Client and the async machinery around channel dispatch and consumer delivery.

## Native AOT (`Source/EasyNetQ.Examples.Aot`, `dotnet publish -r osx-arm64 -p:PublishAot=true`)

- Publishes successfully; **22 trim/AOT warnings, all in EasyNetQ** (`DefaultTypeNameSerializer` ×5, `MessageFactory` ×1,
  `SystemTextJsonSerializer`/`V2` ×8, `MessagePropertiesConverter` ×4, `JsonHeaderExtensions` ×4).
  RabbitMQ.Client 7.2.1 produces none.
- At runtime the binary connects, declares the queue and consumer, then fails on the first publish:
  `InvalidOperationException: Reflection-based serialization has been disabled for this application.
  Either use the source generator APIs or explicitly configure the 'JsonSerializerOptions.TypeInfoResolver' property.`
  (`ISerializer` is `Type`/`object` based; no `JsonSerializerContext` exists.)
