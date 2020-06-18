using System;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    public delegate string ExchangeNameConvention(Type messageType);

    public delegate string TopicNameConvention(Type messageType);

    public delegate string QueueNameConvention(Type messageType, string subscriberId);

    public delegate string RpcRoutingKeyNamingConvention(Type messageType);

    public delegate string ErrorQueueNameConvention(MessageReceivedInfo info);

    public delegate string ErrorExchangeNameConvention(MessageReceivedInfo info);

    public delegate string RpcExchangeNameConvention(Type messageType);

    public delegate string RpcReturnQueueNamingConvention();

    public delegate string ConsumerTagConvention();

    public interface IConventions
    {
        ExchangeNameConvention ExchangeNamingConvention { get; set; }
        TopicNameConvention TopicNamingConvention { get; set; }
        QueueNameConvention QueueNamingConvention { get; set; }
        RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }

        ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }
        ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }
        RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; set; }
        RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; set; }
        RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

        ConsumerTagConvention ConsumerTagConvention { get; set; }
    }

    /// <inheritdoc />
    public class Conventions : IConventions
    {
        public Conventions(ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            // Establish default conventions.
            ExchangeNamingConvention = messageType =>
            {
                var attr = GetQueueAttribute(messageType);

                return string.IsNullOrEmpty(attr.ExchangeName)
                    ? typeNameSerializer.Serialize(messageType)
                    : attr.ExchangeName;
            };

            TopicNamingConvention = messageType => "";

            QueueNamingConvention =
                (messageType, subscriptionId) =>
                {
                    var attr = GetQueueAttribute(messageType);

                    if (string.IsNullOrEmpty(attr.QueueName))
                    {
                        var typeName = typeNameSerializer.Serialize(messageType);

                        return string.IsNullOrEmpty(subscriptionId)
                            ? typeName
                            : $"{typeName}_{subscriptionId}";
                    }

                    return string.IsNullOrEmpty(subscriptionId)
                        ? attr.QueueName
                        : $"{attr.QueueName}_{subscriptionId}";
                };
            RpcRoutingKeyNamingConvention = typeNameSerializer.Serialize;

            ErrorQueueNamingConvention = info => "EasyNetQ_Default_Error_Queue";
            ErrorExchangeNamingConvention = info => "ErrorExchange_" + info.RoutingKey;
            RpcRequestExchangeNamingConvention = (type) => "easy_net_q_rpc";
            RpcResponseExchangeNamingConvention = (type) => "easy_net_q_rpc";
            RpcReturnQueueNamingConvention = () => "easynetq.response." + Guid.NewGuid();

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
