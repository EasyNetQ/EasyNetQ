// ReSharper disable InconsistentNaming

using System;
using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_9_1;

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

        [Test]
        public void The_default_error_queue_name_should_be()
        {
            var result = conventions.ErrorQueueNamingConvention();
            result.ShouldEqual("EasyNetQ_Default_Error_Queue");
        }

        [Test]
        public void The_default_error_exchange_name_should_be()
        {
            var result = conventions.ErrorExchangeNamingConvention("routingKey");
            result.ShouldEqual("ErrorExchange_routingKey");
        }

        [Test]
        public void The_default_rpc_exchange_name_should_be()
        {
            var result = conventions.RpcExchangeNamingConvention();
            result.ShouldEqual("easy_net_q_rpc");
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
		    var advancedBus = new RabbitAdvancedBus(
                new ConnectionConfiguration(), 
                new MockConnectionFactory(new MockConnection(model)),
                TypeNameSerializer.Serialize,
                new JsonSerializer(),
                new MockConsumerFactory(),
                new MockLogger(),
                CorrelationIdGenerator.GetCorrelationId,
                conventions
		        );

			bus = new RabbitBus(
				x => TypeNameSerializer.Serialize(x.GetType()),
				new MockLogger(),
				conventions,
                advancedBus
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

    [TestFixture]
    public class When_registering_respons_handler
    {
        private RabbitBus bus;
        private string createdExchangeName;

        [SetUp]
        public void SetUp()
        {
            var mockModel = new MockModel
            {
                ExchangeDeclareAction = (exchangeName, type, durable, autoDelete, arguments) => createdExchangeName = exchangeName,
                BasicConsumeAction = (queue, noAck, consumerTag, consumer) => { return string.Empty; }
            };

            var customConventions = new Conventions
            {
                RpcExchangeNamingConvention = () => "CustomRpcExchangeName"
            };

            CreateBus(customConventions, mockModel);
            bus.Respond<TestMessage, TestMessage>(t => { return new TestMessage(); });
        }

        private void CreateBus(IConventions conventions, IModel model)
        {
            var advancedBus = new RabbitAdvancedBus(
                new ConnectionConfiguration(),
                new MockConnectionFactory(new MockConnection(model)),
                TypeNameSerializer.Serialize,
                new JsonSerializer(),
                new MockConsumerFactory(),
                new MockLogger(),
                CorrelationIdGenerator.GetCorrelationId,
                new Conventions()
                );

            bus = new RabbitBus(
                x => TypeNameSerializer.Serialize(x.GetType()),
                new MockLogger(),
                conventions,
                advancedBus
                );
        }

        [Test]
        public void Should_use_exchange_name_from_custom_names_provider()
        {
            createdExchangeName.ShouldEqual("CustomRpcExchangeName");
        }
    }


    [TestFixture]
    public class When_using_default_consumer_error_strategy
    {
        private DefaultConsumerErrorStrategy errorStrategy;
        private string createdExchangeName;
        private string createdQueueName;

        [SetUp]
        public void SetUp()
        {
            var mockModel = new MockModel
                                {
                                    ExchangeDeclareAction = (exchangeName, type, durable, autoDelete, arguments) => createdExchangeName = exchangeName,
                                    QueueDeclareAction = (queue, durable, exclusive, autoDelete, arguments) =>
                                                             {
                                                                 createdQueueName = queue;
                                                                 return new QueueDeclareOk(queue, 0, 0);
                                                             }
                                };

            var customConventions = new Conventions
                                        {
                                            ErrorQueueNamingConvention = () => "CustomEasyNetQErrorQueueName",
                                            ErrorExchangeNamingConvention = (originalRoutingKey) => "CustomErrorExchangePrefixName." + originalRoutingKey
                                        };

            errorStrategy = new DefaultConsumerErrorStrategy(new MockConnectionFactory(new MockConnection(mockModel)), new JsonSerializer(), new MockLogger(), customConventions);

            const string originalMessage = "";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var deliverArgs = new BasicDeliverEventArgs
            {
                RoutingKey = "originalRoutingKey",
                Exchange = "orginalExchange",
                Body = originalMessageBody,
                BasicProperties = new BasicProperties
                {
                    CorrelationId = string.Empty,
                    AppId = string.Empty
                }
            };

            try
            {
                errorStrategy.HandleConsumerError(deliverArgs, new Exception());
            }
            catch (Exception exc)
            {
                // swallow
            }
        }

        [Test]
        public void Should_use_exchange_name_from_custom_names_provider()
        {
            createdExchangeName.ShouldEqual("CustomErrorExchangePrefixName.originalRoutingKey");
        }

        [Test]
        public void Should_use_queue_name_from_custom_names_provider()
        {
            createdQueueName.ShouldEqual("CustomEasyNetQErrorQueueName");
        }
    }
}

// ReSharper restore InconsistentNaming