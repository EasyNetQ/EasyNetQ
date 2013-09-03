// ReSharper disable InconsistentNaming

using System;
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_9_1;
using Rhino.Mocks;

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

        [Test]
        public void The_default_rpc_routingkey_naming_convention_should_use_the_TypeNameSerializers_Serialize_method()
        {
            var result = conventions.RpcRoutingKeyNamingConvention(typeof(TestMessage));
            result.ShouldEqual(TypeNameSerializer.Serialize(typeof(TestMessage)));
        }
	}

	[TestFixture]
	public class When_publishing_a_message
	{
        private MockBuilder mockBuilder;

		[SetUp]
		public void SetUp()
		{
            var customConventions = new Conventions
            {
                ExchangeNamingConvention = x => "CustomExchangeNamingConvention",
                QueueNamingConvention = (x, y) => "CustomQueueNamingConvention",
                TopicNamingConvention = x => "CustomTopicNamingConvention"
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(_ => customConventions));

		    using (var publishChannel = mockBuilder.Bus.OpenPublishChannel())
		    {
                publishChannel.Publish(new TestMessage());
		    }
		}

		[Test]
		public void Should_use_exchange_name_from_conventions_to_create_the_exchange()
		{
            mockBuilder.Channels[1].AssertWasCalled(x => 
                x.ExchangeDeclare("CustomExchangeNamingConvention", "topic", true, false, null));
		}

		[Test]
		public void Should_use_exchange_name_from_conventions_as_the_exchange_to_publish_to()
		{
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.BasicPublish(
                    Arg<string>.Is.Equal("CustomExchangeNamingConvention"), 
                    Arg<string>.Is.Anything, 
                    Arg<IBasicProperties>.Is.Anything,
                    Arg<byte[]>.Is.Anything));
		}

		[Test]
		public void Should_use_topic_name_from_conventions_as_the_topic_to_publish_to()
		{
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.BasicPublish(
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Equal("CustomTopicNamingConvention"),
                    Arg<IBasicProperties>.Is.Anything,
                    Arg<byte[]>.Is.Anything));
        }
	}

    [TestFixture]
    public class When_registering_response_handler
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            var customConventions = new Conventions
            {
                RpcExchangeNamingConvention = () => "CustomRpcExchangeName",
                RpcRoutingKeyNamingConvention = messageType => "CustomRpcRoutingKeyName"
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(_ => customConventions));

            mockBuilder.Bus.Respond<TestMessage, TestMessage>(t => new TestMessage());
        }

        [Test]
        public void Should_correctly_bind_using_new_conventions()
        {
            mockBuilder.Channels[2].AssertWasCalled(x => 
                x.QueueBind(
                    "CustomRpcRoutingKeyName",
                    "CustomRpcExchangeName",
                    "CustomRpcRoutingKeyName"));
        }

        [Test]
        public void Should_declare_correct_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.ExchangeDeclare("CustomRpcExchangeName", "direct", true, false, null));
        }

    }


    [TestFixture]
    public class When_using_default_consumer_error_strategy
    {
        private DefaultConsumerErrorStrategy errorStrategy;
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            var customConventions = new Conventions
            {
                ErrorQueueNamingConvention = () => "CustomEasyNetQErrorQueueName",
                ErrorExchangeNamingConvention = originalRoutingKey => "CustomErrorExchangePrefixName." + originalRoutingKey
            };

            mockBuilder = new MockBuilder();

            errorStrategy = new DefaultConsumerErrorStrategy(
                mockBuilder.ConnectionFactory, 
                new JsonSerializer(), 
                MockRepository.GenerateStub<IEasyNetQLogger>(), 
                customConventions);

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
            catch (Exception)
            {
                // swallow
            }
        }

        [Test]
        public void Should_use_exchange_name_from_custom_names_provider()
        {
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.ExchangeDeclare("CustomErrorExchangePrefixName.originalRoutingKey", "direct", true));
        }

        [Test]
        public void Should_use_queue_name_from_custom_names_provider()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.QueueDeclare("CustomEasyNetQErrorQueueName", true, false, false, null));
        }
    }
}

// ReSharper restore InconsistentNaming