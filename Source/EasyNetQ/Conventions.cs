using System;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    public delegate string ExchangeNameConvention(Type messageType);

    public delegate string TopicNameConvention(Type messageType);

    public delegate string QueueNameConvention(Type messageType, string subscriberId);

    public delegate string RpcRoutingKeyNamingConvention(Type messageType);

    public delegate string ErrorQueueNameConvention(MessageReceivedInfo receivedInfo);

    public delegate string ErrorExchangeNameConvention(MessageReceivedInfo receivedInfo);

    public delegate string RpcExchangeNameConvention(Type messageType);

    public delegate string RpcReturnQueueNamingConvention(Type messageType);

    public delegate string ConsumerTagConvention();

    public interface IConventions
    {
        ExchangeNameConvention ExchangeNamingConvention { get; }
        TopicNameConvention TopicNamingConvention { get; }
        QueueNameConvention QueueNamingConvention { get; }
        RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; }

        ErrorQueueNameConvention ErrorQueueNamingConvention { get; }
        ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; }
        RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; }
        RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; }
        RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; }

        ConsumerTagConvention ConsumerTagConvention { get; }
    }

    /// <inheritdoc />
    public class Conventions : IConventions
    {
        public Conventions(ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            // Establish default conventions.
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
