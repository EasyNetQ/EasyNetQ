// ReSharper disable InconsistentNaming

using NUnit.Framework;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
	[TestFixture]
	public class When_using_default_conventions
	{
		private Conventions conventions;

		[SetUp]
		public void SetUp()
		{
			conventions = new Conventions();
		}

		[Test]
		public void The_default_exchange_naming_convention_should_use_the_TypeNameSerializers_Serialize_method()
		{
			var result = conventions.ExchangeNamingConvention(typeof (TestMessage));
			result.ShouldEqual(TypeNameSerializer.Serialize(typeof (TestMessage)));
		}

		[Test]
		public void The_default_topic_naming_convention_should_return_an_empty_string()
		{
			var result = conventions.TopicNamingConvention(typeof (TestMessage));
			result.ShouldEqual("");
		}

		[Test]
		public void The_default_queue_naming_convention_should_use_the_TypeNameSerializers_Serialize_method_then_an_underscore_then_the_subscription_id()
		{
			const string subscriptionId = "test";
			var result = conventions.QueueNamingConvention(typeof (TestMessage), subscriptionId);
			result.ShouldEqual(TypeNameSerializer.Serialize(typeof (TestMessage)) + "_" + subscriptionId);
		}
	}

	[TestFixture]
	public class When_publishing_a_message
	{
		private RabbitBus bus;
	    private string createdExchangeName;
		private string publishedToExchangeName;
		private string publishedToTopic;

		[SetUp]
		public void SetUp()
		{
			var mockModel = new MockModel
			            	{
			            		ExchangeDeclareAction = (exchangeName, type, durable, autoDelete, arguments) => createdExchangeName = exchangeName,
								BasicPublishAction = (exchangeName, topic, properties, messageBody) =>
								                     	{
								                     		publishedToExchangeName = exchangeName;
								                     		publishedToTopic = topic;
								                     	}
			            	};


			var customConventions = new Conventions
			                  	{
			                  		ExchangeNamingConvention = x => "CustomExchangeNamingConvention",
			                  		QueueNamingConvention = (x, y) => "CustomQueueNamingConvention",
			                  		TopicNamingConvention = x => "CustomTopicNamingConvention"
			                  	};
			CreateBus(customConventions, mockModel);
		    using (var publishChannel = bus.OpenPublishChannel())
		    {
                publishChannel.Publish(new TestMessage());
		    }
		}

		private void CreateBus(Conventions conventions, IModel model)
		{
			bus = new RabbitBus(
				x => TypeNameSerializer.Serialize(x.GetType()),
				new JsonSerializer(),
				new MockConsumerFactory(),
				new MockConnectionFactory(new MockConnection(model)),
				new MockLogger(),
				CorrelationIdGenerator.GetCorrelationId,
				conventions
				);
		}

		[Test]
		public void Should_use_exchange_name_from_conventions_to_create_the_exchange()
		{
			createdExchangeName.ShouldEqual("CustomExchangeNamingConvention");
		}

		[Test]
		public void Should_use_exchange_name_from_conventions_as_the_exchange_to_publish_to()
		{
			publishedToExchangeName.ShouldEqual("CustomExchangeNamingConvention");
		}

		[Test]
		public void Should_use_topic_name_from_conventions_as_the_topic_to_publish_to()
		{
			publishedToTopic.ShouldEqual("CustomTopicNamingConvention");
		}
	}
}

// ReSharper restore InconsistentNaming