# Phase 4 — Project split, client alignment, observability

Branch `v9/phase-4-transport`, commits 4a–4e. Same machine/runtime as earlier phases (Apple M3 Max,
.NET 10.0.11, BenchmarkDotNet 0.15.8) but **not a quiet machine this run**: unrelated developer containers
(FHIR server, PostgreSQL, Aspire dashboard, a second RabbitMQ) were running throughout, so timing deltas
carry more noise than earlier phases — re-baseline cold at the start of Phase 5. Allocations are exact and
byte-identical to Phase 2/3 everywhere; every allocation-test ceiling holds unchanged except the one noted.

## What landed

- **4a/4b — the three-package split.** `EasyNetQ.Core` (transport-agnostic; no `RabbitMQ.Client` reference,
  enforced by `Core_should_not_reference_the_rabbitmq_client`), `EasyNetQ.RabbitMQ` (client-coupled stack),
  and the `EasyNetQ` bundle (compat surface: AutoSubscribe, NonGeneric*, legacy serializers/naming,
  RabbitHutch, MessageVersioning, MultipleExchange). Per-assembly approval snapshots and per-assembly
  reflection-ratchet allowlists; all four packages pack.
- **4c — publisher confirms delegated to the client** (`publisherConfirmationTrackingEnabled: true` + explicit
  `ThrottlingRateLimiter(128)`); the publish action starts `BasicPublishAsync` inside the channel mutex and
  returns the in-flight task, awaited outside it. `PublishConfirmationListener`, the confirmation-id header
  and the republish loop are deleted; `PublishException`/`PublishReturnException` map to the existing
  EasyNetQ exceptions. Found & fixed on the way: the per-request `PublisherConfirms` was a non-nullable bool
  that silently overrode connection-level confirms to off for every PubSub/SendReceive/Rpc/FuturePublish
  publish (all of 8.x affected; released for 8.x in 8.1.7 via #1924). Consumer restart is event-driven
  (channel-fault event, 5 s rate limit, 60 s safety-net timer replaces the 5 s poll); client connection
  options exposed (`CredentialsProvider`, socket timeouts, `MaxInboundMessageBodySize`, endpoint resolver).
- **4d/4e — observability.** `ActivitySource`/`Meter` "EasyNetQ" with semconv instruments; metrics + tracing
  middleware on the default pipelines; trace-context propagation (in-box `DistributedContextPropagator`);
  error-queue republish span; `rpc {routingKey}` client span with the response's process span linked back
  (net9+). Disabled paths are a bool check and a tail call.

## Confirmed publish (new scenario, bench broker on 5673)

| Method | Mean | Allocated | Note |
|---|---:|---:|---|
| PublishConfirmed (sequential) | 224.9 µs | 3.52 KB | raw-client tracked probe: 215 µs → EasyNetQ adds ~0 |
| PublishConfirmedConcurrent16 (per op) | 26.8 µs | 3.31 KB | 8.4× from overlapping confirms; raw client: 29.7 µs |
| Publish (fire-and-forget) | NA | — | known broker-overrun cliff, see phase 2 notes |
| PublishAndConsume | 570.5 µs | 1.89 KB | 470 µs in phase 3; busy-machine noise |

Before 4c a confirmed publish held the channel mutex across the broker round trip — Concurrent16 would sit at
the sequential 225 µs. The delegated tracking matches the raw client exactly.

## Pipeline benchmarks vs phase 3

| Method | Phase 3 | Phase 4 | Allocated |
|---|---:|---:|---:|
| Consume_Small | 222.1 ns | 268.0 ns* | 64 B (=) |
| Consume_Medium | 684.1 ns | 903.3 ns* | 1632 B (=) |
| Consume_Large | 10,243 ns | 12,982 ns* | 13520 B (=) |
| Advanced_Small | 385.8 ns | 376.6 ns | 264 B (=) |
| PubSub_Small | 403.6 ns | 389.5 ns | 264 B (=) |
| Pipeline Consume_Noop | 47.4 ns | 136.5 ns* (StdDev 30.8) | 0 B (=) |
| PropertyBag / EventBus / TypeName / CreateMessage | = | = (within 1 ns) | 0 B (=) |

\* The consume pipeline gained two telemetry steps whose disabled path is a guard + tail call (a few ns
each); the rest of the regression is the busy machine — Consume_Noop's StdDev alone is 30 ns, and the publish
side got *faster* on the same run. Allocations are the ratchet and are byte-identical. Re-measure cold before
reading anything into the timing columns.

## Gates

- Unit 428, integration 50/50 (three consecutive runs during 4c–4e), generator 7, serialization 16,
  hosepipe (CI filter) 8, approval 4 (per-assembly snapshots), format clean.
- Allocation ceilings unchanged except `PublishPubSubSmall` 320 → 328 B: `PublishConfiguration.PublisherConfirms`
  became `bool?` (nullable padding) so per-request confirms no longer silently override the connection setting.
- AOT publish: 18 ILC warnings (unchanged from phase 3; all from the STJ reflection-resolver set that a
  user-supplied `JsonSerializerContext` removes).
- Integration fixture now maps host ports 5674/15674 (container `easynetq.tests`) so the suite coexists with a
  developer's broker on the defaults.

## Deferred to Phase 5

`ITransport` abstraction + `RabbitMqTransport` + `EasyNetQ.Transport.InMemory` (built together with the
`RabbitAdvancedBus` decomposition so the abstraction is designed once, against its real consumer), lifecycle
pipelines replacing `IEventBus` (entangled with the fluent builders), MockBuilder/tests reorganization,
enabled-path telemetry allocation ratchet, `CachedString` publish overloads (revisit when publish definitions
declare exchange/routing-key pairs up front).
