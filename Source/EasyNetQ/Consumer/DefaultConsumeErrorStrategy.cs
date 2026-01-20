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
    private readonly IConsumerConnection connection;
    private readonly IConventions conventions;
    private readonly IErrorMessageSerializer errorMessageSerializer;
    private readonly ConcurrentDictionary<string, bool> existingErrorExchangesWithQueues = new();
    private readonly ISerializer serializer;
    private readonly ITypeNameSerializer typeNameSerializer;
    private readonly ConnectionConfiguration configuration;

    /// <summary>
    ///     Creates DefaultConsumerErrorStrategy
    /// </summary>
    public DefaultConsumeErrorStrategy(
        ILogger<DefaultConsumeErrorStrategy> logger,
        IConsumerConnection connection,
        ISerializer serializer,
        IConventions conventions,
        ITypeNameSerializer typeNameSerializer,
        IErrorMessageSerializer errorMessageSerializer,
        ConnectionConfiguration configuration
    )
    {
        this.logger = logger;
        this.connection = connection;
        this.serializer = serializer;
        this.conventions = conventions;
        this.typeNameSerializer = typeNameSerializer;
        this.errorMessageSerializer = errorMessageSerializer;
        this.configuration = configuration;
    }

    /// <inheritdoc />
    public virtual async ValueTask<AckStrategyAsync> HandleErrorAsync(
        ConsumeContext context,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var receivedInfo = context.ReceivedInfo;
        var properties = context.Properties;
        var body = context.Body.ToArray();

        logger.LogError(
            exception,
            "Exception thrown by subscription callback, receivedInfo={ReceivedInfo}, properties={Properties}, message={Message}",
            receivedInfo,
            properties,
            Convert.ToBase64String(body)
        );

        try
        {
            var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(configuration.PublisherConfirms, configuration.PublisherConfirms),
                cancellationToken);

            var errorExchange = await DeclareErrorExchangeWithQueueAsync(channel, receivedInfo, cancellationToken);

            using var message = CreateErrorMessage(receivedInfo, properties, body, exception);

            var errorProperties = new BasicProperties
            {
                Persistent = true,
                Type = typeNameSerializer.Serialize(typeof(Error))
            };

            await channel.BasicPublishAsync(errorExchange, receivedInfo.RoutingKey, false, errorProperties, message.Memory, cancellationToken).ConfigureAwait(false);
            return AckStrategies.AckAsync;
        }
        catch (BrokerUnreachableException unreachableException)
        {
            // thrown if the broker is unreachable during initial creation.
            logger.LogError(
                unreachableException,
                "Cannot connect to broker while attempting to publish error message"
            );
        }
        catch (OperationInterruptedException interruptedException)
        {
            // thrown if the broker connection is broken during declare or publish.
            logger.LogError(
                interruptedException,
                "Broker connection was closed while attempting to publish error message"
            );
        }
        catch (Exception unexpectedException)
        {
            // Something else unexpected has gone wrong :(
            logger.LogError(unexpectedException, "Failed to publish error message");
        }

        return AckStrategies.NackWithRequeueAsync;
    }

    /// <inheritdoc />
    public virtual ValueTask<AckStrategyAsync> HandleCancelledAsync(ConsumeContext context, CancellationToken cancellationToken = default)
    {
        return new(AckStrategies.NackWithRequeueAsync);
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
        return serializer.MessageToBytes(typeof(Error), error);
    }
}
