# Phase 2 — Message type registry, generic serializer, typed dispatch

Branch `v9/phase-2-registry`. Same machine/runtime as Phases 0–1 (Apple M3 Max, .NET 10.0.11, BenchmarkDotNet 0.15.8).
Δ columns are against `phase1.md`.

## What changed on the hot paths

- `MessageTypeDescriptor<T>` / `IMessageTypeRegistry` replace `MessageFactory` (expression-compiled envelope
  activator) and the per-consume `ITypeNameSerializer.Deserialize` → `Type` → handler-dictionary chain. The wire
  name is computed once per type by the DI'd `ITypeNameSerializer` (8.x + legacy parity for free) and cached on the
  descriptor; `RuntimeDescriptorFactory` is the one remaining reflection site, used only for runtime `Type`s until
  the Phase 3 generator emits closed registrations.
- Consume dispatch is typed end to end: `ResolveMessageTypeStep` → `ResolveHandlerStep` → `SelectSerializerStep` →
  `DeserializeStep` → dispatch terminal. Handlers are `MessageHandler<T>(T body, ConsumeContext)` returning
  `ValueTask<AckDecision>`; the `Message<T>` envelope (144 B, embeds `MessageProperties`) is no longer materialised
  on consume, publish (PubSub/SendReceive) or RPC — only the legacy `IMessageHandler<IMessage<T>>` overloads build it.
- `IMessageSerializer` is generic (`Serialize<T>`/`Deserialize<T>` against the descriptor); the STJ implementation
  caches `JsonTypeInfo<T>` in `descriptor.SerializerState`, so steady-state serialization does no options lookup.
  A `JsonSerializerContext` ctor overload prepares AOT. User `ISerializer`s are auto-wrapped by
  `LegacyMessageSerializerAdapter`.
- `HandlerTable` resolves handlers by wire name with a cached polymorphic fallback. (During integration testing the
  first cut cached polymorphic resolutions into the same map used for descriptor lookup, so a derived-type message
  arriving after a base-type resolution deserialized as the base type — fixed by splitting the registration map from
  the resolution cache; `HandlerTableTests` pin the behaviour.)
- RabbitMQ.Client alignment slice: `MultiPersistentChannelDispatcher` rewritten on `System.Threading.Channels`
  (its `DisposeAsync` previously leaked N−1 pooled channels), `SinglePersistentChannelDispatcher` `GetOrAdd` race
  fixed via `Lazy<T>`, `DefaultConsumeErrorStrategy` publishes through the persistent-channel dispatcher instead of
  opening a channel per failed message, dead code removed (`NoopDefaultConsumer`, the pre-v7 "pipelining forbidden"
  verdict, the `IRecoverable` cast/throw). **Behaviour change:** `ConsumerDispatchConcurrency` now defaults to 1
  (ordered delivery) instead of the prefetch count; opt in via `consumerDispatcherConcurrency=N`.

## Allocation gates (`Ceilings.cs`)

| Scenario | Phase 0 | Phase 1 | Phase 2 |
|---|---:|---:|---:|
| Consume small / medium / large | 208 / 1776 / 13664 B | 208 / 1776 / 13664 B | **64 / 1632 / 13520 B** |
| Publish advanced small | 408 B | 408 B | **264 B** |
| Publish pubsub small | 464 B | 464 B | **320 B** |
| Pipeline plumbing, no-op terminal | — | 0 B | 0 B |
| PropertyBag get/set / inherited lookup | — | 0 B | 0 B |

