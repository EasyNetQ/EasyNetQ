# Phase 3 — Source generator, reflection-free hot paths, LoggerMessage logging

Branch `v9/phase-3-generator`. Same machine/runtime as earlier phases (Apple M3 Max, .NET 10.0.11, BenchmarkDotNet 0.15.8).
Δ columns are against `phase2.md`. This phase is about removing reflection and logging overhead, not allocations —
the allocation gates are unchanged and still hold.

## The source generator (`EasyNetQ.Generators`)

- `MessagingModuleGenerator` (incremental) harvests message types from four sources: closed generic type arguments
  at EasyNetQ call sites (`PublishAsync<T>`, `SubscribeAsync<T>`, `RequestAsync<TReq,TRsp>`, `SendAsync<T>`,
  `ConsumeAsync<T>`, `FuturePublishAsync<T>`, `Handle<T>`, …), `IConsume<T>`/`IConsumeAsync<T>` implementations,
  `[Queue]`/`[Exchange]`/`[DeliveryMode]`-annotated types, and `[assembly: EasyNetQMessages(typeof(...))]` opt-ins.
- Emits `{Assembly}.EasyNetQ.Generated.MessagingModule`: an `IMessageTypeRegistryInitializer` with a closed
  `registry.GetOrAdd<T>()` per discovered type (AOT-visible instantiations, no runtime reflection), registered via
  `IEasyNetQModule`; C# interceptors on every `AddEasyNetQ(...)` call site register modules automatically
  (`AddGeneratedModules()` is the manual fallback), and modules compose across assemblies at compile time through
  `[assembly: EasyNetQModule]` metadata — no runtime scanning.
- **Deviation from the plan:** no `JsonSerializerContext` is emitted. Roslyn generators cannot see each other's
  output, so the System.Text.Json generator would never fill a context we emit. Instead the DI factory combines any
  DI-registered `JsonSerializerContext`s and `SystemTextJsonMessageSerializer` gained an `IJsonTypeInfoResolver`
  ctor (`MakeReadOnly(populateMissingResolver: false)` — the reflection-resolver warning disappears when a context
  is supplied). AOT users declare one context class and pass it to `UseSystemTextJson(context)`.
- 7 generator tests run the real generator against in-memory compilations referencing the real EasyNetQ assembly,
  including a two-assembly composition test (contracts assembly → host) and interceptor compilation.

## Reflection removed from the hot paths

| Site | 8.x/Phase 2 | Phase 3 |
|---|---|---|
| `[Exchange]`/`[Queue]`/`[DeliveryMode]` reads (`Conventions`, `MessageDeliveryModeStrategy`) | per publish/subscribe | once per type, stored on the `MessageTypeDescriptor` (attributes are the runtime fallback; generated registrations pre-empt them) |
| Connection-string parsing | `Expression.Compile` per key **per parse** (grammar), `CreateDelegate` per key per parse (AMQP) | span-based parsers, zero reflection; `Sprache/` vendored parser combinators deleted |
| `NonGeneric*Extensions` (PubSub/SendReceive/Scheduler) | `GetMethod` + `MakeGenericMethod` + `Expression.Compile` per type ×4 files | descriptor trampolines (`PublishViaAsync` etc. closed in `MessageTypeDescriptor<T>`); `[RequiresDynamicCode]` documents the runtime-`Type` entry points |
| `NonGenericRpcExtensions` | expression tree per request/response pair | one static bridge method + cached `CreateDelegate` per pair |
| `MessageVersionStack` | **uncached** `GetInterfaces`/`GetGenericTypeDefinition`/`IsSubclassOf` walk per serialized message and per publish | version chain cached per type |
| `MultipleExchangeDeclareStrategy` | **uncached** `GetInterfaces()` + bind loop per publish | whole declare+bind unit cached per (type, exchangeType) |
| `HandlerCollection.GetHandler` | **uncached** linear `IsAssignableFrom` scan per consumed message (legacy envelope path) | memoized, registration map kept separate from the resolution cache |
| `ReflectionHelpers` | attribute cache used per message | deleted |

`ReflectionBanTests` pins this: scans the compiled assembly's member references, bans `Expression.Compile` and
`Activator.CreateInstance` outright, and ratchets the remaining escape hatches (each with its documented reason:
`RuntimeDescriptorFactory`, type-name serializers, the non-generic bridges, AutoSubscriber — all bundle-destined
or generator-superseded).

## Logging

All ~40 `ILogger` call sites migrated to source-generated `[LoggerMessage]` extensions (`Internals/Log.cs`, 34
methods, stable EventIds grouped by subsystem: 1xx connection, 2xx channel, 3xx consumer, 4xx advanced bus, 5xx RPC,
6xx error strategy, 7xx infrastructure). Behavioural changes: the per-publish and per-delivery Debug logs are gone
(superseded by tracing in Phase 4), and the error strategy no longer base64-encodes the failed message body unless
`Error` is enabled.

## Allocation gates (`Ceilings.cs`)

Unchanged from Phase 2 and still green: consume small/medium/large 64 / 1632 / 13520 B, publish advanced/pubsub
small 264 / 320 B, plumbing 0 B.

## Benchmarks

No hot-path shape changed this phase, and the measured numbers confirm it — allocations are byte-identical to
Phase 2 everywhere and timings sit within run-to-run variance:

| Method | Phase 2 | Phase 3 | Allocated |
|---|---:|---:|---:|
| Consume_Small | 185.9 ns | 222.1 ns* | 64 B (=) |
| Consume_Medium | 648.1 ns | 684.1 ns | 1632 B (=) |
| Consume_Large | 9,666 ns | 10,243 ns | 13520 B (=) |
| Advanced_Small | 370.4 ns | 385.8 ns | 264 B (=) |
| PubSub_Small | 393.0 ns | 403.6 ns | 264 B (=) |
| Pipeline Consume_Noop | 46.6 ns | 47.4 ns (StdDev 8.7) | 0 B (=) |
| e2e PublishAndConsume | 447.2 μs / 1.88 KB | 470.2 μs / 1.88 KB | — |
| Registry/type-name/`CreateMessage` | — | reproduce Phase 2 within 1 ns | — |

\* This session's Phase 3 runs happened on a machine warmed by hours of builds/tests (two runs: 207 and 222 ns);
the plumbing-only `Consume_Noop` and the byte-exact allocation gates show no structural change. Phase 4's baseline
run re-measures on a cold machine. The e2e `Publish` scenario remains NA for the reason documented in `phase2.md`.

## Native AOT (`EasyNetQ.Examples.Aot`)

- Publishes successfully; ILC trim/AOT warnings: **18** (Phase 2: 16): `DefaultTypeNameSerializer` ×5,
  `RuntimeDescriptorFactory` ×1, and ×12 in the STJ serializer's converters/header extensions (the example still
  uses the reflection-resolver default `UseSystemTextJson()`; supplying a `JsonSerializerContext` removes the STJ
  set — that wiring is the Phase 7 exercise). `MakeGenericMethod`/`Expression.Compile` warnings from the
  NonGeneric* extensions are gone.
- The generator now runs in the example (analyzer project reference): `MessagingModule` + the `AddEasyNetQ`
  interceptor are emitted and compiled into the published binary. `PublishAot` moved from the CLI into the
  example's csproj — as a `-p:` global property it flowed into the netstandard2.0 generator project and broke the
  publish (NETSDK1207).
