using System;


namespace EasyNetQ
{
	public delegate string ExchangeNameConvention(Type messageType);
	public delegate string TopicNameConvention(Type messageType);
	public delegate string QueueNameConvention(Type messageType, string subscriberId);

	public interface IConventions
	{

		ExchangeNameConvention ExchangeNamingConvention { get; set; }
		TopicNameConvention TopicNamingConvention { get; set; }
		QueueNameConvention QueueNamingConvention { get; set; }
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
		}


		public ExchangeNameConvention ExchangeNamingConvention { get; set; }
		public TopicNameConvention TopicNamingConvention { get; set; }
		public QueueNameConvention QueueNamingConvention { get; set; }
	}
}