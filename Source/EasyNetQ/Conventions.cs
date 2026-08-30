using EasyNetQ.Internals;

namespace EasyNetQ;

/// <summary>
///     Convention for exchange naming
/// </summary>
public delegate string ExchangeNameConvention(Type messageType);
/// <summary>
///     Convention for exchange type
/// </summary>
public delegate string ExchangeTypeConvention(Type messageType);
/// <summary>
///     Convention for topic naming
/// </summary>
public delegate string TopicNameConvention(Type messageType);

/// <summary>
///     Convention for queue naming
/// </summary>
public delegate string QueueNameConvention(Type messageType, string subscriberId);

/// <summary>
///     Convention for queue type
/// </summary>
public delegate string QueueTypeConvention(Type messageType);

/// <summary>
///     Convention for error queue routing key naming
/// </summary>
public delegate string ErrorQueueNameConvention(MessageReceivedInfo receivedInfo);

/// <summary>
///     Convention for error queue type
/// </summary>
public delegate string ErrorQueueTypeConvention();

/// <summary>
///     Convention for error exchange naming
/// </summary>
public delegate string ErrorExchangeNameConvention(MessageReceivedInfo receivedInfo);

/// <summary>
///     Convention for error exchange type
/// </summary>
public delegate string ErrorExchangeTypeConvention();


/// <summary>
///     Convention for error exchange Routing Key
/// </summary>
public delegate string ErrorExchangeRoutingKeyConvention(MessageReceivedInfo receivedInfo);

/// <summary>
///     Convention for rpc routing key naming
/// </summary>
public delegate string RpcRoutingKeyNamingConvention(Type messageType);

/// <summary>
///     Convention for RPC exchange naming
/// </summary>
public delegate string RpcExchangeNameConvention(Type messageType);

/// <summary>
///     Convention for RPC return queue naming
/// </summary>
public delegate string RpcReturnQueueNamingConvention(Type messageType);

/// <summary>
///     Convention for consumer tag naming
/// </summary>
public delegate string ConsumerTagConvention();

/// <summary>
///     Represents various naming conventions
/// </summary>
public interface IConventions
{
    /// <summary>
    ///     Convention for exchange naming
    /// </summary>
    ExchangeNameConvention ExchangeNamingConvention { get; }
    /// <summary>
    ///     Convention for exchange type
    /// </summary>
    ExchangeTypeConvention ExchangeTypeConvention { get; }

    /// <summary>
    ///     Convention for topic naming
    /// </summary>
    TopicNameConvention TopicNamingConvention { get; }

    /// <summary>
    ///     Convention for queue naming
    /// </summary>
    QueueNameConvention QueueNamingConvention { get; }

    /// <summary>
    ///     Convention for queue type
    /// </summary>
    QueueTypeConvention QueueTypeConvention { get; }

    /// <summary>
    ///     Convention for RPC routing key naming
    /// </summary>
    RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; }

    /// <summary>
    ///     Convention for RPC request exchange naming
    /// </summary>
    RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; }

    /// <summary>
    ///     Convention for RPC response exchange naming
    /// </summary>
    RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; }

    /// <summary>
    ///     Convention for RPC return queue naming
    /// </summary>
    RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; }

    /// <summary>
    ///     Convention for consumer tag naming
    /// </summary>
    ConsumerTagConvention ConsumerTagConvention { get; }

    /// <summary>
    ///     Convention for error queue naming
    /// </summary>
    ErrorQueueNameConvention ErrorQueueNamingConvention { get; }

    /// <summary>
    ///     Convention for error queue type
    /// </summary>
    ErrorQueueTypeConvention ErrorQueueTypeConvention { get; }

    /// <summary>
    ///     Convention for error exchange naming
    /// </summary>
    ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; }

    /// <summary>
    ///     Convention for error exchange type
    /// </summary>
    ErrorExchangeTypeConvention ErrorExchangeTypeConvention { get; }

    /// <summary>
    ///     Convention for error exchange Routing key
    /// </summary>
    ErrorExchangeRoutingKeyConvention ErrorExchangeRoutingKeyConvention { get; }
}

/// <inheritdoc />
public class Conventions : IConventions
{
    /// <summary>
    ///     Creates Conventions
    /// </summary>
    public Conventions(ITypeNameSerializer typeNameSerializer, IMessageTypeRegistry messageTypeRegistry)
    {
        // Naming metadata comes from the message-type descriptor (populated once per type from attributes or by
        // generated registrations), so no attribute reflection happens per publish/subscribe
        ExchangeNamingConvention = type => messageTypeRegistry.GetOrAdd(type).ExchangeName ?? typeNameSerializer.Serialize(type);
        ExchangeTypeConvention = type => messageTypeRegistry.GetOrAdd(type).ExchangeType ?? ExchangeType.Topic;
        QueueTypeConvention = type => messageTypeRegistry.GetOrAdd(type).QueueType;

        TopicNamingConvention = _ => "";

        QueueNamingConvention = (type, subscriptionId) =>
        {
            var queueName = messageTypeRegistry.GetOrAdd(type).QueueName ?? typeNameSerializer.Serialize(type);
            return string.IsNullOrEmpty(subscriptionId) ? queueName : $"{queueName}_{subscriptionId}";
        };
        RpcRoutingKeyNamingConvention = typeNameSerializer.Serialize;

        ErrorQueueNamingConvention = _ => "EasyNetQ_Default_Error_Queue";
        ErrorExchangeNamingConvention = receivedInfo => "ErrorExchange_" + receivedInfo.RoutingKey;
        ErrorQueueTypeConvention = () => null;
        ErrorExchangeTypeConvention = () => ExchangeType.Direct;
        ErrorExchangeRoutingKeyConvention = receivedInfo => receivedInfo.RoutingKey;

        RpcRequestExchangeNamingConvention = _ => "easy_net_q_rpc";
        RpcResponseExchangeNamingConvention = _ => "easy_net_q_rpc";
        RpcReturnQueueNamingConvention = _ => "easynetq.response." + Guid.NewGuid();

        ConsumerTagConvention = () => Guid.NewGuid().ToString();
    }

    /// <inheritdoc />
    public ExchangeNameConvention ExchangeNamingConvention { get; set; }
    /// <inheritdoc />
    public ExchangeTypeConvention ExchangeTypeConvention { get; set; }
    /// <inheritdoc />
    public TopicNameConvention TopicNamingConvention { get; set; }

    /// <inheritdoc />
    public QueueNameConvention QueueNamingConvention { get; set; }

    /// <inheritdoc />
    public QueueTypeConvention QueueTypeConvention { get; set; }

    /// <inheritdoc />
    public RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }

    /// <inheritdoc />
    public ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }

    /// <inheritdoc />
    public ErrorQueueTypeConvention ErrorQueueTypeConvention { get; set; }

    /// <inheritdoc />
    public ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }

    /// <inheritdoc />
    public ErrorExchangeTypeConvention ErrorExchangeTypeConvention { get; set; }

    /// <inheritdoc />
    public RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; set; }

    /// <inheritdoc />
    public RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; set; }

    /// <inheritdoc />
    public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

    /// <inheritdoc />
    public ConsumerTagConvention ConsumerTagConvention { get; set; }

    /// <inheritdoc />
    public ErrorExchangeRoutingKeyConvention ErrorExchangeRoutingKeyConvention { get; set; }
}
