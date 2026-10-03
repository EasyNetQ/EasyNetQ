using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Diagnostics;
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
    private readonly ConcurrentDictionary<string, bool> existingErrorExchangesWithQueues = new();
    private readonly IMessageSerializer serializer;
    private readonly MessageTypeDescriptor<Error> errorMessageDescriptor;
    private readonly ConnectionConfiguration configuration;
    private readonly ConsumeErrorOptions options;

    /// <summary>
    ///     Creates DefaultConsumerErrorStrategy with the default <see cref="ConsumeErrorOptions" />
    /// </summary>
    public DefaultConsumeErrorStrategy(
        ILogger<DefaultConsumeErrorStrategy> logger,
        IPersistentChannelDispatcher channelDispatcher,
        IMessageSerializer serializer,
        IMessageTypeRegistry registry,
        IConventions conventions,
        IErrorMessageSerializer errorMessageSerializer,
        ConnectionConfiguration configuration
    ) : this(logger, channelDispatcher, serializer, registry, conventions, errorMessageSerializer, configuration, new ConsumeErrorOptions())
    {
    }

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
        ConnectionConfiguration configuration,
        ConsumeErrorOptions options
    )
    {
        this.options = options;
        this.logger = logger;
        this.channelDispatcher = channelDispatcher;
        errorDispatchOptions = new PersistentChannelDispatchOptions("Error", PersistentConnectionType.Consumer, configuration.PublisherConfirms);
        this.serializer = serializer;
        errorMessageDescriptor = registry.GetOrAdd<Error>();
        this.conventions = conventions;
        this.errorMessageSerializer = errorMessageSerializer;
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
        if (options.LogMessageBody && logger.IsEnabled(LogLevel.Error))
        {
            // opt-in: bodies may carry personal data, and the error queue keeps them anyway
            logger.FailedMessageBody(receivedInfo.Queue, Convert.ToBase64String(body));
        }

        // the republish to the error queue gets its own PRODUCER span so failed messages are visible in traces
        using var errorPublishActivity = EasyNetQDiagnostics.Source.HasListeners()
            ? EasyNetQDiagnostics.Source.StartActivity($"send {conventions.ErrorExchangeNamingConvention(receivedInfo)}", System.Diagnostics.ActivityKind.Producer)
            : null;
        if (errorPublishActivity is not null)
        {
            errorPublishActivity.SetTag(MessagingTags.ErrorQueue, true);
            errorPublishActivity.SetTag(MessagingTags.DestinationName, conventions.ErrorExchangeNamingConvention(receivedInfo));
            errorPublishActivity.SetTag(MessagingTags.ErrorType, exception.GetType().FullName);
            if (properties.CorrelationIdPresent)
                errorPublishActivity.SetTag(MessagingTags.ConversationId, properties.CorrelationId);
        }

        try
        {
            // one long-lived channel per bus (with reconnect/retry) instead of a channel per failed message;
            // with publisher confirms on, the client-side tracking completes BasicPublishAsync only when the
            // broker confirms - serializing error publishes on this channel is fine at error-path volumes
            await channelDispatcher.InvokeAsync(
                async channel =>
                {
                    var errorExchange = await DeclareErrorExchangeWithQueueAsync(channel, receivedInfo, cancellationToken);

                    using var message = CreateErrorMessage(receivedInfo, properties, body, exception);

                    var errorProperties = new BasicProperties
                    {
                        Persistent = true,
                        Type = errorMessageDescriptor.WireName
                    };

                    await channel.BasicPublishAsync(errorExchange, receivedInfo.RoutingKey, false, errorProperties, message.Memory, cancellationToken).ConfigureAwait(false);
                    return true;
                },
                errorDispatchOptions,
                cancellationToken
            ).ConfigureAwait(false);

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

        errorPublishActivity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Error message publish failed");
        return AckDecision.NackRequeue;
    }

    /// <inheritdoc />
    public virtual ValueTask<AckDecision> HandleCancelledAsync(ConsumeContext context, CancellationToken cancellationToken = default)
    {
        return new(AckDecision.NackRequeue);
    }

    private async Task DeclareAndBindErrorExchangeWithErrorQueueAsync(
        IChannel channel,
        string exchangeName,
        string exchangeType,
        string queueName,
        string? queueType,
        string routingKey,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, object>? queueArgs = null;
        if (queueType != null)
            queueArgs = new Dictionary<string, object> { { Argument.QueueType, queueType } };
        if (options.ErrorQueueArguments is { Count: > 0 } configured)
        {
            queueArgs ??= new Dictionary<string, object>();
            foreach (var argument in configured)
                queueArgs[argument.Key] = argument.Value;
        }

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
