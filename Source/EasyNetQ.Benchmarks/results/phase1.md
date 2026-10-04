# Phase 1 — Property bag, layered contexts, unified pipeline, `AckDecision`

Branch `v9/phase-1-pipeline`. Same machine/runtime as Phase 0 (Apple M3 Max, .NET 10.0.11, BenchmarkDotNet 0.15.8).
Δ columns are against `phase0.md`.

## What changed on the hot paths

- `ConsumeContext` / `PublishContext` are pooled classes rented per delivery/publish instead of `readonly record struct`s
  copied through every `with`; results (`AckDecision`) live on the context, so one `PipelineStep<TContext>` delegate
  shape serves every layer.
- Middleware is resolved **once at `Build`**: `ErrorHandlingMiddleware` captures the strategy + logger,
  `Consume/ProduceInterceptorMiddleware` capture the interceptor array (the 8.x pipeline resolved
  `IEnumerable<IProduceConsumeInterceptor>` from DI and called `ToArray()` per message).
- `AckStrategyAsync(IChannel, …)` delegates are gone; the transport maps `AckDecision` to `BasicAck/BasicNack` itself.
- Per-message `IEventBus` publishes (`DeliveredMessageEvent`, `AckEvent`, `PublishedMessageEvent`) removed.
- `MessageProperties.CopyTo` passes the headers dictionary by reference instead of copying it.
- `PersistentChannelDispatchOptions` is a `readonly record struct` (ordinal equality) instead of a class hashed with
  `StringComparer.InvariantCulture` on every publish.

## Allocation gates (`Ceilings.cs`)

| Scenario | Phase 0 | Phase 1 |
|---|---:|---:|
| Consume small / medium / large | 208 / 1776 / 13664 B | 208 / 1776 / 13664 B |
| Publish (advanced / pubsub) small | 408 / 464 B | 408 / 464 B |
| **Pipeline plumbing, no-op terminal** (new) | — | **0 B** |
| **PropertyBag get/set steady state** (new) | — | **0 B** |
| **Inherited property lookup via pooled context** (new) | — | **0 B** |

Totals are unchanged because the remaining bytes are `Message<T>` (144 B, embeds `MessageProperties`) plus the
deserialized payload on consume, and `Message<T>` + `ArrayPooledMemoryStream` + `BasicProperties` + the correlation-id
string on publish. Those are Phase 2's targets (typed descriptors, no `Message<T>`, pooled buffer writer).

## Consume pipeline

| Method         | Mean        | Δ vs phase 0 | Allocated |
|--------------- |------------:|-------------:|----------:|
| Consume_Small  |    203.7 ns | −14 %        |     208 B |
| Consume_Medium |    678.1 ns |  −3 %        |    1776 B |
| Consume_Large  | 10,053.7 ns |  +3 % (noise, STJ-dominated) | 13664 B |

## Pipeline plumbing alone (new)

Rent pooled context → `ErrorHandlingMiddleware` → `ConsumeInterceptorMiddleware` (0 interceptors) → no-op terminal → return.

| Method       | Mean     | Allocated |
|------------- |---------:|----------:|
| Consume_Noop | 53.04 ns |         - |

## Property bag / context hierarchy (new)

| Method                                | Mean       | Allocated |
|-------------------------------------- |-----------:|----------:|
| TryGet_First                          |  0.0000 ns |         - |
| TryGet_Fourth                         |  1.3988 ns |         - |
| TryGet_Missing                        |  1.2151 ns |         - |
| Set_Existing_ReferenceType            |  2.1824 ns |         - |
| Context_Get_Inherited_Three_Layers_Up | 11.0667 ns |         - |

## Publish pipeline (to the transport boundary)

| Method          | Mean       | Δ vs phase 0 | Allocated |
|---------------- |-----------:|-------------:|----------:|
| Advanced_Small  |   378.5 ns | −9 %         |     408 B |
| Advanced_Medium |   624.3 ns | −7 %         |     720 B |
| Advanced_Large  | 5,678.1 ns | +3 % (noise) |     720 B |
| PubSub_Small    |   421.8 ns | −2 %         |     408 B |
| PubSub_Medium   |   676.8 ns |  0 %         |     720 B |
| PubSub_Large    | 5,718.0 ns | +4 % (noise) |     720 B |

## Unchanged reference points

Serializers, type-name serializers and `MessageFactory` are untouched in this phase and reproduce the Phase 0 numbers
(STJ small deserialize 96.8 ns / 64 B; `Message<T>` 144 B). `EventBus` (now lifecycle-only): 5.3 ns / 13.9 ns.

## End to end (real broker, no publisher confirms)

| Method            | Mean       | Δ vs phase 0 | Allocated |
|------------------ |-----------:|-------------:|----------:|
| Publish           |   3.848 μs | +9 % (phase 0 had 12 % StdDev) | 1.59 KB |
| PublishAndConsume | 454.4 μs   | −5 %         | 6.75 KB   |

The whole-stack `Publish` allocation (985 B → 1.59 KB) is not reproduced by the in-process publish gate (408 B, unchanged);
BenchmarkDotNet's memory diagnoser also counts RabbitMQ.Client's socket writer/reader threads, whose frame batching
depends on timing. Treat the e2e allocation column as indicative only; the deterministic gates are the contract.

## Native AOT (`EasyNetQ.Examples.Aot`)

- Trim/AOT warnings: 22 (Phase 0: 22). Nothing in this phase touches the reflection sites.
