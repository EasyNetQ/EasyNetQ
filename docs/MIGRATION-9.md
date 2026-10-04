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
  intercepts `AddEasyNetQ(...)`; without it, unregistered types fail at runtime. It ships inside the
  `EasyNetQ.Core` package (`analyzers/dotnet/cs`), together with `buildTransitive/EasyNetQ.Core.props`, which opts
  the consuming project into the generated interceptors (`InterceptorsNamespaces`), so referencing `EasyNetQ` is
  all a project needs. `tests/package-consumer` proves it in CI: the Native AOT sample built against the packed
  packages, which `publish-to-nuget` waits for.

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
- `IAdvancedBus.PublishAsync<T>` also runs through `SerializeStep`: pipeline steps see the typed message and
  its descriptor before serialization. New: a message type declaring `[DeliveryMode]` gets its delivery mode
  stamped on direct advanced publishes too (previously only the high-level APIs stamped it); types without the
  attribute are unchanged. Publishing a derived instance through a base type parameter, or registering a custom
  `IMessageSerializationStrategy` (e.g. message versioning), keeps the 8.x pre-serializing path.
- Request-response works on any transport: `TransportRpc` implements `IRpc` over the transport abstraction
  (request publish pipeline + reply consumer + correlation), so Core-only hosts (e.g. InMemory tests) get RPC.
  RabbitMQ hosts keep `DefaultRpc` until phase 6. Deviations in `TransportRpc`: a faulted responder acks the
  request after publishing the fault reply (no error-queue copy), and non-durable reply subscriptions are not
  yet reset on reconnect.
- Lifecycle pipeline: `builder.Lifecycle(l => l.Use(...))` runs for connection events
  (Connected/Recovered/Disconnected/Blocked/Unblocked/RecoveryError/CallbackError) and consumer Started/Stopped
  on any transport; `LifecycleContext` carries the layer, event, reason and error, parented to the layer's
  context. This replaces subscribing to `IEventBus`, which becomes internal and is removed in phase 6; with no
  steps registered the notifications cost nothing.

## Serialization

- `IMessageSerializer` (generic, descriptor-based) is the primary interface. `ISerializer` implementations
  (including Newtonsoft) keep working through `LegacyMessageSerializerAdapter`.
- System.Text.Json is the default; pass a `JsonSerializerContext` for AOT/trimmed apps.

## Observability (new, not breaking)

- `ActivitySource`/`Meter` named `EasyNetQ`; enable with `AddSource("EasyNetQ")` + `AddMeter("EasyNetQ")` and
  keep the client's `RabbitMQ.Client.*` sources on for wire spans.

## Dogfooding fixes (additive unless noted)

Gaps found running v9 in production consumers, fixed in the library rather than worked around in apps.

- Pipeline steps resolvable from DI: `Replace<TMarker, TMiddleware>()`, `InsertBefore<TMarker, TMiddleware>()`
  and `InsertAfter<TMarker, TMiddleware>()` resolve the step from the service provider at build time; the
  `Func<IServiceProvider, TMiddleware>` overloads take a factory. The step is then addressable by its own type.
- Wire names and aliases per message type: `MessageType<T>(m => m.WireName("orders.placed.v1").Alias("Legacy.Name"))`
  on the builder, or `[MessageType("orders.placed.v1", Aliases = new[] { ... })]` on the type (read by the source
  generator, AOT-safe). The wire name is what publishes stamp and consumers match; aliases are extra incoming
  names, e.g. the `Type.FullName` a Wolverine or MassTransit peer sends. `IMessageTypeRegistry.Register<T>(wireName,
  aliases)` is the underlying call; a wire name that would resolve to two types throws at startup.
- Per-route wire name: `Publish(p => p.Exchange("x").Message<T>("key", r => r.WireName("contract.v1")))` stamps
  that name on the route's messages without changing the type's own wire name.
- Unknown messages: `Consume(c => c.Handle<T>(...).HandleUnknown((body, context) => ...))` receives every message no
  typed handler matches (a wire name this process cannot load, a known type without a handler) as raw bytes, with
  `context.Properties.Type` carrying the incoming name; the body is not deserialized. Without it such messages fail
  with `UnknownMessageTypeException` (a subclass of `EasyNetQException`, previously a plain `EasyNetQException`
  thrown before handler matching), logged once per queue and type name.
- Messages without a `type` property (plain AMQP clients, shovels, non-.NET peers) dispatch to the consumer's only
  handler, or to `DefaultMessageType<T>()` when it has several; otherwise `HandleUnknown` or
  `UnknownMessageTypeException` as above.
- **Behavior change:** the default System.Text.Json options read property names case-insensitively
  (`SystemTextJsonMessageSerializer.CreateDefaultOptions()`), also with source-generated contexts. A camelCase body
  from another stack used to deserialize silently into default values. Writing is unchanged. Options you pass
  yourself are used as given.
- Per-consumer serializer: `Consume(c => c.Serializer(serializer))`.
- `BindExisting(exchange, routingKey)` binds to an exchange another application owns without declaring it
  (`Bind` declares, as before), like `ExistingQueue`/`ExistingExchange`.
