# Phase 5 — Transport abstraction, fluent API, netstandard restore

Branch `v9/phase-5-fluent`, commits 5b–5h. Same machine as phase 4 and **still not quiet** (developer
containers running throughout), so timings are indicative; allocations are exact. Short-job runs
(IterationCount=3) for the timing columns — the deltas that matter this phase are the allocation columns,
which are deterministic.

## What landed

- **5b — `ITransport`** in Core (`ITransportConnection`/`ITransportChannel`/`ITopology` + definition records);
  `RabbitMqTransport` maps logical channels onto the persistent dispatcher; `RabbitAdvancedBus` runs on it.
- **5c — `EasyNetQ.Transport.InMemory`**: in-process broker (AMQP topic matching, redelivery, server-named
  queues); `EasyNetQ.Core.Tests` exercises the whole stack without a broker.
- **5d — fluent consumers**: CRTP `ConsumerBuilder<TSelf>` in Core, `RabbitMqConsumerBuilder`/`RabbitMqQueueBuilder`
  typed layer under `UseRabbitMq(...)`, `IHostedService` startup declaring topology and starting consumers.
- **5e — fluent publish routes** + `IMessagePublisher`: one route per message type, fixed or per-message
  routing key (typed resolver, no boxing), exchange declared on first publish, one built pipeline per
  definition. `SerializeStep` moves serialization into the publish pipeline, before interceptors.
- **netstandard2.0 restored** across the packages (maintainer decision: reach is a selling point):
  multi-targeted in the same assemblies with PolySharp + per-TFM refs + `#if`; no separate compat assembly.
  net472 SDK-style compile proof in `EasyNetQ.Examples.NetFramework`. The net8+ binaries are unchanged.
- **5f — typed publishes through the pipeline**: `IAdvancedBus.PublishAsync<T>` sets `Message`/`MessageType`
  on the context and lets `SerializeStep` serialize in-pipeline; custom serialization strategies and
  derived-type bodies keep the legacy pre-serializing path. `[DeliveryMode]` now stamps direct publishes.
- **5g — lifecycle pipeline**: `builder.Lifecycle(l => l.Use(...))` runs for connection events and consumer
  Started/Stopped on any transport; free when unused. Replaces `IEventBus` as the user-facing surface.
- **5h — `TransportRpc`**: `IRpc` over `ITransport` (request publish pipeline + reply consumer + correlation),
  same wire shape as `DefaultRpc`; Core-only hosts get RPC, which makes the InMemory RPC benchmark possible.

## Numbers

### RPC round trip, InMemory (new baseline — no prior phase had an RPC benchmark)

| Method | Mean | Allocated |
|---|---:|---:|
| RequestResponse | 7.3 µs* | 2.45 KB |

Request publish (serialize, correlation, expiration) + reply consumer dispatch (deserialize, correlation
resolution) + response publish + TCS machinery. Two full InMemory deliveries (~1 KB of the total) are the
transport cost; the rest is TCS/timeout/configuration per request.

### End to end, InMemory

| Method | Phase 5c | Phase 5h | Allocated |
|---|---:|---:|---:|
| PublishAndConsume | 2.6 µs | 3.7 µs* | 480 B (=) |

### Consume pipeline (unchanged — the typed dispatch steps moved to a shared helper, same chain)

| Method | Phase 4 | Phase 5 | Allocated |
|---|---:|---:|---:|
| Consume_Small | 268.0 ns* | 206.0 ns* | 64 B (=) |
| Consume_Medium | 903.3 ns* | 697.7 ns* | 1632 B (=) |
| Consume_Large | 12,982 ns* | 10,431 ns* | 13520 B (=) |

### Publish pipeline (now runs SerializeStep inside the pipeline)

| Method | Phase 4 | Phase 5 | Allocated |
|---|---:|---:|---:|
| Advanced_Small | 376.6 ns | 400.7 ns* | 264 B (=) |
| Advanced_Medium | — | 663.6 ns* | 576 B (=) |
| PubSub_Small | 389.5 ns | 409.5 ns* | 264 B (=) |
| PubSub_Large | — | 5,862 ns* | 576 B (=) |

\* busy-machine timing; allocation columns exact.

## Gates

Every allocation-test ceiling holds unchanged through 5b–5h: moving serialization into the pipeline and
adding the fluent/lifecycle machinery costs zero bytes on the hot paths. The phase-5 exit criterion
"publish path allocations unchanged despite more steps" is met by the allocation gates, not just the
benchmark columns.

Unit 457, Core.Tests 25, allocation 10, serialization 16, hosepipe 8, approval 4 (per-assembly), generator 7,
integration 52/52, format clean, net472 example builds, packs (4 lib TFMs, per-TFM dependency groups).

## Deferred

- Cold-machine timing re-baseline (blocked on a quiet machine since phase 4).
- Facade decomposition remainder: PubSub/SendReceive/scheduler definitions, `RabbitAdvancedBus` shrink,
  `DefaultRpc` replacement by `TransportRpc` (phase 6, with error-queue fault copies and reconnect resets).
- Integration `ApiMode.Fluent`/`ApiMode.Compat` parametrization.
