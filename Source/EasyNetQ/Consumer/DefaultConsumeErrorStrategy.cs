using System.Buffers;
using System.Collections.Concurrent;
using EasyNetQ.Logging;
using EasyNetQ.SystemMessages;
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
    public virtual ValueTask<AckStrategy> HandleErrorAsync(ConsumeContext context, Exception exception)
    {
        var receivedInfo = context.ReceivedInfo;
        var properties = context.Properties;
        var body = context.Body.ToArray();

        logger.Error(
            exception,
            "Exception thrown by subscription callback, receivedInfo={receivedInfo}, properties={properties}, message={message}",
            receivedInfo,
            properties,
            Convert.ToBase64String(body)
        );

        try
        {
            using var model = connection.CreateModel();
            if (configuration.PublisherConfirms) model.ConfirmSelect();

            var errorExchange = DeclareErrorExchangeWithQueue(model, receivedInfo);

            using var message = CreateErrorMessage(receivedInfo, properties, body, exception);

            var errorProperties = model.CreateBasicProperties();
            errorProperties.Persistent = true;
            errorProperties.Type = typeNameSerializer.Serialize(typeof(Error));

            model.BasicPublish(errorExchange, receivedInfo.RoutingKey, errorProperties, message.Memory);

            return new ValueTask<AckStrategy>(
                configuration.PublisherConfirms
                    ? model.WaitForConfirms(configuration.Timeout) ? AckStrategies.Ack : AckStrategies.NackWithRequeue
                    : AckStrategies.Ack
            );
        }
        catch (BrokerUnreachableException unreachableException)
        {
            // thrown if the broker is unreachable during initial creation.
            logger.Error(
                unreachableException,
                "Cannot connect to broker while attempting to publish error message"
            );
        }
        catch (OperationInterruptedException interruptedException)
        {
            // thrown if the broker connection is broken during declare or publish.
            logger.Error(
                interruptedException,
                "Broker connection was closed while attempting to publish error message"
            );
        }
        catch (Exception unexpectedException)
        {
            // Something else unexpected has gone wrong :(
            logger.Error(unexpectedException, "Failed to publish error message");
        }

        return new ValueTask<AckStrategy>(AckStrategies.NackWithRequeue);
    }

    /// <inheritdoc />
    public virtual ValueTask<AckStrategy> HandleCancelledAsync(ConsumeContext context) => new(AckStrategies.NackWithRequeue);

    private static void DeclareAndBindErrorExchangeWithErrorQueue(
        IModel model,
        string exchangeName,
        string exchangeType,
        string queueName,
        string? queueType,
        string routingKey
    )
    {
        Dictionary<string, object>? queueArgs = null;
        if (queueType != null)
        {
            queueArgs = new Dictionary<string, object> { { "x-queue-type", queueType } };
        }

        model.QueueDeclare(queueName, true, false, false, queueArgs);
        model.ExchangeDeclare(exchangeName, exchangeType, true);
        model.QueueBind(queueName, exchangeName, routingKey);
    }

    private string DeclareErrorExchangeWithQueue(IModel model, MessageReceivedInfo receivedInfo)
    {
        var errorExchangeName = conventions.ErrorExchangeNamingConvention(receivedInfo);
        var errorExchangeType = conventions.ErrorExchangeTypeConvention();
        var errorQueueName = conventions.ErrorQueueNamingConvention(receivedInfo);
        var errorQueueType = conventions.ErrorQueueTypeConvention();
        var routingKey = conventions.ErrorExchangeRoutingKeyConvention(receivedInfo);

        var errorTopologyIdentifier = $"{errorExchangeName}-{errorQueueName}-{routingKey}";

        existingErrorExchangesWithQueues.GetOrAdd(errorTopologyIdentifier, _ =>
        {
            DeclareAndBindErrorExchangeWithErrorQueue(model, errorExchangeName, errorExchangeType, errorQueueName, errorQueueType, routingKey);
            return true;
        });

        return errorExchangeName;
    }

    private IMemoryOwner<byte> CreateErrorMessage(
        in MessageReceivedInfo receivedInfo, in MessageProperties properties, byte[] body, Exception exception
    )
    {
        var error = new Error(
            receivedInfo.RoutingKey,
            receivedInfo.Exchange,
            receivedInfo.Queue,
            exception.ToString(),
            errorMessageSerializer.Serialize(body),
            DateTime.UtcNow,
            properties
        );
        return serializer.MessageToBytes(typeof(Error), error);
    }
}
