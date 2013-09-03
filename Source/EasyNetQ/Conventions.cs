using System;


namespace EasyNetQ
{
	public delegate string ExchangeNameConvention(Type messageType);
	public delegate string TopicNameConvention(Type messageType);
    public delegate string QueueNameConvention(Type messageType, string subscriberId);
    public delegate string RpcRoutingKeyNamingConvention(Type messageType);

    public delegate string ErrorQueueNameConvention();
    public delegate string ErrorExchangeNameConvention(string  originalRoutingKey);
    public delegate string RpcExchangeNameConvention();

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
        RpcExchangeNameConvention RpcExchangeNamingConvention { get; set; }
        RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

        ConsumerTagConvention ConsumerTagConvention { get; set; }
	}

	public class Conventions : IConventions
	{
		public Conventions()
		{
			// Establish default conventions.
			ExchangeNamingConvention = TypeNameSerializer.Serialize;
			TopicNamingConvention = messageType => "";
			QueueNamingConvention =
					(messageType, subscriptionId) =>
					{
						var typeName = TypeNameSerializer.Serialize(messageType);
						return string.Format("{0}_{1}", typeName, subscriptionId);
					};
            RpcRoutingKeyNamingConvention = TypeNameSerializer.Serialize;

            ErrorQueueNamingConvention = () => "EasyNetQ_Default_Error_Queue";
            ErrorExchangeNamingConvention = (originalRoutingKey) => "ErrorExchange_" + originalRoutingKey;
            RpcExchangeNamingConvention = () => "easy_net_q_rpc";
		    RpcReturnQueueNamingConvention = () => "easynetq.response." + Guid.NewGuid().ToString();

            ConsumerTagConvention = () => Guid.NewGuid().ToString();
		}

		public ExchangeNameConvention ExchangeNamingConvention { get; set; }
		public TopicNameConvention TopicNamingConvention { get; set; }
        public QueueNameConvention QueueNamingConvention { get; set; }
        public RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }

        public ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }
        public ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }
        public RpcExchangeNameConvention RpcExchangeNamingConvention { get; set; }
        public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

        public ConsumerTagConvention ConsumerTagConvention { get; set; }
	}
}