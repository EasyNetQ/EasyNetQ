using System.Diagnostics;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Persistent;

/// <inheritdoc />
public sealed class PersistentChannel : IPersistentChannel
{
    private const int MinRetryTimeoutMs = 50;
    private const int MaxRetryTimeoutMs = 5000;
    private readonly IPersistentConnection connection;

    private readonly CancellationTokenSource disposeCts = new();
    private readonly IEventBus eventBus;
    private readonly AsyncLock mutex = new();
    private readonly PersistentChannelOptions options;
    private readonly ILogger<PersistentChannel> logger;

    private volatile IChannel initializedChannel;
    private volatile bool disposed;

    /// <summary>
    ///     Creates PersistentChannel
    /// </summary>
    /// <param name="options">The channel options</param>
    /// <param name="logger">The logger</param>
    /// <param name="connection">The connection</param>
    /// <param name="eventBus">The event bus</param>
    public PersistentChannel(
        in PersistentChannelOptions options,
        ILogger<PersistentChannel> logger,
        IPersistentConnection connection,
        IEventBus eventBus
    )
    {
        this.connection = connection;
        this.eventBus = eventBus;
        this.options = options;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<TResult> InvokeChannelActionAsync<TResult, TChannelAction>(
        TChannelAction channelAction, CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PersistentChannel));

        var (success, result) = await TryInvokeChannelActionFastAsync<TResult, TChannelAction>(channelAction, cancellationToken);
        return success
            ? result!
            : await InvokeChannelActionSlowAsync<TResult, TChannelAction>(channelAction, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        disposeCts.Cancel();
        mutex.Dispose();
        await CloseChannelAsync();
        disposeCts.Dispose();
    }

    private async Task<(bool Success, TResult Result)> TryInvokeChannelActionFastAsync<TResult, TChannelAction>(
    TChannelAction channelAction,
    CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        TResult result = default;

        if (mutex.TryAcquire(out var releaser))
        {
            try
            {
                var channel = initializedChannel ?? await CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                initializedChannel ??= channel;

                result = await channelAction.InvokeAsync(channel, cancellationToken);
                return (true, result);
            }
            catch (Exception exception)
            {
                var exceptionVerdict = GetExceptionVerdict(exception);
                if (exceptionVerdict.CloseChannel)
                    await CloseChannelAsync(cancellationToken);

                if (exceptionVerdict.Rethrow)
                    throw;

                logger.FailedToFastInvokeChannelAction(exception);
            }
            finally
            {
                try
                {
                    releaser.Dispose();
                }
                catch (Exception exception)
                {
                    logger.SemaphoreAlreadyDisposed(exception);
                }
            }
        }

        return (false, result);
    }


    private async Task<TResult> InvokeChannelActionSlowAsync<TResult, TChannelAction>(
    TChannelAction channelAction, CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disposeCts.Token);
        using var _ = await mutex.AcquireAsync(cts.Token).ConfigureAwait(false);

        var retryTimeoutMs = MinRetryTimeoutMs;

        while (true)
        {
            cts.Token.ThrowIfCancellationRequested();

            try
            {
                if (initializedChannel == null)
                {
                    initializedChannel = await CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                return await channelAction.InvokeAsync(initializedChannel, cancellationToken);
            }
            catch (Exception exception)
            {
                var exceptionVerdict = GetExceptionVerdict(exception);
                if (exceptionVerdict.CloseChannel)
                    await CloseChannelAsync(cancellationToken);

                if (exceptionVerdict.Rethrow)
                    throw;

                logger.FailedToInvokeChannelAction(exception);
            }

            await Task.Delay(retryTimeoutMs, cts.Token).ConfigureAwait(false);
            retryTimeoutMs = Math.Min(retryTimeoutMs * 2, MaxRetryTimeoutMs);
        }
    }

    private async Task<IChannel> CreateChannelAsync(CreateChannelOptions createChannelOptions = null, CancellationToken cancellationToken = default)


    {
        // publisherConfirmationTrackingEnabled must stay FALSE: with tracking enabled the client awaits the broker
        // confirm inside BasicPublishAsync, which runs while this channel's mutex is held - serializing all confirmed
        // publishes on the bus to one in flight. EasyNetQ tracks confirms itself (PublishConfirmationListener) and
        // awaits them outside the mutex. See When_a_channel_is_created_with_publisher_confirms.
        createChannelOptions ??= new CreateChannelOptions(options.PublisherConfirms, publisherConfirmationTrackingEnabled: false);
        var channel = await connection.CreateChannelAsync(createChannelOptions, cancellationToken).ConfigureAwait(false);
        AttachChannelEvents(channel);
        return channel;
    }

