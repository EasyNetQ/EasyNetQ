using System;
using EasyNetQ;

namespace EasyNetQ.Tests.Integration
{
    public class DefaultExchangeConventions : IConventions
    {
        public DefaultExchangeConventions(ITypeNameSerializer typeNameSerializer)
        {
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
                                : string.Format("{0}_{1}", typeName, subscriptionId);
                        }

                        return string.IsNullOrEmpty(subscriptionId)
                            ? attr.QueueName
                            : string.Format("{0}_{1}", attr.QueueName, subscriptionId);
                    };
            RpcRoutingKeyNamingConvention = typeNameSerializer.Serialize;

            ErrorQueueNamingConvention = () => "EasyNetQ_Default_Error_Queue";
            ErrorExchangeNamingConvention = info => "ErrorExchange_" + info.RoutingKey;
            RpcRequestExchangeNamingConvention = (type) => "easy_net_q_rpc";
            RpcResponseExchangeNamingConvention = (type) => "";
            RpcReturnQueueNamingConvention = () => "easynetq.response." + Guid.NewGuid();

            ConsumerTagConvention = () => Guid.NewGuid().ToString();
        }

        private QueueAttribute GetQueueAttribute(Type messageType)
        {
            return messageType.GetAttribute<QueueAttribute>() ?? new QueueAttribute(string.Empty);
        }

        public ExchangeNameConvention ExchangeNamingConvention { get; set; }
        public TopicNameConvention TopicNamingConvention { get; set; }
        public QueueNameConvention QueueNamingConvention { get; set; }
        public RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }

        public ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }
        public ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }
        public RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; set; }
        public RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; set; }
        public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

        public ConsumerTagConvention ConsumerTagConvention { get; set; }
    }
}