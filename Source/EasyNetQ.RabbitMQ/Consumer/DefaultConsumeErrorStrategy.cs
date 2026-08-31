using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using System.Buffers;
using System.Collections.Concurrent;
using EasyNetQ.SystemMessages;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

/// <summary>
/// A strategy for dealing with failed messages. When a message consumer throws, HandleConsumerError is invoked.
///
/// The general principle is to put all failed messages in a dedicated error queue so that they can be
/// examined and retried (or ignored).
///
/// Each failed message is wrapped in a special system message, 'Error' and routed by a special exchange
/// named after the original message's routing key. This is so that ad-hoc queues can be attached for
/// errors on specific message types.
///
/// Each exchange is bound to the central EasyNetQ error queue.
/// </summary>
public class DefaultConsumeErrorStrategy : IConsumeErrorStrategy
{
    private readonly ILogger<DefaultConsumeErrorStrategy> logger;
    private readonly IPersistentChannelDispatcher channelDispatcher;
    private readonly PersistentChannelDispatchOptions errorDispatchOptions;
    private readonly IConventions conventions;
    private readonly IErrorMessageSerializer errorMessageSerializer;
    private readonly Producer.IPublishConfirmationListener confirmationListener;
    private readonly ConcurrentDictionary<string, bool> existingErrorExchangesWithQueues = new();
    private readonly IMessageSerializer serializer;
    private readonly MessageTypeDescriptor<Error> errorMessageDescriptor;
    private readonly ConnectionConfiguration configuration;

    /// <summary>
    ///     Creates DefaultConsumerErrorStrategy
    /// </summary>
    public DefaultConsumeErrorStrategy(
        ILogger<DefaultConsumeErrorStrategy> logger,
        IPersistentChannelDispatcher channelDispatcher,
        IMessageSerializer serializer,
        IMessageTypeRegistry registry,
        IConventions conventions,
        IErrorMessageSerializer errorMessageSerializer,
        Producer.IPublishConfirmationListener confirmationListener,
        ConnectionConfiguration configuration
    )
    {
        this.logger = logger;
        this.channelDispatcher = channelDispatcher;
        errorDispatchOptions = new PersistentChannelDispatchOptions("Error", PersistentConnectionType.Consumer, configuration.PublisherConfirms);
        this.serializer = serializer;
        errorMessageDescriptor = registry.GetOrAdd<Error>();
        this.conventions = conventions;
        this.errorMessageSerializer = errorMessageSerializer;
        this.confirmationListener = confirmationListener;
        this.configuration = configuration;
    }

    /// <inheritdoc />
    public virtual async ValueTask<AckDecision> HandleErrorAsync(
        ConsumeContext context,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var receivedInfo = context.ReceivedInfo;
        var properties = context.Properties;
        var body = context.Body.ToArray();

        logger.ConsumeCallbackFailed(exception, receivedInfo.Queue, receivedInfo.RoutingKey, receivedInfo.Exchange, properties.CorrelationId);
        if (logger.IsEnabled(LogLevel.Error))
        {
            // Materialize the base64 body string only when the log will actually be emitted
            logger.FailedMessageBody(receivedInfo.Queue, Convert.ToBase64String(body));
        }

        try
        {
            // one long-lived channel per bus (with reconnect/retry) instead of a channel per failed message;
            // with publisher confirms on, the confirmation is awaited outside the channel mutex
            var pendingConfirmation = await channelDispatcher.InvokeAsync(
                async channel =>
                {
                    var errorExchange = await DeclareErrorExchangeWithQueueAsync(channel, receivedInfo, cancellationToken);

                    using var message = CreateErrorMessage(receivedInfo, properties, body, exception);

                    var errorProperties = new BasicProperties
                    {
                        Persistent = true,
                        Type = errorMessageDescriptor.WireName
                    };

                    var confirmation = configuration.PublisherConfirms
                        ? await confirmationListener.CreatePendingConfirmationAsync(channel, cancellationToken).ConfigureAwait(false)
                        : null;
                    await channel.BasicPublishAsync(errorExchange, receivedInfo.RoutingKey, false, errorProperties, message.Memory, cancellationToken).ConfigureAwait(false);
                    return confirmation;
                },
                errorDispatchOptions,
                cancellationToken
            ).ConfigureAwait(false);

            if (pendingConfirmation is not null)
                await pendingConfirmation.WaitAsync(cancellationToken).ConfigureAwait(false);
            return AckDecision.Ack;
        }
        catch (BrokerUnreachableException unreachableException)
        {
            // thrown if the broker is unreachable during initial creation.
            logger.CannotConnectToBrokerForErrorPublish(unreachableException);
        }
        catch (OperationInterruptedException interruptedException)
        {
            // thrown if the broker connection is broken during declare or publish.
            logger.BrokerConnectionClosedForErrorPublish(interruptedException);
        }
        catch (Exception unexpectedException)
        {
            // Something else unexpected has gone wrong :(
            logger.FailedToPublishErrorMessage(unexpectedException);
        }

        return AckDecision.NackRequeue;
    }

    /// <inheritdoc />
    public virtual ValueTask<AckDecision> HandleCancelledAsync(ConsumeContext context, CancellationToken cancellationToken = default)
    {
        return new(AckDecision.NackRequeue);
    }

    private static async Task DeclareAndBindErrorExchangeWithErrorQueueAsync(
        IChannel channel,
        string exchangeName,
        string exchangeType,
        string queueName,
        string queueType,
        string routingKey,
        CancellationToken cancellationToken
    )
    {
        var queueArgs = queueType != null
            ? new Dictionary<string, object> { { Argument.QueueType, queueType } }
            : null;

        await channel.QueueDeclareAsync(queueName, true, false, false, queueArgs, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(exchangeName, exchangeType, true, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName, exchangeName, routingKey, cancellationToken: cancellationToken);
    }

    private async Task<string> DeclareErrorExchangeWithQueueAsync(IChannel channel, MessageReceivedInfo receivedInfo, CancellationToken cancellationToken = default)
    {
        var errorExchangeName = conventions.ErrorExchangeNamingConvention(receivedInfo);
        var errorExchangeType = conventions.ErrorExchangeTypeConvention();
        var errorQueueName = conventions.ErrorQueueNamingConvention(receivedInfo);
        var errorQueueType = conventions.ErrorQueueTypeConvention();
        var routingKey = conventions.ErrorExchangeRoutingKeyConvention(receivedInfo);

        var errorTopologyIdentifier = $"{errorExchangeName}-{errorQueueName}-{routingKey}";

        if (!existingErrorExchangesWithQueues.ContainsKey(errorTopologyIdentifier))
        {
            await DeclareAndBindErrorExchangeWithErrorQueueAsync(channel, errorExchangeName, errorExchangeType, errorQueueName, errorQueueType, routingKey, cancellationToken);
            existingErrorExchangesWithQueues.GetOrAdd(errorTopologyIdentifier, true);
        }

        return errorExchangeName;
    }

    private IMemoryOwner<byte> CreateErrorMessage(
        in MessageReceivedInfo receivedInfo, in MessageProperties properties, byte[] body, Exception exception
    )
    {
        var message = errorMessageSerializer.Serialize(body);
        var error = new Error(
            receivedInfo.RoutingKey,
            receivedInfo.Exchange,
            receivedInfo.Queue,
            exception.ToString(),
            message,
            DateTime.UtcNow,
            properties
        );
        return serializer.Serialize(error, errorMessageDescriptor);
    }
}
