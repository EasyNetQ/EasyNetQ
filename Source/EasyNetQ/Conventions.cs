using System;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     Convention for exchange naming
    /// </summary>
    public delegate string ExchangeNameConvention(Type messageType);

    /// <summary>
    ///     Convention for topic naming
    /// </summary>
    public delegate string TopicNameConvention(Type messageType);

    /// <summary>
    ///     Convention for queue naming
    /// </summary>
    public delegate string QueueNameConvention(Type messageType, string subscriberId);

    /// <summary>
    ///     Convention for error queue routing key naming
    /// </summary>
    public delegate string ErrorQueueNameConvention(MessageReceivedInfo receivedInfo);

    /// <summary>
    ///     Convention for error exchange naming
    /// </summary>
    public delegate string ErrorExchangeNameConvention(MessageReceivedInfo receivedInfo);

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
        ///     Convention for topic naming
        /// </summary>
        TopicNameConvention TopicNamingConvention { get; }

        /// <summary>
        ///     Convention for queue naming
        /// </summary>
        QueueNameConvention QueueNamingConvention { get; }

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
        ///     Convention for error exchange naming
        /// </summary>
        ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; }
    }

    /// <inheritdoc />
    public class Conventions : IConventions
    {
        /// <summary>
        ///     Creates Conventions
        /// </summary>
        public Conventions(ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            ExchangeNamingConvention = type =>
            {
                var attr = GetQueueAttribute(type);

                return string.IsNullOrEmpty(attr.ExchangeName)
                    ? typeNameSerializer.Serialize(type)
                    : attr.ExchangeName;
            };

            TopicNamingConvention = type => "";

            QueueNamingConvention = (type, subscriptionId) =>
                {
                    var attr = GetQueueAttribute(type);

                    if (string.IsNullOrEmpty(attr.QueueName))
                    {
                        var typeName = typeNameSerializer.Serialize(type);

                        return string.IsNullOrEmpty(subscriptionId)
                            ? typeName
                            : $"{typeName}_{subscriptionId}";
                    }

                    return string.IsNullOrEmpty(subscriptionId)
                        ? attr.QueueName
                        : $"{attr.QueueName}_{subscriptionId}";
                };
            RpcRoutingKeyNamingConvention = typeNameSerializer.Serialize;

            ErrorQueueNamingConvention = receivedInfo => "EasyNetQ_Default_Error_Queue";
            ErrorExchangeNamingConvention = receivedInfo => "ErrorExchange_" + receivedInfo.RoutingKey;
            RpcRequestExchangeNamingConvention = type => "easy_net_q_rpc";
            RpcResponseExchangeNamingConvention = type => "easy_net_q_rpc";
            RpcReturnQueueNamingConvention = type => "easynetq.response." + Guid.NewGuid();

            ConsumerTagConvention = () => Guid.NewGuid().ToString();
        }

        private QueueAttribute GetQueueAttribute(Type messageType)
        {
            return messageType.GetAttribute<QueueAttribute>() ?? QueueAttribute.Default;
        }

        /// <inheritdoc />
        public ExchangeNameConvention ExchangeNamingConvention { get; set; }

        /// <inheritdoc />
        public TopicNameConvention TopicNamingConvention { get; set; }

        /// <inheritdoc />
        public QueueNameConvention QueueNamingConvention { get; set; }

        /// <inheritdoc />
        public RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }

        /// <inheritdoc />
        public ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }

        /// <inheritdoc />
        public ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }

        /// <inheritdoc />
        public RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; set; }

        /// <inheritdoc />
        public RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; set; }

        /// <inheritdoc />
        public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

        /// <inheritdoc />
        public ConsumerTagConvention ConsumerTagConvention { get; set; }
	}
}
