# EasyNetQ 9.0: release notes, design direction and migration guide

9.0 is in pre-release (`9.0.0-alpha.1` on nuget.org). This document tracks the v9 branches and changes until 9.0
ships. Sections: [Highlights](#highlights), [Design direction](#design-direction), [Performance](#performance),
[Upgrading from 8.x](#upgrade-in-five-steps), [What v9 adds](#what-v9-adds-in-detail),
[Known gaps before 9.0](#known-gaps-before-90).

## Highlights

- **Drop-in for `IBus` users.** `PubSub`, `Rpc`, `SendReceive` and `Scheduler` keep their shape; most apps
  recompile with few or no changes and stay wire-compatible with 8.x services, so rolling upgrades work.
- **One middleware pipeline** for publishing, consuming and connection lifecycle (`IMiddleware<TContext>`),
  replacing 8.x's pipeline builders, ack strategies and per-message events.
- **Fluent configuration**: declare consumers, queues, exchanges and publish routes at startup, with typed
  RabbitMQ settings (quorum queues, dead-lettering, error queues) and background consumer startup.
- **Transport abstraction**: `EasyNetQ.Core` has no RabbitMQ dependency; `EasyNetQ.RabbitMQ` implements it, and
  `EasyNetQ.Transport.InMemory` runs the same code in tests without a broker.
- **Native AOT**: a source generator replaces runtime reflection; a default app publishes with zero trim/AOT
  warnings, enforced in CI.
- **Interop**: stable wire names, aliases for foreign type names (Wolverine, MassTransit), raw handling of unknown
  and untyped messages.
- **Observability**: OpenTelemetry `ActivitySource` and `Meter` named `EasyNetQ`.
- **New package `EasyNetQ.AspNetCore.SignalR`**: a SignalR scale-out backplane over EasyNetQ instead of Redis.
- **Packages**: `EasyNetQ` becomes a bundle over `EasyNetQ.Core` and `EasyNetQ.RabbitMQ`; targets
  `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`.

## Design direction

v9 is a rewrite of the internals around a few rules, each enforced by tests rather than review:

- **Everything is a pipeline step.** Connection, channel, consumer and handler are layers; each feature
  (serialization, error handling, interceptors, confirms) is a step you can insert before, after, replace or
  remove. Lower layers cannot modify higher ones.
- **No reflection in Core.** A Roslyn source generator discovers message types and wires `AddEasyNetQ(...)`
  at compile time. The 8.x reflection APIs survive in the `EasyNetQ` bundle for compatibility, annotated so AOT
  apps get a warning at the call site. Native AOT is a result, not an add-on.
- **The transport is a library.** Core never references RabbitMQ.Client. Typed broker settings live under
  `UseRabbitMq(...)`; the generic, top-level configuration exists for portable code and back-compat.
- **Do not rebuild what RabbitMQ.Client 7 does.** Publisher-confirm tracking, recovery and callbacks are the
  client's; EasyNetQ adds the semantic layer on top. The same goes for tracing: the client emits wire spans,
  EasyNetQ adds message-level spans and metrics.
- **Lifecycle pipelines replace the event bus** as the user-facing surface for connection and consumer events.
- **Allocation budgets only go down.** Every hot path has an allocation ceiling in
  `Source/EasyNetQ.AllocationTests`; every phase commits its benchmark deltas
  (`Source/EasyNetQ.Benchmarks/results/`).
- **Compatibility is best effort.** The API shape is kept, signatures may break, and this guide covers the
  difference, rather than keeping an 8.x snapshot frozen.
- **Reach stays.** `netstandard2.0` is still a target, multi-targeted in the same assemblies (no separate compat
  assembly), so .NET Framework 4.7.2+ SDK-style projects keep working.
- **Dogfooded.** v9 runs in production consumers; gaps found there are fixed in the library, not worked around in
  the apps (the [dogfooding fixes](#wire-names-aliases-and-foreign-messages) below came from that).

## Performance

Allocations per message on the hot paths, 8.x pipeline versus v9 (BenchmarkDotNet on .NET 10; allocations are
deterministic, timings were taken on a busy machine and are left out):

| Path | 8.x | 9.0 |
|---|---:|---:|
| Consume, small message | 208 B | 64 B |
| Consume, medium message | 1,776 B | 1,632 B |
| Publish (advanced or `PubSub`), small message | 408 B | 264 B |
| Publish, medium message | 720 B | 576 B |
| Publish + consume end to end (in-memory) | n/a | 480 B |

The fixed per-message overhead of the pipeline dropped by 144 B on each side; what remains is mostly the JSON
payload itself. Moving serialization into the publish pipeline and adding the fluent and lifecycle machinery
cost zero bytes on the hot paths.

# Upgrading from 8.x

**The short version:** most applications that use `IBus` (`PubSub`, `Rpc`, `SendReceive`, `Scheduler`) recompile with
few or no source changes, and keep talking to 8.x services on the wire. Code that touches the internals (custom
pipelines, ack strategies, the event bus, publisher-confirmation types) needs editing, and a handful of defaults
changed behavior. Everything new in v9 is opt-in.

## Upgrade in five steps

1. **Check the toolchain.** An SDK-style project and **.NET SDK 9.0.300 or later** (the source generator targets
   Roslyn 4.14). `packages.config` projects cannot run the generator: stay on 8.x there.
2. **Update the packages.** Every EasyNetQ package moves to the same 9.x version:

   ```xml
   <PackageReference Include="EasyNetQ" Version="9.0.0-alpha.1" />
   <!-- only if you used it in 8.x -->
   <PackageReference Include="EasyNetQ.Serialization.NewtonsoftJson" Version="9.0.0-alpha.1" />
   ```

   `EasyNetQ` now bundles `EasyNetQ.Core` (transport-agnostic) and `EasyNetQ.RabbitMQ`; keep referencing `EasyNetQ`.
   The source generator ships inside `EasyNetQ.Core`, so nothing else is needed.
3. **Rebuild everything that references EasyNetQ.** Types moved between assemblies: binary compatibility is gone even
   where source compatibility stays. A library compiled against 8.x will not load under 9.x.
4. **Fix the compile errors** with [Source changes](#source-changes).
5. **Go through [Behavior changes](#behavior-changes)** before deploying. Two of them change semantics silently:
   consumer concurrency and publishes interrupted by a reconnect.

## Wire compatibility: running 8.x and 9.x side by side

A rolling upgrade works: by default, 9.x names, routes and serializes messages the way 8.x does.

| | 8.x and 9.x |
|---|---|
| `type` property (wire name) | `Namespace.Type, Assembly`: the same `DefaultTypeNameSerializer` output |
| Exchange and queue names | same conventions (`Conventions` falls back to the type name, as in 8.x) |
| JSON written | System.Text.Json, `JsonSerializerDefaults.General` (PascalCase): same as 8.x's default `SystemTextJsonSerializerV2` |
| `UseNewtonsoftJson()`, `UseLegacyConventions()` | still available, same output |

Differences a peer can notice:

- 9.x no longer adds the `EasyNetQ.Confirmation.Id` header.
- 9.x **reads** JSON property names case-insensitively, so a camelCase body from another stack now populates the
  message instead of silently yielding default values. Writing is unchanged.
- A `byte[]` (or `ReadOnlyMemory<byte>`, `Memory<byte>`, `ArraySegment<byte>`) passed to `IAdvancedBus.PublishAsync`
  is sent as-is. In 8.x a `byte[]` bound to the typed overload and was published as a base64 JSON string.
- A wire name or alias you set with `MessageType<T>(m => m.WireName(...))` or `[MessageType("...")]` replaces the
  default `type`. Only use one once every consumer of that type runs 9.x, or give the 9.x consumers the old name as
  an alias.

## Source changes

### Removed types

| 8.x | 9.0 replacement |
|---|---|
| `AckStrategyAsync`, `AckStrategies`, `AckResult` | `AckDecision` (`Ack`, `NackRequeue`, `NackDiscard`, `Handled`) |
| `ConsumePipelineBuilder`, `ProducePipelineBuilder`, `ProduceContext` | `PipelineBuilder<ConsumeContext>` / `PipelineBuilder<PublishContext>`, steps are `IMiddleware<TContext>` |
| `DeliveredMessageEvent`, `AckEvent`, `PublishedMessageEvent` | a pipeline step (see [Pipelines](#pipelines-and-per-message-events)) |
| `MessageConfirmationEvent`, `ChannelRecoveredEvent`, `ChannelShutdownEvent` | removed with the confirmation listener; connection events via `Lifecycle(...)` |
| `IPublishConfirmationListener`, `IPublishPendingConfirmation`, `PublishConfirmationListener`, `PublishInterruptedException` | RabbitMQ.Client tracks confirms (see [Behavior changes](#behavior-changes)) |
| `MessageFactory` | `MessageTypeDescriptor<T>.CreateMessage` (legacy `IMessage` paths only) |
| `new MessageProperties(IReadOnlyBasicProperties)` | `BasicPropertiesMapper.FromBasicProperties` |
| `ReflectionHelpers`, the vendored `Sprache` parser | none (internal) |

No namespace was removed. One type moved: `ConsumeContext` is now in `EasyNetQ.Pipeline` (was `EasyNetQ.Consumer`).
New code needs `using EasyNetQ.Pipeline;` for `IMiddleware<T>`, the contexts and `LifecycleEvent`, and
`using EasyNetQ.Configuration;` for the fluent builder extensions (`Consume`, `Publish`, `UseRabbitMq`, `Lifecycle`).

### Ack strategies → `AckDecision`

Advanced-bus handlers and error strategies return a `ValueTask<AckDecision>` instead of a `Task<AckStrategyAsync>`.

```csharp
// 8.x
await bus.Advanced.ConsumeAsync(queue, async (body, properties, info) =>
{
    await HandleAsync(body);
    return AckStrategies.Ack;
});

// 9.0
await bus.Advanced.ConsumeAsync(queue, async (body, properties, info) =>
{
    await HandleAsync(body);
    return AckDecision.Ack;
});
```

`AckStrategies.NackWithRequeue` becomes `AckDecision.NackRequeue`; `NackWithoutRequeue` becomes `NackDiscard`.
`Handled` means the step already settled the delivery itself.

### Custom `IConsumeErrorStrategy`

Both methods return `ValueTask<AckDecision>`, take `EasyNetQ.Pipeline.ConsumeContext`, and receive the consumer's
cancellation token.

```csharp
public sealed class MyErrorStrategy : IConsumeErrorStrategy
{
    public ValueTask<AckDecision> HandleErrorAsync(ConsumeContext context, Exception exception, CancellationToken ct = default)
        => new(AckDecision.NackDiscard);

    public ValueTask<AckDecision> HandleCancelledAsync(ConsumeContext context, CancellationToken ct = default)
        => new(AckDecision.NackRequeue);
}
```

`DefaultConsumeErrorStrategy` lost its confirmation-listener constructor parameter.

### Pipelines and per-message events

8.x's `Func<ConsumeDelegate, ConsumeDelegate>` pipelines and its per-message events are replaced by one middleware
model for publishing, consuming and connection lifecycle:

```csharp
public sealed class TimingStep : IMiddleware<ConsumeContext>
{
    public async ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        var started = Stopwatch.GetTimestamp();
        await next(context);
        Log(context.ReceivedInfo.Queue, context.Ack, Stopwatch.GetElapsedTime(started));
    }
}
```

- Consume side: `Consume(c => c.Message(p => p.Use<TimingStep>()))` per consumer. `context.Ack` holds the decision
  (what `AckEvent` reported); `context.Message` holds the deserialized message (what `DeliveredMessageEvent` reported).
- Publish side: `Publish(p => p.Pipeline(b => b.InsertAfter<SerializeStep, MyStep>()))`. Steps after `SerializeStep`
  see the serialized body, which is where compression or encryption belong (what `PublishedMessageEvent` reported).
- Inline: `Use("name", async (context, next) => { ...; await next(context); })`.
- `Use<T>()`, `InsertBefore<TMarker, T>()`, `InsertAfter<TMarker, T>()` and `Replace<TMarker, T>()` resolve the
  step from DI; the overloads taking `Func<IServiceProvider, T>` use a factory.
- `IProduceConsumeInterceptor` (`GZipInterceptor`, `TripleDESInterceptor`) still works:
  `UseConsumeInterceptors()` / `UseProduceInterceptors()` are now extensions on `PipelineBuilder<T>`.

### Connection events → lifecycle pipeline

Subscribing to `IEventBus` for connection events still compiles, but `IEventBus` becomes internal before 9.0 ships.
Move to the lifecycle pipeline, which works on every transport:

```csharp
services.AddEasyNetQ("host=rabbitmq")
    .Lifecycle(l => l.Use("log", (context, next) =>
    {
        if (context.Event == LifecycleEvent.Disconnected)
            logger.LogWarning("broker connection lost: {Reason}", context.Reason);
        return next(context);
    }));
```

Events: `Connected`, `Recovered`, `Disconnected`, `Blocked`, `Unblocked`, `RecoveryError`, `CallbackError`, and for
consumers `Started`, `Stopped`, `StartFailed`, `Cancelled`. `LifecycleContext` carries `Layer`, `Event`, `Reason`
and `Error`. With no steps registered, notifications cost nothing.

### Constructors of DI-built types

These only matter if you construct them yourself (tests, fakes, custom registrations):

- `Conventions` and `MessageDeliveryModeStrategy` take an `IMessageTypeRegistry`.
- `RabbitAdvancedBus` takes an `ITransport`; the confirmation-listener, dispatcher and consumer-factory parameters are gone.
- `ConsumerHostedService` takes `ConsumerHostOptions`, `ConsumerHostStatus` and a logger.

### Nullable annotations

The public API now says where `null` is possible; nothing was renamed or removed, so code compiles, but projects
with `<Nullable>enable</Nullable>` see new warnings where they dereference these without a check:

- `MessageProperties`: the string properties and `Headers`.
- Topology `arguments` (`Queue`, `Exchange`, `Binding` and the `IAdvancedBus` declare methods) and `.Arguments`.
- Optional parameters defaulting to `null` (`ExchangeAttribute`, `QueueAttribute`, `AdvancedBusEventHandlers`,
  `CreateChannelAsync(options)`), unset `ConnectionConfiguration` properties and events, and members that can return
  `null` (`IMessage.GetBody()`, `QueueTypeConvention`, `ErrorQueueTypeConvention`).
- `AsyncQueue<T>.TryDequeue` carries `[MaybeNullWhen(false)]`.

### Serializers

- `IMessageSerializer` (generic, descriptor-based) is the primary interface. Existing `ISerializer` implementations,
  Newtonsoft included, keep working: they are wrapped in `LegacyMessageSerializerAdapter`.
- `QueueStats` moved from the RabbitMQ assembly to `EasyNetQ.Core` (same namespace).

## Behavior changes

Review each one; the right-hand column restores 8.x behavior where that is possible.

| Change | 8.x | 9.0 | Restore 8.x behavior |
|---|---|---|---|
| Consumer dispatch concurrency | `PrefetchCount` (concurrent, unordered) | **1** (ordered) | `consumerDispatcherConcurrency=<n>` in the connection string, or `ConnectionConfiguration.ConsumerDispatcherConcurrency` |
| Publish interrupted by a reconnect | silently republished | **fails to the caller** | retry in your code (`PublishInterruptedException` and the retry loop are gone) |
| Publisher confirms | EasyNetQ's listener | RabbitMQ.Client: `PublishAsync` completes on confirm, at most 128 outstanding confirms per channel | none needed |
| Per-request `PublisherConfirms` | `bool` (default silently disabled confirms on every high-level publish) | `bool?`; unset uses the connection setting | set it explicitly per request |
| Consumer restart after a channel error | polled every 5 s | immediate, event-driven; 60 s safety-net timer | none needed |
| Connection string | unknown keys ignored | **unknown keys throw** `EasyNetQException`; keys are case-insensitive | remove typos and obsolete keys |
| Fluent consumers' startup | n/a | in the background, retrying with backoff; host startup never blocks or crashes on a broker outage or a missing exchange | `ConsumerHost(o => o.WaitForStartup = true)` |
| JSON property names when reading | case-sensitive | case-insensitive | pass your own `JsonSerializerOptions` |
| `byte[]` body on `IAdvancedBus.PublishAsync` | serialized as a base64 JSON string | sent as-is | serialize it yourself |
| Failed message bodies in logs (event 601) | logged | not logged (error queues often hold personal data); event 600 still logs the failure | `UseRabbitMq(r => r.LogFailedMessageBodies())` |
| Unknown message type on a consumer | plain `EasyNetQException` before handler matching | `UnknownMessageTypeException` (subclass), logged once per queue and type; or handled by `HandleUnknown` | catch the base type |
| `[DeliveryMode]` on direct advanced publishes | ignored | stamped | remove the attribute |
| Topology operations | only channel acquisition honored the timeout | the whole operation honors it | none needed |
| Lifecycle steps during container shutdown | could see `Disconnected` and hit `ObjectDisposedException` | the host releases its connection first | none needed |

Code that publishes right after starting the host should `await IConsumerHostStatus.WaitForStartedAsync()` first,
because fluent consumers now start in the background.

# What v9 adds in detail

None of this is needed to upgrade; adopt it when it helps.

### Fluent configuration

Declare consumers and publish routes at startup, with typed RabbitMQ settings:

```csharp
using EasyNetQ;
using EasyNetQ.Configuration;

services.AddEasyNetQ("host=rabbitmq")
    .UseRabbitMq(r => r
        .ErrorQueue("orders.errors", q => q.Quorum())
        .Publish(p => p
            .Exchange("orders", e => e.Topic())
            .Message<OrderPlaced>(o => $"order.{o.Region}"))
        .Consume(c => c
            .Queue("billing.orders", q => q.Quorum().DeadLetterExchange("orders.dlx"))
            .Bind("orders", "order.*", e => e.Topic())
            .Handle<OrderPlaced>(async (order, context) =>
            {
                await billing.ChargeAsync(order);
                return AckDecision.Ack;
            })));

// publish anywhere: the route picks exchange and routing key, the exchange is declared on first publish
await provider.GetRequiredService<IMessagePublisher>().PublishAsync(new OrderPlaced(...));
```

- `AddEasyNetQCore()` plus `Consume(...)`/`Publish(...)` is the transport-agnostic version, for code that must not
  depend on RabbitMQ.
- `Bind` declares the exchange; `BindExisting`, `ExistingQueue` and `ExistingExchange` use topology another app owns.
- A message type publishes through exactly one route; publishing an unrouted type throws.
- Consumers start from an `IHostedService`. `IConsumerHostStatus` (`IsStarted`, `PendingConsumers`, `LastError`,
  `WaitForStartedAsync`) serves readiness checks. `ConsumerHost(o => ...)` tunes `RetryDelay`, `MaxRetryDelay` and
  `WaitForStartup`.
- `ErrorQueue(...)` declares the bus-wide error queue with typed arguments (e.g. quorum), and can rename it from
  `EasyNetQ_Default_Error_Queue` for brokers that scope permissions by name.
- `Consume(c => c.Serializer(...))` sets a serializer per consumer.

### Wire names, aliases and foreign messages

- `MessageType<T>(m => m.WireName("orders.placed.v1").Alias("Legacy.Name"))`, or
  `[MessageType("orders.placed.v1", Aliases = new[] { ... })]` on the type: a stable contract name instead of the
  CLR name. Aliases are additional incoming names, e.g. the `Type.FullName` a Wolverine or MassTransit peer sends.
  A wire name that resolves to two types fails at startup.
- `Message<T>("key", r => r.WireName("contract.v1"))` overrides the name for one publish route only.
- `HandleUnknown((body, context) => ...)` receives the raw bytes of messages no typed handler matches;
  `context.Properties.Type` holds the incoming name.
- Messages without a `type` property (plain AMQP clients, shovels) go to the consumer's only handler, or to
  `DefaultMessageType<T>()`.

### Transports and testing

- The `EasyNetQ.Transport` namespace (`ITransport`, `ITopology`, exchange/queue/binding definitions) abstracts the
  broker. `EasyNetQ.RabbitMQ` implements it.
- `EasyNetQ.Transport.InMemory` runs the same code in tests without a broker. It follows AMQP closely: mandatory
  publishes without a route throw `UnroutableMessageException`, and deleting a queue cancels its consumers.
- `UnroutableMessageException` (Core) is the base of `PublishReturnedException`, so transport-agnostic code can
  catch "nobody is listening".
- Request/response works on any transport (`TransportRpc`). RabbitMQ hosts keep `DefaultRpc` for now. Differences in
  `TransportRpc`: a faulted responder acks the request after replying (no error-queue copy), and non-durable reply
  subscriptions are not yet reset on reconnect.

### Native AOT

A default `AddEasyNetQ(...)` app publishes with **zero** trim/AOT warnings; CI enforces this with
`EasyNetQ.Examples.Aot` and with `tests/package-consumer`, which builds that sample against the packed packages.

- Pass a source-generated context: `UseSystemTextJson(MyJsonContext.Default)`. Contexts registered by transports and
  by you are combined.
- The source generator registers the message types it sees at call sites and intercepts `AddEasyNetQ(...)`.
  Types it cannot see need `MessageType<T>()`, or `[MessageType]` on the type.
- Reflection fallbacks (loading a type from its wire name, describing an unregistered type, reflection-based JSON)
  throw an `EasyNetQException` naming what to register under AOT, or when `EasyNetQ.RuntimeReflection.IsSupported`
  is false.
- The 8.x reflection APIs carry `[RequiresUnreferencedCode]` (and `[RequiresDynamicCode]` where they emit code), so
  AOT apps get a warning at the call site: `AutoSubscriber`, `UseLegacyTypeNaming`, `UseLegacyConventions`,
  `UseAdvancedMessagePolymorphism`, `UseVersionedMessage`, `SystemTextJsonSerializer(V2)`,
  `LegacyTypeNameSerializer`, the versioning and multiple-exchange strategies.
- A custom `IMessageSerializationStrategy` (message versioning, for example), or publishing a derived instance
  through a base type parameter, keeps the 8.x pre-serializing publish path.

### Observability

`ActivitySource` and `Meter`, both named `EasyNetQ`: `AddSource("EasyNetQ")` and `AddMeter("EasyNetQ")`. Keep
RabbitMQ.Client's own `RabbitMQ.Client.*` sources enabled for wire-level spans.

### SignalR backplane

The new `EasyNetQ.AspNetCore.SignalR` package scales SignalR out over EasyNetQ, with the semantics of the Redis
backplane:

```csharp
builder.Services.AddEasyNetQ("host=rabbitmq");
builder.Services.AddSignalR().AddEasyNetQ(b => b.Prefix("chat"));
```

It supports all/except, connections, groups (including acknowledged membership changes across servers), users and
client results, and is Native AOT safe. See the package README.

### Consuming EasyNetQ from source

For a git submodule instead of packages, import
`<Import Project="…/EasyNetQ/Source/EasyNetQ.SourceReference.props" />`. It adds the project references
(`EasyNetQSourcePackages`, default `EasyNetQ`), the generator as an analyzer, and the interceptors namespace.

- Every EasyNetQ project builds once, in the consumer's configuration.
- MinVer is skipped when the checkout has no `.git`, as in Docker build contexts.
- In a consumer solution, every project that references EasyNetQ, even transitively, imports the props.
- `tests/source-reference/check.sh` guards this in CI.

# Known gaps before 9.0

- `IEventBus` becomes internal; move connection-event subscribers to `Lifecycle(...)` now.
- RabbitMQ hosts still use `DefaultRpc`; `TransportRpc` replaces it once it copies faulted requests to the error
  queue and resets non-durable reply subscriptions on reconnect.
- `PubSub`, `SendReceive` and the scheduler are not yet expressed as fluent definitions, and `RabbitAdvancedBus`
  still carries more than the transport needs.
- Timings will be re-baselined on a quiet machine; only allocation numbers are authoritative so far.

# Platform notes

- Target frameworks: `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`.
- .NET Framework 4.7.2+ works through `netstandard2.0` from SDK-style projects. Those binaries skip pooled async
  builders on the publish/consume path; `net8.0`+ is unaffected.
- `IsAotCompatible` is set for Core, RabbitMQ, InMemory and the bundle (net9.0+ assets).
- The generator package layout: `analyzers/dotnet/cs/EasyNetQ.Generators.dll` plus
  `buildTransitive/EasyNetQ.Core.props`, which adds `EasyNetQ.Generated` to `InterceptorsNamespaces`. If a build
  reports CS9137, that props file was not imported (e.g. an `ExcludeAssets`/`IncludeAssets` setting that drops
  `buildTransitive`).
