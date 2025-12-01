using System.Diagnostics;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Persistent;

/// <inheritdoc />
public class PersistentChannel : IPersistentChannel
{
    private const string RequestPipeliningForbiddenMessage = "Pipelining of requests forbidden";

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

                logger.LogError(exception, "Failed to fast invoke channel action, invocation will be retried");
            }
            finally
            {
                releaser.Dispose();
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

                logger.LogError(exception, "Failed to invoke channel action, invocation will be retried");
            }

            await Task.Delay(retryTimeoutMs, cts.Token).ConfigureAwait(false);
            retryTimeoutMs = Math.Min(retryTimeoutMs * 2, MaxRetryTimeoutMs);
        }
    }

    private async Task<IChannel> CreateChannelAsync(CreateChannelOptions createChannelOptions = null, CancellationToken cancellationToken = default)


    {
        createChannelOptions ??= new CreateChannelOptions(options.PublisherConfirms, options.PublisherConfirms);
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
        else
            throw new NotSupportedException("Non-recoverable channel is not supported");
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
        var messageProperties = new MessageProperties(args.BasicProperties);
        var messageReturnedInfo = new MessageReturnedInfo(args.Exchange, args.RoutingKey, args.ReplyText);
        var messageEvent = new ReturnedMessageEvent(
            (IChannel)sender!,
            args.Body,
            messageProperties,
            messageReturnedInfo
        );
        return eventBus.PublishAsync(messageEvent);
        return Task.CompletedTask;
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
            case NotSupportedException e:
                var isRequestPipeliningForbiddenException = e.Message.Contains(RequestPipeliningForbiddenMessage);
                return isRequestPipeliningForbiddenException
                    ? ExceptionVerdict.SuppressAndCloseChannel
                    : ExceptionVerdict.Throw;
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
