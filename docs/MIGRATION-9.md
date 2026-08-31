# Migrating from EasyNetQ 8.x to 9.0

Living document; updated as v9 phases land. Best-effort compatibility: the high-level API shape survives,
signatures and internals do not.

## Platform and packaging

- Target frameworks: `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. .NET Framework 4.7.2+ works through
  the `netstandard2.0` assets, but only from SDK-style projects: the required source generator runs in the
  compiler, so packages.config-era projects cannot use v9 — stay on 8.x there. The `netstandard2.0` binaries
  trade some publish/consume-path efficiency (no pooled async builders) for reach; `net8.0`+ binaries are
  unaffected.
- The `EasyNetQ` package is now a bundle over two new packages: `EasyNetQ.Core` (transport-agnostic) and
  `EasyNetQ.RabbitMQ` (client-coupled). Keep referencing `EasyNetQ` for a drop-in experience. Types moved
  between assemblies, so binary compatibility is gone even where source compatibility remains: recompile.
- The source generator (`EasyNetQ.Generators`) is required. It registers message types found at call sites and
  intercepts `AddEasyNetQ(...)`; without it, unregistered types fail at runtime.

## Behavioral changes

- `ConsumerDispatchConcurrency` defaults to **1** (ordered processing). 8.x defaulted to `PrefetchCount`
  (concurrent, unordered). Set `ConnectionConfiguration.ConsumerDispatcherConcurrency` to restore concurrency.
- Publisher confirms are tracked by RabbitMQ.Client, not EasyNetQ:
  - `BasicPublishAsync` completes when the broker confirms. Outstanding confirms are bounded per channel by a
    rate limiter (128).
  - A publish interrupted by reconnect **fails to the caller**; 8.x silently republished
    (`PublishInterruptedException` and the retry loop are gone).
  - The `EasyNetQ.Confirmation.Id` header is no longer added to messages.
  - `PublishNackedException`/`PublishReturnedException` remain, now wrapping the client's
    `PublishException`/`PublishReturnException` as inner exceptions.
- Per-request `PublisherConfirms` on `IPublishConfiguration`/`ISendConfiguration`/`IRequestConfiguration`/
  `IFuturePublishConfiguration` is `bool?`. Unset falls back to the connection-level setting (in 8.x the
  non-nullable default silently disabled confirms for every high-level publish; fixed in 8.1.7 as well).
- Consumer restart on channel-level errors is event-driven (immediate) with a 60 s safety-net timer; 8.x
  polled every 5 s.
- Connection-string parsing: unknown keys throw `EasyNetQException`; keys are case-insensitive.

## Removed APIs

| 8.x | 9.0 replacement |
|---|---|
| `ConsumePipelineBuilder`, `ProducePipelineBuilder` | `PipelineBuilder<ConsumeContext>` / `PipelineBuilder<PublishContext>` |
| `AckStrategyAsync`, `AckStrategies`, `AckResult` | `AckDecision` (`Ack`, `NackRequeue`, `NackDiscard`, `Handled`) |
| `IConsumeErrorStrategy` returning `AckStrategyAsync` | returns `AckDecision`; receives the consumer token |
| Per-message events (`DeliveredMessageEvent`, `AckEvent`, `PublishedMessageEvent`) | pipeline middleware |
| `MessageConfirmationEvent`, `ChannelRecoveredEvent`, `ChannelShutdownEvent` | removed with the confirmation listener |
| `IPublishConfirmationListener`, `IPublishPendingConfirmation`, `PublishInterruptedException` | client-side confirmation tracking |
| `MessageFactory` | `MessageTypeDescriptor<T>.CreateMessage` (legacy `IMessage` paths only) |
| `new MessageProperties(IReadOnlyBasicProperties)` | `BasicPropertiesMapper.FromBasicProperties` |
| `ReflectionHelpers`, vendored `Sprache/` | deleted |

## Changed constructors (DI-built types; affects manual construction and test fakes)

- `Conventions`, `MessageDeliveryModeStrategy`: take `IMessageTypeRegistry`.
- `RabbitAdvancedBus`: confirmation listener parameter removed.
- `DefaultConsumeErrorStrategy`: confirmation listener parameter removed.

## Transport abstraction (phase 5)

- New `EasyNetQ.Transport` namespace in Core: `ITransport`, `ITransportConnection`, `ITransportChannel`,
  `ITransportConsumer`, `ITopology`, and `ExchangeDefinition`/`QueueDefinition`/`BindingDefinition`.
  `EasyNetQ.RabbitMQ` implements them over the persistent connection/channel infrastructure.
- `RabbitAdvancedBus` constructor takes `ITransport`; the dispatcher and consumer-factory parameters are gone.
- `QueueStats` moved from the RabbitMQ assembly to `EasyNetQ.Core`.
- Topology operations now receive the timeout-linked cancellation token; in 8.x only the channel-acquisition
  wait honored the configured timeout, the operation itself did not.

## Fluent configuration (phase 5, additive)

- Transport-agnostic: `services.AddEasyNetQCore().Consume(c => c.Queue("orders").Handle<T>(...))` with any
  registered `ITransport` (e.g. `EasyNetQ.Transport.InMemory` for tests). Consumers start via `IHostedService`.
- RabbitMQ-typed: `AddEasyNetQ("host=...").UseRabbitMq(r => r.Consume(c => c.Queue("q", q => q.Quorum()
  .DeadLetterExchange("dlx")).Bind("orders", "order.*", e => e.Topic()).Handle<T>(...)))`. The transport owns
  the typed queue/exchange/consumer settings; the generic layer stays for portable code.
- Core-only hosts fall back to `SimpleConsumeErrorStrategy.NackWithRequeue`; the RabbitMQ registration keeps
  the error-queue strategy.
- Publish routes: `Publish(p => p.Exchange("orders", e => e.Topic()).Message<OrderPlaced>("order.placed"))`
  or a per-message routing key `Message<OrderPlaced>(o => $"order.{o.Region}")`. Publish through
  `IMessagePublisher.PublishAsync(message)`; the route decides exchange and routing key, the exchange is
  declared on first publish. A message type publishes through exactly one route; an unrouted type throws.
- The publish pipeline serializes inside the pipeline (`SerializeStep`); steps added via
  `Pipeline(...)`/`InsertAfter<SerializeStep>` see the serialized body (compress/encrypt goes there).

## Serialization

- `IMessageSerializer` (generic, descriptor-based) is the primary interface. `ISerializer` implementations
  (including Newtonsoft) keep working through `LegacyMessageSerializerAdapter`.
- System.Text.Json is the default; pass a `JsonSerializerContext` for AOT/trimmed apps.

## Observability (new, not breaking)

- `ActivitySource`/`Meter` named `EasyNetQ`; enable with `AddSource("EasyNetQ")` + `AddMeter("EasyNetQ")` and
  keep the client's `RabbitMQ.Client.*` sources on for wire spans.
