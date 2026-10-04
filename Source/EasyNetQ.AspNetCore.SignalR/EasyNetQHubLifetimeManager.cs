using HubSerializedMessage = Microsoft.AspNetCore.SignalR.SerializedMessage;
using System.Collections.Concurrent;
using EasyNetQ.AspNetCore.SignalR.Internal;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyNetQ.AspNetCore.SignalR;

/// <summary>
///     SignalR <see cref="HubLifetimeManager{THub}" /> that scales out over an EasyNetQ transport, with the semantics
///     of the Redis backplane: every server sees every send, group changes for remote connections are acknowledged
///     by the owning server, and client results travel back to the server that asked.
/// </summary>
/// <remarks>
///     Topology per hub: one direct exchange <c>{prefix}.{hub}</c>; one queue per server (<c>x-expires</c>), bound to
///     <c>all</c>, <c>groups</c>, its own <c>ack.*</c>/<c>return.*</c> keys, and to <c>conn.*</c>/<c>user.*</c>/
///     <c>group.*</c> while it holds a matching connection. Backplane traffic bypasses the application's publish and
///     consume pipelines: it is SignalR's payload, not application messages.
/// </remarks>
public sealed class EasyNetQHubLifetimeManager<THub> : HubLifetimeManager<THub>, IBackplaneResync, IAsyncDisposable, IDisposable
    where THub : Hub
{
    private readonly ITransport transport;
    private readonly IServiceProvider services;
    private readonly EasyNetQBackplaneOptions options;
    private readonly ILogger logger;
    private readonly IHubProtocol[] protocols;
    private readonly IReadOnlyList<IHubProtocol> allProtocols;
    private readonly string hubName;
    private readonly BackplaneNames names;

    private readonly HubConnectionStore connections = new();
    private readonly ConcurrentDictionary<string, HubConnectionStore> groups = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, HubConnectionStore> users = new(StringComparer.Ordinal);
    private readonly ClientResults clientResults = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource> acks = new();
    private readonly HashSet<string> bindings = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim subscriptions = new(1, 1);
    private readonly SemaphoreSlim initLock = new(1, 1);
    private volatile Runtime? runtime;
    private int lastAckId;
    private long lastInvocationId;
    private int resyncing;
    private bool disposed;

    private sealed class Runtime
    {
        public required ITransportConnection ProducerConnection { get; init; }
        public required ITransportConnection ConsumerConnection { get; init; }
        public required ITransportChannel ProducerChannel { get; init; }
        public required ITransportChannel ConsumerChannel { get; init; }
        public required ChannelContext ProducerChannelContext { get; init; }
        public required ChannelContext ConsumerChannelContext { get; init; }
        public required ITopology Topology { get; init; }
        public ITransportConsumer? Consumer { get; set; }
    }

    /// <summary>Created by DI through <c>AddSignalR().AddEasyNetQ()</c></summary>
    public EasyNetQHubLifetimeManager(
        ITransport transport,
        IServiceProvider services,
        EasyNetQBackplaneOptions options,
        IHubProtocolResolver protocolResolver,
        IOptions<HubOptions> globalHubOptions,
        IOptions<HubOptions<THub>> hubOptions,
        ILogger<EasyNetQHubLifetimeManager<THub>> logger
    )
    {
        this.transport = transport;
        this.services = services;
        this.options = options;
        this.logger = logger;
        allProtocols = protocolResolver.AllProtocols;
        var supported = hubOptions.Value.SupportedProtocols ?? globalHubOptions.Value.SupportedProtocols;
        protocols = supported is null
            ? allProtocols.ToArray()
            : allProtocols.Where(p => supported.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
        hubName = typeof(THub).FullName ?? typeof(THub).Name;
        names = new BackplaneNames(options.Prefix, hubName, options.ServerName);
    }

    /// <summary>This server's name, as other servers address it</summary>
    public string ServerName => options.ServerName;

    /// <inheritdoc />
    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        await EnsureRuntimeAsync(connection.ConnectionAborted).ConfigureAwait(false);

        var feature = new BackplaneConnectionFeature();
        connection.Features.Set(feature);
        connections.Add(connection);
        BackplaneMetrics.ConnectionAdded(hubName);

        await subscriptions.WaitAsync(connection.ConnectionAborted).ConfigureAwait(false);
        try
        {
            await BindAsync(BackplaneNames.Connection(connection.ConnectionId), default).ConfigureAwait(false);
            if (connection.UserIdentifier is { } userId)
                await AddToStoreAsync(users, userId, connection, BackplaneNames.User(userId)).ConfigureAwait(false);
        }
        finally
        {
            subscriptions.Release();
        }
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        connections.Remove(connection);
        BackplaneMetrics.ConnectionRemoved(hubName);

        await subscriptions.WaitAsync().ConfigureAwait(false);
        try
        {
            await UnbindAsync(BackplaneNames.Connection(connection.ConnectionId)).ConfigureAwait(false);
            if (connection.UserIdentifier is { } userId)
                await RemoveFromStoreAsync(users, userId, connection, BackplaneNames.User(userId)).ConfigureAwait(false);
            if (connection.Features.Get<BackplaneConnectionFeature>() is { } feature)
                foreach (var group in feature.TakeGroups())
                    await RemoveFromStoreAsync(groups, group, connection, BackplaneNames.Group(group)).ConfigureAwait(false);
        }
        finally
        {
            subscriptions.Release();
        }

        // invocations still waiting on this connection: fail the local ones, answer the remote callers
        foreach (var (invocationId, invocation) in clientResults.Disconnected(connection.ConnectionId))
            await ForwardCompletionAsync(
                invocation, CompletionMessage.WithError(invocationId, $"Connection '{connection.ConnectionId}' disconnected."), default
            ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        => PublishInvocationAsync(BackplaneNames.All, InvocationTarget.All, null, null, methodName, args, cancellationToken);

    /// <inheritdoc />
    public override Task SendAllExceptAsync(
        string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default
    ) => PublishInvocationAsync(BackplaneNames.All, InvocationTarget.All, null, excludedConnectionIds, methodName, args, cancellationToken);

    /// <inheritdoc />
    public override Task SendConnectionAsync(
        string connectionId, string methodName, object?[] args, CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        var connection = connections[connectionId];
        if (connection is not null)
            return connection.WriteAsync(new InvocationMessage(methodName, args), cancellationToken).AsTask();
        return PublishInvocationAsync(
            BackplaneNames.Connection(connectionId), InvocationTarget.Connection, connectionId, null, methodName, args, cancellationToken
        );
    }

    /// <inheritdoc />
    public override Task SendConnectionsAsync(
        IReadOnlyList<string> connectionIds, string methodName, object?[] args, CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connectionIds);
        var serialized = Serialize(new InvocationMessage(methodName, args));
        var tasks = new List<Task>(connectionIds.Count);
        foreach (var connectionId in connectionIds)
        {
            var connection = connections[connectionId];
            tasks.Add(connection is not null
                ? connection.WriteAsync(new SerializedHubMessage(serialized), cancellationToken).AsTask()
                : PublishAsync(
                    BackplaneNames.Connection(connectionId),
                    BackplaneMessageKind.Invocation,
                    BackplaneProtocol.WriteInvocation(new BackplaneInvocation(InvocationTarget.Connection, connectionId, null, serialized)),
                    false,
                    cancellationToken
                ));
        }
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public override Task SendGroupAsync(string groupName, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groupName);
        return PublishInvocationAsync(BackplaneNames.Group(groupName), InvocationTarget.Group, groupName, null, methodName, args, cancellationToken);
    }

    /// <inheritdoc />
    public override Task SendGroupExceptAsync(
        string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(groupName);
        return PublishInvocationAsync(
            BackplaneNames.Group(groupName), InvocationTarget.Group, groupName, excludedConnectionIds, methodName, args, cancellationToken
        );
    }

    /// <inheritdoc />
    public override Task SendGroupsAsync(
        IReadOnlyList<string> groupNames, string methodName, object?[] args, CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(groupNames);
        var serialized = Serialize(new InvocationMessage(methodName, args));
        return Task.WhenAll(groupNames.Select(group => PublishAsync(
            BackplaneNames.Group(group),
            BackplaneMessageKind.Invocation,
            BackplaneProtocol.WriteInvocation(new BackplaneInvocation(InvocationTarget.Group, group, null, serialized)),
            false,
            cancellationToken
        )));
    }

    /// <inheritdoc />
    public override Task SendUserAsync(string userId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return PublishInvocationAsync(BackplaneNames.User(userId), InvocationTarget.User, userId, null, methodName, args, cancellationToken);
    }

    /// <inheritdoc />
    public override Task SendUsersAsync(
        IReadOnlyList<string> userIds, string methodName, object?[] args, CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var serialized = Serialize(new InvocationMessage(methodName, args));
        return Task.WhenAll(userIds.Select(user => PublishAsync(
            BackplaneNames.User(user),
            BackplaneMessageKind.Invocation,
            BackplaneProtocol.WriteInvocation(new BackplaneInvocation(InvocationTarget.User, user, null, serialized)),
            false,
            cancellationToken
        )));
    }

    /// <inheritdoc />
    public override async Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        ArgumentNullException.ThrowIfNull(groupName);
        var connection = connections[connectionId];
        if (connection is not null)
        {
            await ChangeLocalGroupAsync(connection, groupName, GroupAction.Add, cancellationToken).ConfigureAwait(false);
            return;
        }
        await SendGroupCommandAsync(GroupAction.Add, connectionId, groupName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        ArgumentNullException.ThrowIfNull(groupName);
        var connection = connections[connectionId];
        if (connection is not null)
        {
            await ChangeLocalGroupAsync(connection, groupName, GroupAction.Remove, cancellationToken).ConfigureAwait(false);
            return;
        }
        await SendGroupCommandAsync(GroupAction.Remove, connectionId, groupName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<T> InvokeConnectionAsync<T>(
        string connectionId, string methodName, object?[] args, CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        var invocationId = $"{options.ServerName}:{Interlocked.Increment(ref lastInvocationId)}";
        var result = clientResults.Add<T>(connectionId, invocationId, cancellationToken);
        var invocation = new InvocationMessage(invocationId, methodName, args);
        try
        {
            var connection = connections[connectionId];
            if (connection is not null)
                await connection.WriteAsync(invocation, cancellationToken).ConfigureAwait(false);
            else
                // mandatory: if no server holds the connection the broker returns the message
                await PublishAsync(
                    BackplaneNames.Connection(connectionId),
                    BackplaneMessageKind.Invocation,
                    BackplaneProtocol.WriteInvocation(new BackplaneInvocation(
                        InvocationTarget.Connection, connectionId, null, Serialize(invocation), invocationId, options.ServerName
                    )),
                    true,
                    cancellationToken
                ).ConfigureAwait(false);
        }
        catch (UnroutableMessageException)
        {
            clientResults.Remove(invocationId);
            throw new IOException($"Connection '{connectionId}' does not exist.");
        }
        catch
        {
            clientResults.Remove(invocationId);
            throw;
        }
        return await result.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SetConnectionResultAsync(string connectionId, CompletionMessage result)
    {
        if (clientResults.TryComplete(connectionId, result))
            return;
        if (result.InvocationId is not null && clientResults.TryTakeForwarded(connectionId, result.InvocationId, out var invocation))
            await ForwardCompletionAsync(invocation, result, default).ConfigureAwait(false);
        // anything else: a late or forged result, ignored like the default lifetime manager does
    }

    /// <inheritdoc />
    public override bool TryGetReturnType(string invocationId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Type? type)
        => clientResults.TryGetReturnType(invocationId, out type);

    /// <summary>Stops consuming and deletes this server's queue</summary>
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        clientResults.FailAll(new IOException("The SignalR backplane is shutting down."));
        if (runtime is { } current)
        {
            if (current.Consumer is { } consumer)
                await consumer.DisposeAsync().ConfigureAwait(false);
            try
            {
                await current.Topology.DeleteQueueAsync(names.Queue).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // best effort: x-expires removes it otherwise
            }
            await current.ProducerChannel.DisposeAsync().ConfigureAwait(false);
            await current.ConsumerChannel.DisposeAsync().ConfigureAwait(false);
            await current.ProducerConnection.DisposeAsync().ConfigureAwait(false);
            await current.ConsumerConnection.DisposeAsync().ConfigureAwait(false);
        }
        subscriptions.Dispose();
        initLock.Dispose();
    }

    /// <inheritdoc />
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    async Task IBackplaneResync.ResyncAsync()
    {
        if (disposed || runtime is not { } current || Interlocked.Exchange(ref resyncing, 1) == 1)
            return;
        try
        {
            await subscriptions.WaitAsync().ConfigureAwait(false);
            try
            {
                if (current.Consumer is { } old)
                    await old.DisposeAsync().ConfigureAwait(false);
                current.Consumer = null;
                await DeclareTopologyAsync(current, CancellationToken.None).ConfigureAwait(false);
                current.Consumer = await StartConsumerAsync(current, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                subscriptions.Release();
            }
            BackplaneMetrics.Resynced(hubName);
            logger.BackplaneResynced(hubName);
        }
        catch (Exception exception)
        {
            logger.BackplaneResyncFailed(hubName, exception);
        }
        finally
        {
            Volatile.Write(ref resyncing, 0);
        }
    }

    internal Task ResyncAsync() => ((IBackplaneResync)this).ResyncAsync();

    internal BackplaneNames Names => names;

    private async Task ChangeLocalGroupAsync(HubConnectionContext connection, string groupName, GroupAction action, CancellationToken cancellationToken)
    {
        var feature = connection.Features.Get<BackplaneConnectionFeature>();
        await subscriptions.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (action == GroupAction.Add)
            {
                if (feature?.AddGroup(groupName) == false) return;
                await AddToStoreAsync(groups, groupName, connection, BackplaneNames.Group(groupName)).ConfigureAwait(false);
            }
            else
            {
                if (feature?.RemoveGroup(groupName) == false) return;
                await RemoveFromStoreAsync(groups, groupName, connection, BackplaneNames.Group(groupName)).ConfigureAwait(false);
            }
        }
        finally
        {
            subscriptions.Release();
        }
    }

    // callers hold the subscriptions lock
    private async Task AddToStoreAsync(
        ConcurrentDictionary<string, HubConnectionStore> stores, string name, HubConnectionContext connection, string routingKey
    )
    {
        var store = stores.GetOrAdd(name, static _ => new HubConnectionStore());
        store.Add(connection);
        if (store.Count == 1)
            await BindAsync(routingKey, default).ConfigureAwait(false);
    }

    // callers hold the subscriptions lock
    private async Task RemoveFromStoreAsync(
        ConcurrentDictionary<string, HubConnectionStore> stores, string name, HubConnectionContext connection, string routingKey
    )
    {
        if (!stores.TryGetValue(name, out var store))
            return;
        store.Remove(connection);
        if (store.Count == 0)
        {
            stores.TryRemove(name, out _);
            await UnbindAsync(routingKey).ConfigureAwait(false);
        }
    }

    private async Task SendGroupCommandAsync(GroupAction action, string connectionId, string groupName, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref lastAckId);
        var ack = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        acks[id] = ack;
        try
        {
            await PublishAsync(
                BackplaneNames.Groups,
                BackplaneMessageKind.GroupCommand,
                BackplaneProtocol.WriteGroupCommand(new BackplaneGroupCommand(id, options.ServerName, action, connectionId, groupName)),
                false,
                cancellationToken
            ).ConfigureAwait(false);
            await ack.Task.WaitAsync(options.AckTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            BackplaneMetrics.AckTimedOut(hubName);
            throw new TimeoutException(
                $"No server acknowledged {(action == GroupAction.Add ? "adding" : "removing")} connection '{connectionId}' "
                + $"{(action == GroupAction.Add ? "to" : "from")} group '{groupName}' within {options.AckTimeout}."
            );
        }
        finally
        {
            acks.TryRemove(id, out _);
        }
    }

    private Task PublishInvocationAsync(
        string routingKey,
        InvocationTarget target,
        string? targetName,
        IReadOnlyList<string>? excluded,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken
    )
    {
        var body = BackplaneProtocol.WriteInvocation(
            new BackplaneInvocation(target, targetName, excluded, Serialize(new InvocationMessage(methodName, args)))
        );
        return PublishAsync(routingKey, BackplaneMessageKind.Invocation, body, false, cancellationToken);
    }

    private IReadOnlyList<HubSerializedMessage> Serialize(HubMessage message)
    {
        var serialized = new HubSerializedMessage[protocols.Length];
        for (var i = 0; i < protocols.Length; i++)
            serialized[i] = new HubSerializedMessage(protocols[i].Name, protocols[i].GetMessageBytes(message));
        return serialized;
    }

    private async Task PublishAsync(
        string routingKey, BackplaneMessageKind kind, ReadOnlyMemory<byte> body, bool mandatory, CancellationToken cancellationToken
    )
    {
        var current = runtime ?? await EnsureRuntimeAsync(cancellationToken).ConfigureAwait(false);
        var context = new PublishContext(current.ProducerChannelContext)
        {
            Exchange = names.Exchange,
            RoutingKey = routingKey,
            Mandatory = mandatory,
            // the broker only reports a returned mandatory message through a publisher confirm
            PublisherConfirms = mandatory,
            Properties = new MessageProperties { DeliveryMode = MessageDeliveryMode.NonPersistent },
            Body = body,
            CancellationToken = cancellationToken,
        };
        await current.ProducerChannel.PublishAsync(context).ConfigureAwait(false);
        BackplaneMetrics.Published(hubName, kind);
    }

    private async Task ForwardCompletionAsync(ClientResults.ForwardedInvocation invocation, CompletionMessage completion, CancellationToken cancellationToken)
    {
        var bytes = invocation.Protocol.GetMessageBytes(completion);
        await PublishAsync(
            BackplaneNames.ReturnFor(invocation.ReturnServer),
            BackplaneMessageKind.Completion,
            BackplaneProtocol.WriteCompletion(new BackplaneCompletion(invocation.Protocol.Name, bytes)),
            false,
            cancellationToken
        ).ConfigureAwait(false);
    }

    // callers hold the subscriptions lock
    private async Task BindAsync(string routingKey, CancellationToken cancellationToken)
    {
        if (!bindings.Add(routingKey)) return;
        var current = runtime ?? await EnsureRuntimeAsync(cancellationToken).ConfigureAwait(false);
        await current.Topology.BindAsync(new BindingDefinition(names.Exchange, names.Queue, routingKey), cancellationToken).ConfigureAwait(false);
    }

    // callers hold the subscriptions lock
    private async Task UnbindAsync(string routingKey)
    {
        if (!bindings.Remove(routingKey) || runtime is not { } current) return;
        await current.Topology.UnbindAsync(new BindingDefinition(names.Exchange, names.Queue, routingKey)).ConfigureAwait(false);
    }

    private async ValueTask<Runtime> EnsureRuntimeAsync(CancellationToken cancellationToken)
    {
        if (runtime is { } ready) return ready;
        ObjectDisposedException.ThrowIf(disposed, this);
        await initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (runtime is { } initialized) return initialized;

            var producerContext = new ConnectionContext($"EasyNetQ.SignalR.{hubName}.Producer", services);
            producerContext.Set(Keys.ConnectionType, PersistentConnectionType.Producer);
            var producerConnection = await transport.ConnectAsync(producerContext, cancellationToken).ConfigureAwait(false);
            var producerChannelContext = new ChannelContext(producerContext);
            var producerChannel = await producerConnection.OpenChannelAsync(producerChannelContext, cancellationToken).ConfigureAwait(false);

            var consumerContext = new ConnectionContext($"EasyNetQ.SignalR.{hubName}.Consumer", services);
            consumerContext.Set(Keys.ConnectionType, PersistentConnectionType.Consumer);
            consumerContext.Set(BackplaneKeys.Owner, (IBackplaneResync)this);
            var consumerConnection = await transport.ConnectAsync(consumerContext, cancellationToken).ConfigureAwait(false);
            var consumerChannelContext = new ChannelContext(consumerContext);
            var consumerChannel = await consumerConnection.OpenChannelAsync(consumerChannelContext, cancellationToken).ConfigureAwait(false);

            var topology = consumerChannel.Topology
                ?? throw new EasyNetQException("The SignalR backplane needs a transport that declares topology (exchanges, queues, bindings).");

            var built = new Runtime
            {
                ProducerConnection = producerConnection,
                ConsumerConnection = consumerConnection,
                ProducerChannel = producerChannel,
                ConsumerChannel = consumerChannel,
                ProducerChannelContext = producerChannelContext,
                ConsumerChannelContext = consumerChannelContext,
                Topology = topology,
            };
            await DeclareTopologyAsync(built, cancellationToken).ConfigureAwait(false);
            built.Consumer = await StartConsumerAsync(built, cancellationToken).ConfigureAwait(false);
            runtime = built;
            logger.BackplaneStarted(hubName, names.Queue);
            return built;
        }
        finally
        {
            initLock.Release();
        }
    }

    private async Task DeclareTopologyAsync(Runtime current, CancellationToken cancellationToken)
    {
        var topology = current.Topology;
        await topology.DeclareExchangeAsync(new ExchangeDefinition(names.Exchange, ExchangeType.Direct), cancellationToken).ConfigureAwait(false);
        // durable: RabbitMQ 4 refuses transient non-exclusive queues, and a durable one also keeps queued sends
        // across a connection blip or broker restart; x-expires still removes it once its server is gone
        await topology.DeclareQueueAsync(
            new QueueDefinition(names.Queue, Durable: true)
            {
                Arguments = new Dictionary<string, object> { ["x-expires"] = (int)options.QueueExpiry.TotalMilliseconds },
            },
            cancellationToken
        ).ConfigureAwait(false);
        foreach (var key in new[] { BackplaneNames.All, BackplaneNames.Groups, names.OwnAck, names.OwnReturn })
            await topology.BindAsync(new BindingDefinition(names.Exchange, names.Queue, key), cancellationToken).ConfigureAwait(false);
        foreach (var key in bindings)
            await topology.BindAsync(new BindingDefinition(names.Exchange, names.Queue, key), cancellationToken).ConfigureAwait(false);
    }

    private ValueTask<ITransportConsumer> StartConsumerAsync(Runtime current, CancellationToken cancellationToken)
    {
        var consumer = new ConsumerContext(current.ConsumerChannelContext, names.Queue)
        {
            PrefetchCount = options.PrefetchCount,
            MessagePipeline = OnMessageAsync,
        };
        return current.ConsumerChannel.StartConsumerAsync([consumer], cancellationToken);
    }

    private async ValueTask OnMessageAsync(ConsumeContext context)
    {
        context.Ack = AckDecision.Ack;
        try
        {
            var kind = BackplaneProtocol.ReadKind(context.Body.Span);
            BackplaneMetrics.Received(hubName, kind);
            switch (kind)
            {
                case BackplaneMessageKind.Invocation:
                    await DeliverInvocationAsync(BackplaneProtocol.ReadInvocation(context.Body)).ConfigureAwait(false);
                    break;
                case BackplaneMessageKind.GroupCommand:
                    await ApplyGroupCommandAsync(BackplaneProtocol.ReadGroupCommand(context.Body)).ConfigureAwait(false);
                    break;
                case BackplaneMessageKind.Ack:
                    if (acks.TryGetValue(BackplaneProtocol.ReadAck(context.Body), out var ack))
                        ack.TrySetResult();
                    break;
                case BackplaneMessageKind.Completion:
                    ReceiveCompletion(BackplaneProtocol.ReadCompletion(context.Body));
                    break;
                default:
                    throw new InvalidDataException($"Unknown backplane message kind {kind}");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // a malformed or undeliverable message must not stall the server's queue
            logger.InvalidBackplaneMessage(hubName, exception);
        }
    }

    private async Task DeliverInvocationAsync(BackplaneInvocation invocation)
    {
        var message = new SerializedHubMessage(invocation.Messages);
        switch (invocation.Target)
        {
            case InvocationTarget.All:
                await WriteAsync(connections, message, invocation.ExcludedConnectionIds).ConfigureAwait(false);
                break;
            case InvocationTarget.Group when groups.TryGetValue(invocation.TargetName!, out var group):
                await WriteAsync(group, message, invocation.ExcludedConnectionIds).ConfigureAwait(false);
                break;
            case InvocationTarget.User when users.TryGetValue(invocation.TargetName!, out var user):
                await WriteAsync(user, message, invocation.ExcludedConnectionIds).ConfigureAwait(false);
                break;
            case InvocationTarget.Connection when connections[invocation.TargetName!] is { } connection:
                if (invocation.InvocationId is not null && invocation.ReturnServer is not null)
                    clientResults.AddForwarded(
                        invocation.InvocationId,
                        new ClientResults.ForwardedInvocation(connection.ConnectionId, invocation.ReturnServer, connection.Protocol)
                    );
                await WriteSafeAsync(connection, message).ConfigureAwait(false);
                break;
        }
    }

    private async Task WriteAsync(HubConnectionStore store, SerializedHubMessage message, IReadOnlyList<string>? excluded)
    {
        List<Task>? writes = null;
        foreach (var connection in store)
        {
            if (excluded is not null && excluded.Contains(connection.ConnectionId))
                continue;
            var write = connection.WriteAsync(message);
            if (write.IsCompletedSuccessfully) continue;
            (writes ??= new List<Task>()).Add(ObserveAsync(write));
        }
        if (writes is not null)
            await Task.WhenAll(writes).ConfigureAwait(false);
    }

    private async Task WriteSafeAsync(HubConnectionContext connection, SerializedHubMessage message)
    {
        try
        {
            await connection.WriteAsync(message).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LocalWriteFailed(hubName, exception);
        }
    }

    private async Task ObserveAsync(ValueTask write)
    {
        try
        {
            await write.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // one broken connection must not fail delivery to the others
            logger.LocalWriteFailed(hubName, exception);
        }
    }

    private async Task ApplyGroupCommandAsync(BackplaneGroupCommand command)
    {
        if (connections[command.ConnectionId] is not { } connection)
            return;
        await ChangeLocalGroupAsync(connection, command.GroupName, command.Action, default).ConfigureAwait(false);
        await PublishAsync(
            BackplaneNames.AckFor(command.OriginServer), BackplaneMessageKind.Ack, BackplaneProtocol.WriteAck(command.Id), false, default
        ).ConfigureAwait(false);
    }

    private void ReceiveCompletion(BackplaneCompletion completion)
    {
        var protocol = allProtocols.FirstOrDefault(p => p.Name.Equals(completion.ProtocolName, StringComparison.OrdinalIgnoreCase));
        if (protocol is null)
        {
            logger.UnknownProtocolReceived(hubName, completion.ProtocolName);
            return;
        }
        var sequence = new System.Buffers.ReadOnlySequence<byte>(completion.Completion);
        if (!protocol.TryParseMessage(ref sequence, clientResults, out var message) || message is not CompletionMessage result)
            throw new InvalidDataException("Client result is not a completion message");
        if (!clientResults.TryComplete(null, result))
            logger.UnknownCompletionReceived(hubName, result.InvocationId ?? "");
    }
}

/// <summary>Groups a connection joined on this server; cleared on disconnect</summary>
internal sealed class BackplaneConnectionFeature
{
    private readonly HashSet<string> groups = new(StringComparer.Ordinal);

    public bool AddGroup(string group)
    {
        lock (groups) return groups.Add(group);
    }

    public bool RemoveGroup(string group)
    {
        lock (groups) return groups.Remove(group);
    }

    public string[] TakeGroups()
    {
        lock (groups)
        {
            var taken = groups.ToArray();
            groups.Clear();
            return taken;
        }
    }
}