- **Behavior change:** fluent consumers start in the background. `ConsumerHostedService.StartAsync` returns at once
  and each consumer retries with backoff until it runs, so a broker outage no longer keeps the host (and Kestrel)
  from starting, and a missing exchange (404 on bind, e.g. one another app's operator declares later) no longer
  crashes it. `IConsumerHostStatus` (`IsStarted`, `PendingConsumers`, `LastError`, `WaitForStartedAsync`) serves
  readiness checks and tests; failures also raise `LifecycleEvent.StartFailed`. Restore blocking startup with
  `ConsumerHost(o => o.WaitForStartup = true)`; `RetryDelay`/`MaxRetryDelay` tune the backoff. Code that publishes
  right after `StartAsync` should await `WaitForStartedAsync` first.
- `ConsumerHostedService` constructor takes `ConsumerHostOptions`, `ConsumerHostStatus` and a logger (DI-built).
- `IAdvancedBus.PublishAsync(..., MessageProperties, byte[] body)` publishes the bytes as is. Before, a `byte[]` bound
  to the typed `PublishAsync<T>` (exact generic match beats the conversion to `ReadOnlyMemory<byte>`), was serialized
  as a JSON base64 string, and failed under Native AOT. `PublishAsync<T>` also sends `byte[]`, `ReadOnlyMemory<byte>`,
  `Memory<byte>` and `ArraySegment<byte>` bodies as is. **Behavior change** for code that relied on the base64 JSON.
- `ConsumerHostedService` owns its transport connection and releases it on `StopAsync` and on `DisposeAsync`
  (`IAsyncDisposable`, for a container disposed without stopping the host). `LifecycleNotifier` is `IDisposable` and
  stops dispatching once disposed. **Behavior change:** lifecycle steps no longer see the connection's `Disconnected`
  while the container shuts down; before, a step resolving a service there threw `ObjectDisposedException` (event 701).
- **Behavior change:** `DefaultConsumeErrorStrategy` no longer logs failed message bodies (event 601) by default; the
  error queue keeps them and they often carry personal data. Opt in with `UseRabbitMq(r => r.LogFailedMessageBodies())`.
  The error itself (event 600: queue, routing key, exchange, correlation id, exception) is still logged.
- `UseRabbitMq(r => r.ErrorQueue(q => q.Quorum()))` declares the (bus-wide) error queue with typed arguments, e.g.
  quorum so failed messages survive a node loss. `ErrorQueue("app.errors", q => q.Quorum())` also names the queue and
  its exchange instead of `EasyNetQ_Default_Error_Queue`, for brokers that scope permissions by name (`^app\.`).
  `ConsumeErrorOptions` carries these settings.
- Consuming EasyNetQ from source (e.g. a git submodule): `<Import Project="…/EasyNetQ/Source/EasyNetQ.SourceReference.props" />`
  adds the project references (`EasyNetQSourcePackages`, default `EasyNetQ`), the source generator as an analyzer and
  the interceptors namespace. Source-referenced builds do not pack, and MinVer is skipped when the checkout has no
  `.git` (Docker build contexts). Every EasyNetQ project is built once, in the consumer's configuration: the references
  carry no global properties, so a project reaching EasyNetQ only transitively (a test project referencing the app, built
  on its own) shares the same instances, and outside EasyNetQ's own solution a build neither packs nor unsets
  Configuration for EasyNetQ's references (before, EasyNetQ.Core and EasyNetQ.RabbitMQ were built twice, partly as Debug,
  into the same `bin/obj`, which broke parallel builds intermittently). In a consumer *solution*, every project that
  references EasyNetQ, also only transitively, imports the props: the referencing side decides whether Configuration
  is unset. `tests/source-reference/check.sh` guards both the solution and the standalone build in CI.

## Native AOT

A default `AddEasyNetQ(...)` application publishes with **zero** trim/AOT warnings; CI fails on any
(`EasyNetQ.Examples.Aot`, which also runs every fluent feature above against a broker). Core, RabbitMQ,
InMemory and the bundle build with `IsAotCompatible` (net9.0+ assets).

- The runtime-reflection fallbacks (loading a type from its wire name, describing an unregistered runtime type,
  reflection-based JSON contracts) are guarded: unavailable under Native AOT (and with
  `EasyNetQ.RuntimeReflection.IsSupported=false`), where they throw an `EasyNetQException` that names what to
  register. Generated registrations, `MessageType<T>()` and source-generated JSON are the AOT path.
- **AOT apps pass a `JsonSerializerContext`** (`UseSystemTextJson(context)`); the transport registers its own context
  for the error-queue message, and the default resolver combines every registered context before falling back to
  reflection where that works.
- RabbitMQ's header and `MessageProperties` JSON no longer uses reflection (same wire format).
- The 8.x-compatible reflection APIs in the `EasyNetQ` bundle are annotated `[RequiresUnreferencedCode]`
  (and `[RequiresDynamicCode]` where they generate code): `AutoSubscriber.SubscribeAsync`, `UseLegacyTypeNaming`,
  `UseLegacyConventions`, `UseAdvancedMessagePolymorphism`, `UseVersionedMessage`, `SystemTextJsonSerializer(V2)`,
  `LegacyTypeNameSerializer`, the versioning/multiple-exchange strategies. Using them in an AOT app now warns at
  the call site instead of failing at runtime.
