// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
	[TestFixture]
	public class When_using_default_conventions
	{
		private Conventions conventions;
	    private ITypeNameSerializer typeNameSerializer;

		[SetUp]
		public void SetUp()
		{
            typeNameSerializer = new TypeNameSerializer();
			conventions = new Conventions(typeNameSerializer);
		}

		[Test]
		public void The_default_exchange_naming_convention_should_use_the_TypeNameSerializers_Serialize_method()
		{
			var result = conventions.ExchangeNamingConvention(typeof (TestMessage));
            result.ShouldEqual(typeNameSerializer.Serialize(typeof(TestMessage)));
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
            result.ShouldEqual(typeNameSerializer.Serialize(typeof(TestMessage)) + "_" + subscriptionId);
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
            var info = new MessageReceivedInfo("consumer_tag", 0, false, "exchange", "routingKey", "queue");

            var result = conventions.ErrorExchangeNamingConvention(info);
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
            result.ShouldEqual(typeNameSerializer.Serialize(typeof(TestMessage)));
        }
	}

    [TestFixture]
    public class When_using_QueueAttribute
    {
        private Conventions conventions;
        private ITypeNameSerializer typeNameSerializer;

        [SetUp]
        public void SetUp()
        {
            typeNameSerializer = new TypeNameSerializer();
            conventions = new Conventions(typeNameSerializer);
        }

        [Test]
        [TestCase(typeof(AnnotatedTestMessage))]
        [TestCase(typeof(IAnnotatedTestMessage))]
        public void The_queue_naming_convention_should_use_attribute_queueName_then_an_underscore_then_the_subscription_id(Type messageType)
        {
            const string subscriptionId = "test";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.ShouldEqual("MyQueue" + "_" + subscriptionId);
        }

        [Test]
        [TestCase(typeof(AnnotatedTestMessage))]
        [TestCase(typeof(IAnnotatedTestMessage))]
        public void And_subscription_id_is_empty_the_queue_naming_convention_should_use_attribute_queueName(Type messageType)
        {
            const string subscriptionId = "";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.ShouldEqual("MyQueue");
        }


        [Test]
        [TestCase(typeof(EmptyQueueNameAnnotatedTestMessage))]
        [TestCase(typeof(IEmptyQueueNameAnnotatedTestMessage))]
        public void And_queueName_is_empty_should_use_the_TypeNameSerializers_Serialize_method_then_an_underscore_then_the_subscription_id(Type messageType)
        {
            const string subscriptionId = "test";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.ShouldEqual(typeNameSerializer.Serialize(messageType) + "_" + subscriptionId);
        }

        [Test]
        [TestCase(typeof(AnnotatedTestMessage))]
        [TestCase(typeof(IAnnotatedTestMessage))]
        public void The_exchange_name_convention_should_use_attribute_exchangeName(Type messageType)
        {
            var result = conventions.ExchangeNamingConvention(messageType);
            result.ShouldEqual("MyExchange");
        }

        [Test]
        [TestCase(typeof(QueueNameOnlyAnnotatedTestMessage))]
        [TestCase(typeof(IQueueNameOnlyAnnotatedTestMessage))]
        public void And_exchangeName_not_specified_the_exchange_name_convention_should_use_the_TypeNameSerializers_Serialize_method(Type messageType)
        {
            var result = conventions.ExchangeNamingConvention(messageType);
            result.ShouldEqual(typeNameSerializer.Serialize(messageType));
        }
    }

	[TestFixture]
	public class When_publishing_a_message
	{
        private MockBuilder mockBuilder;
	    private ITypeNameSerializer typeNameSerializer;

		[SetUp]
		public void SetUp()
		{
            typeNameSerializer = new TypeNameSerializer();
            var customConventions = new Conventions(typeNameSerializer)
            {
                ExchangeNamingConvention = x => "CustomExchangeNamingConvention",
                QueueNamingConvention = (x, y) => "CustomQueueNamingConvention",
                TopicNamingConvention = x => "CustomTopicNamingConvention"
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(_ => customConventions));
            mockBuilder.Bus.Publish(new TestMessage());
		}

		[Test]
		public void Should_use_exchange_name_from_conventions_to_create_the_exchange()
		{
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.ExchangeDeclare("CustomExchangeNamingConvention", "topic", true, false, new Dictionary<string, object>()));
		}

		[Test]
		public void Should_use_exchange_name_from_conventions_as_the_exchange_to_publish_to()
		{
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.BasicPublish(
                    Arg<string>.Is.Equal("CustomExchangeNamingConvention"), 
                    Arg<string>.Is.Anything, 
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(false),
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
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(false),
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
            var customConventions = new Conventions(new TypeNameSerializer())
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
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.QueueBind(
                    "CustomRpcRoutingKeyName",
                    "CustomRpcExchangeName",
                    "CustomRpcRoutingKeyName"));
        }

        [Test]
        public void Should_declare_correct_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.ExchangeDeclare("CustomRpcExchangeName", "direct", true, false, new Dictionary<string, object>()));
        }

    }


    [TestFixture]
    public class When_using_default_consumer_error_strategy
    {
        private DefaultConsumerErrorStrategy errorStrategy;
        private MockBuilder mockBuilder;
        private AckStrategy errorAckStrategy;
        private AckStrategy cancelAckStrategy;

        [SetUp]
        public void SetUp()
        {
            var customConventions = new Conventions(new TypeNameSerializer())
            {
                ErrorQueueNamingConvention = () => "CustomEasyNetQErrorQueueName",
                ErrorExchangeNamingConvention = info => "CustomErrorExchangePrefixName." + info.RoutingKey
            };

            mockBuilder = new MockBuilder();

            errorStrategy = new DefaultConsumerErrorStrategy(
                mockBuilder.ConnectionFactory, 
                new JsonSerializer(new TypeNameSerializer()), 
                MockRepository.GenerateStub<IEasyNetQLogger>(), 
                customConventions,
                new TypeNameSerializer());

            const string originalMessage = "";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var context = new ConsumerExecutionContext(
                (bytes, properties, arg3) => null,
                new MessageReceivedInfo("consumerTag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
                new MessageProperties
                    {
                        CorrelationId = string.Empty,
                        AppId = string.Empty
                    },
                originalMessageBody,
                MockRepository.GenerateStub<IBasicConsumer>()
                );

            try
            {
                errorAckStrategy = errorStrategy.HandleConsumerError(context, new Exception());
                cancelAckStrategy = errorStrategy.HandleConsumerCancelled(context);
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

        [Test]
        public void Should_Ack_failed_message()
        {
            Assert.AreSame(AckStrategies.Ack, errorAckStrategy);
        }
        
        [Test]
        public void Should_Ack_canceled_message()
        {
            Assert.AreSame(AckStrategies.Ack, cancelAckStrategy);
        }
    }
}

// ReSharper restore InconsistentNaming