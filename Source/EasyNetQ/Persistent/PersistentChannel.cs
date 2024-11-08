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

    private volatile IChannel? initializedChannel;
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
    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        disposeCts.Cancel();
        mutex.Dispose();
        CloseChannel();
        disposeCts.Dispose();
    }

    private async Task<(bool Success, TResult? Result)> TryInvokeChannelActionFastAsync<TResult, TChannelAction>(
    TChannelAction channelAction,
    CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        TResult? result = default;

        if (mutex.TryAcquire(out var releaser))
        {
            try
            {
                var channel = initializedChannel ?? await CreateChannelAsync(cancellationToken).ConfigureAwait(false);
                // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
                result = await channelAction.InvokeAsync(channel, cancellationToken);
                return (true, result);
            }
            catch (Exception exception)
            {
                var exceptionVerdict = GetExceptionVerdict(exception);
                if (exceptionVerdict.CloseChannel)
                    CloseChannel(cancellationToken);

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
                initializedChannel ??= await CreateChannelAsync(cancellationToken).ConfigureAwait(false);
                return await channelAction.InvokeAsync(initializedChannel, cancellationToken);
            }
            catch (Exception exception)
            {
                var exceptionVerdict = GetExceptionVerdict(exception);
                if (exceptionVerdict.CloseChannel)
                    CloseChannel(cancellationToken);

                if (exceptionVerdict.Rethrow)
                    throw;

                logger.LogError(exception, "Failed to invoke channel action, invocation will be retried");
            }

            await Task.Delay(retryTimeoutMs, cts.Token).ConfigureAwait(false);
            retryTimeoutMs = Math.Min(retryTimeoutMs * 2, MaxRetryTimeoutMs);
        }
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken).ConfigureAwait(false);
        AttachChannelEvents(channel);
        return channel;
    }

    private void CloseChannel(CancellationToken cancellationToken = default)
    {
        var channel = Interlocked.Exchange(ref initializedChannel, null);
        if (channel == null)
            return;

        channel.CloseAsync(cancellationToken: cancellationToken);
        DetachChannelEvents(channel);
        channel.Dispose();
    }

    private void AttachChannelEvents(IChannel channel)
    {
        if (options.PublisherConfirms)
        {
            // channel.ConfirmSelectAsync();

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

    private Task OnChannelRecovered(object? sender, AsyncEventArgs e)
    {
        eventBus.Publish(new ChannelRecoveredEvent((IChannel)sender!));
        return Task.CompletedTask;
    }

    private Task OnChannelShutdown(object? sender, ShutdownEventArgs e)
    {
        eventBus.Publish(new ChannelShutdownEvent((IChannel)sender!));
        return Task.CompletedTask;
    }

    private Task OnReturn(object? sender, BasicReturnEventArgs args)
    {
        var messageProperties = new MessageProperties(args.BasicProperties);
        var messageReturnedInfo = new MessageReturnedInfo(args.Exchange, args.RoutingKey, args.ReplyText);
        var @event = new ReturnedMessageEvent(
            (IChannel)sender!,
            args.Body,
            messageProperties,
            messageReturnedInfo
        );
        eventBus.Publish(@event);
        return Task.CompletedTask;
    }

    private Task OnAck(object? sender, BasicAckEventArgs args)
    {
        eventBus.Publish(MessageConfirmationEvent.Ack((IChannel)sender!, args.DeliveryTag, args.Multiple));
        return Task.CompletedTask;
    }

    private Task OnNack(object? sender, BasicNackEventArgs args)
    {
        eventBus.Publish(MessageConfirmationEvent.Nack((IChannel)sender!, args.DeliveryTag, args.Multiple));
        return Task.CompletedTask;
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