Consume small is now exactly the deserialized message object (STJ's own 64 B) — the framework adds nothing.
The remaining publish bytes are `ArrayPooledMemoryStream` + `BasicProperties` + the correlation-id string, i.e.
transport-boundary costs owned by Phases 4/5.

## Consume pipeline

| Method         | Mean       | Δ vs phase 1 | Allocated | Δ |
|--------------- |-----------:|-------------:|----------:|---:|
| Consume_Small  |   185.9 ns | −9 %         |    **64 B** | −144 B |
| Consume_Medium |   648.1 ns | −4 %         |  **1632 B** | −144 B |
| Consume_Large  | 9,666.0 ns | −4 %         | **13520 B** | −144 B |

STJ alone deserializes the small payload in 101.5 ns / 64 B, so the framework's share of `Consume_Small` is
~84 ns and **0 bytes** — the Phase 2 exit target ("consume = the deserialized object and nothing else").

## Publish pipeline (to the transport boundary)

| Method          | Mean       | Δ vs phase 1 | Allocated | Δ |
|---------------- |-----------:|-------------:|----------:|---:|
| Advanced_Small  |   370.4 ns | −2 %         |  **264 B** | −144 B |
| Advanced_Medium |   623.2 ns |  0 %         |  **576 B** | −144 B |
| Advanced_Large  | 5,607.2 ns | −1 %         |  **576 B** | −144 B |
| PubSub_Small    |   393.0 ns | −7 %         |  **264 B** | −144 B |
| PubSub_Medium   |   635.4 ns | −6 %         |  **576 B** | −144 B |
| PubSub_Large    | 5,457.6 ns | −5 %         |  **576 B** | −144 B |

## Registry vs 8.x type-name lookup

Steady-state wire-name round trips (once per published and consumed message in 8.x; in 9.x the consume path hits
the descriptor cache instead).

| Method                      | Mean      | Allocated |
|---------------------------- |----------:|----------:|
| Default_Serialize           |  2.65 ns  |         - |
| Default_Deserialize         |  9.66 ns  |         - |
| Default_Deserialize_Generic | 20.52 ns  |         - |
| Legacy_Serialize            |  2.65 ns  |         - |
| Legacy_Deserialize          |  9.28 ns  |         - |

Unchanged from Phase 0 — these serializers now run **once per type** (when the registry first computes a wire name)
instead of once per message; the per-message cost is a single dictionary probe inside `ResolveMessageTypeStep`,
already included in the consume totals above.

## Envelope materialisation (legacy path only)

`descriptor.CreateMessage` replaces the expression-compiled `MessageFactory` for the remaining legacy
`IMessage`-based overloads; the typed path allocates no envelope at all.

| Method                   | Mean     | Ratio | Allocated |
|------------------------- |---------:|------:|----------:|
| Direct_New               | 13.47 ns |  1.00 |     144 B |
| Descriptor_CreateMessage | 13.68 ns |  1.02 |     144 B |

Parity with the expression-compiled `MessageFactory` it replaced, without `MakeGenericType`/`Expression.Compile`.

## End to end (real broker, no publisher confirms)

| Method            | Mean     | Δ vs phase 1 | Allocated | Δ |
|------------------ |---------:|-------------:|----------:|---:|
| Publish           | NA*      | —            | NA        | — |
| PublishAndConsume | 447.2 μs | −2 %         | **1.88 KB** | −72 % |

\* `Publish` (fire-and-forget at max rate into an exchange nobody consumes from) now sustains ~300k msg/s per
17 recorded iterations (3.0–4.0 μs/op, median ≈3.4 μs vs 3.85 μs in phase 1) — fast enough to overrun the local
Docker broker's ingest rate, so one publish eventually stalls in the client's outgoing-frame writer past EasyNetQ's
10 s default operation timeout and BenchmarkDotNet voids the run. Reproduced across three runs (auto-sized and
pinned invocation counts); the broker reports no alarms, and `PublishAndConsume` — which paces itself on the
round trip — is the meaningful end-to-end number. Phase 4's confirmed-publish benchmark replaces this scenario
with rate-limited confirms. As with phase 1, treat the e2e allocation column as indicative (it includes
RabbitMQ.Client's writer/reader threads); the deterministic gates are the contract.

## Native AOT (`EasyNetQ.Examples.Aot`)

- ILC trim/AOT warnings: **16** (Phases 0–1: 22). Gone: `MessageFactory` (expression compile, ×1) and the old
  `Type`-based `SystemTextJsonSerializer`/`V2` sites (×8). New: `RuntimeDescriptorFactory` (`MakeGenericType`, ×1)
  and `SystemTextJsonMessageSerializer`'s reflection-options ctor (×2) — both removed by the Phase 3 generator
  (closed generic registrations + `JsonSerializerContext`). Remaining: `DefaultTypeNameSerializer` ×5,
  `MessagePropertiesConverter` ×4, `JsonHeaderExtensions` ×4. RabbitMQ.Client 7.2.1 still produces none.