    private async ValueTask CloseChannelAsync(CancellationToken cancellationToken = default)
    {
        var channel = Interlocked.Exchange(ref initializedChannel, null);
        if (channel == null)
            return;

        await channel.CloseAsync(cancellationToken: cancellationToken);
        DetachChannelEvents(channel);
        await channel.DisposeAsync();
    }

    private void AttachChannelEvents(IChannel channel)
    {
        if (options.PublisherConfirms)
        {
            channel.BasicAcksAsync += OnAck;
            channel.BasicNacksAsync += OnNack;
        }

        channel.BasicReturnAsync += OnReturn;
        channel.ChannelShutdownAsync += OnChannelShutdown;

        if (channel is IRecoverable recoverable)
            recoverable.RecoveryAsync += OnChannelRecovered;
    }

    private void DetachChannelEvents(IChannel channel)
    {
        if (channel is IRecoverable recoverable)
            recoverable.RecoveryAsync -= OnChannelRecovered;

        channel.ChannelShutdownAsync -= OnChannelShutdown;
        channel.BasicReturnAsync -= OnReturn;

        if (!options.PublisherConfirms)
            return;

        channel.BasicNacksAsync -= OnNack;
        channel.BasicAcksAsync -= OnAck;
    }

    private Task OnChannelRecovered(object sender, AsyncEventArgs e)
    {
        return eventBus.PublishAsync(new ChannelRecoveredEvent((IChannel)sender!));
    }

    private Task OnChannelShutdown(object sender, ShutdownEventArgs e)
    {
        return eventBus.PublishAsync(new ChannelShutdownEvent((IChannel)sender!));
    }

    private Task OnReturn(object sender, BasicReturnEventArgs args)
    {
        var messageProperties = BasicPropertiesMapper.FromBasicProperties(args.BasicProperties);
        var messageReturnedInfo = new MessageReturnedInfo(args.Exchange, args.RoutingKey, args.ReplyText);
        var messageEvent = new ReturnedMessageEvent(
            (IChannel)sender!,
            args.Body,
            messageProperties,
            messageReturnedInfo
        );
        return eventBus.PublishAsync(messageEvent);
    }

    private async Task OnAck(object sender, BasicAckEventArgs args)
    {
        await eventBus.PublishAsync(MessageConfirmationEvent.Ack((IChannel)sender!, args.DeliveryTag, args.Multiple));
    }

    private Task OnNack(object sender, BasicNackEventArgs args)
    {
        return eventBus.PublishAsync(MessageConfirmationEvent.Nack((IChannel)sender!, args.DeliveryTag, args.Multiple));
    }

    private static ExceptionVerdict GetExceptionVerdict(Exception exception)
    {
        switch (exception)
        {
            case OperationInterruptedException e:
                return e.ShutdownReason?.ReplyCode switch
                {
                    AmqpErrorCodes.ConnectionClosed => ExceptionVerdict.Suppress,
                    AmqpErrorCodes.AccessRefused => ExceptionVerdict.ThrowAndCloseChannel,
                    AmqpErrorCodes.NotFound => ExceptionVerdict.ThrowAndCloseChannel,
                    AmqpErrorCodes.ResourceLocked => ExceptionVerdict.ThrowAndCloseChannel,
                    AmqpErrorCodes.PreconditionFailed => ExceptionVerdict.ThrowAndCloseChannel,
                    AmqpErrorCodes.InternalErrors => ExceptionVerdict.SuppressAndCloseChannel,
                    _ => ExceptionVerdict.Throw
                };
            case BrokerUnreachableException e:
                var isAuthenticationFailureException = e.InnerException is AuthenticationFailureException;
                return isAuthenticationFailureException
                    ? ExceptionVerdict.Throw
                    : ExceptionVerdict.Suppress;
            case EasyNetQException:
                return ExceptionVerdict.Suppress;
            default:
                return ExceptionVerdict.Throw;
        }
    }

    private readonly struct ExceptionVerdict
    {
        public static ExceptionVerdict Suppress { get; } = new(false, false);
        public static ExceptionVerdict SuppressAndCloseChannel { get; } = new(false, true);
        public static ExceptionVerdict Throw { get; } = new(true, false);
        public static ExceptionVerdict ThrowAndCloseChannel { get; } = new(true, true);

        private ExceptionVerdict(bool rethrow, bool closeChannel)
        {
            Rethrow = rethrow;
            CloseChannel = closeChannel;
        }

        public bool Rethrow { get; }
        public bool CloseChannel { get; }
    }
}
