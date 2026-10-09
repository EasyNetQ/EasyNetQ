# EasyNetQ.AspNetCore.SignalR

A SignalR scale-out backplane on EasyNetQ: run several servers behind a load balancer and every hub send reaches
every connected client, whichever server it is on. Same semantics as `Microsoft.AspNetCore.SignalR.StackExchangeRedis`,
over RabbitMQ (or any EasyNetQ transport) instead of Redis. Native AOT and trim safe.

```csharp
using EasyNetQ;

builder.Services.AddEasyNetQ("host=rabbitmq;username=app;password=...");
builder.Services.AddSignalR()
    .AddEasyNetQ(backplane => backplane.Prefix("chat"));
```

`AddEasyNetQ` (the backplane) needs an EasyNetQ transport registered first: `AddEasyNetQ(connectionString)` for
RabbitMQ, or `AddSingleton<ITransport>(new InMemoryTransport(broker))` plus `AddEasyNetQCore()` in tests.

## What is supported

Everything `HubLifetimeManager` covers, across servers:

| Operation | How |
|---|---|
| `Clients.All`, `AllExcept` | every server's queue is bound to `all`; except-lists travel with the message |
| `Client`, `Clients` | local connections are written directly, others through `conn.{id}` |
| `Group`, `Groups`, `GroupExcept` | a server binds `group.{name}` while it holds a member |
| `User`, `Users` | a server binds `user.{id}` while it holds one of the user's connections |
| `Groups.AddToGroupAsync` / `RemoveFromGroupAsync` for a connection on another server | a command to every server, acknowledged by the owner; `AckTimeout` (30 s) otherwise, then `TimeoutException` |
| Client results (`Clients.Client(id).InvokeAsync<T>`) | the invocation goes to the connection's server, the result comes back to the caller; a connection no server holds fails at once with `IOException` (mandatory publish, returned by the broker) |

## Topology

Per hub, `{prefix}.{hub full name}`:

- one **direct** exchange: group, user and connection names are user data and never act as topic wildcards;
- one **durable** queue per server, `{prefix}.{hub}.{server}`, with `x-expires` (`QueueExpiry`, default 1 minute):
  it survives connection blips and broker restarts with the messages sent meanwhile, and the broker removes it once
  its server is gone. (RabbitMQ 4 refuses transient non-exclusive queues.) A server deletes its queue on shutdown;
- bindings `all`, `groups`, `ack.{server}`, `return.{server}`, plus `conn.*`/`user.*`/`group.*` while needed.
  Names over 255 bytes are replaced by their SHA-256.

## Resilience

When the backplane's consumer connection recovers, or the broker cancels its consumer (queue deleted, queue node
lost, policy change), the backplane redeclares its queue and every binding and restarts consuming. It hooks into the
EasyNetQ lifecycle pipeline (`LifecycleEvent.Recovered`, `LifecycleEvent.Cancelled`); nothing to configure.

Messages are best effort, as with any SignalR backplane: a send while a server's queue is gone is lost for that server.
While the broker is unreachable a send waits for the channel to recover for at most `SendTimeout` (5 s), then throws
`TimeoutException`; a request that sends after its own work (a write, then a notification) should treat that as
"not delivered", not as a failed write.

## Options

| Builder method | Default | Meaning |
|---|---|---|
| `Prefix(string)` | `signalr` | prefix of every exchange and queue; servers of one app must share it |
| `ServerName(string)` | machine name + random suffix | unique per server process |
| `AckTimeout(TimeSpan)` | 30 s | wait for a remote group change |
| `SendTimeout(TimeSpan)` | 5 s | bound on one backplane publish while the broker is unreachable |
| `QueueExpiry(TimeSpan)` | 1 min | `x-expires` of the server queue |
| `PrefetchCount(ushort)` | 100 | unacknowledged messages per server |

## Native AOT

The backplane itself is reflection-free (binary envelope, SignalR's own per-protocol payloads). For an AOT app, give
the JSON hub protocol a source-generated context, as SignalR requires anyway:

```csharp
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJson.Default))
    .AddEasyNetQ();
```

`EasyNetQ.Examples.SignalRAot` publishes with zero trim/AOT warnings and runs the cross-server checks against a broker.

## Observability

Instruments on the `EasyNetQ` meter: `easynetq.signalr.messages` (by kind and direction), `easynetq.signalr.connections`,
`easynetq.signalr.ack_timeouts`, `easynetq.signalr.send_timeouts` (by kind), `easynetq.signalr.resyncs`. Backplane traffic does not run through the application's
publish/consume pipelines: it is SignalR's payload, not application messages.

## Not supported

- Streaming from the server to a specific client on another server is not a lifetime-manager concern in SignalR and
  works as usual; there is nothing extra to scale out.
- Sticky sessions are still required for transports other than WebSockets, as with every SignalR backplane.
