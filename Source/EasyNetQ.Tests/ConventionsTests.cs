// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EasyNetQ.Tests
{
	public class When_using_default_conventions
	{
		private Conventions conventions;
	    private ITypeNameSerializer typeNameSerializer;

		public When_using_default_conventions()
		{
            typeNameSerializer = new TypeNameSerializer();
			conventions = new Conventions(typeNameSerializer);
		}

		[Fact]
		public void The_default_exchange_naming_convention_should_use_the_TypeNameSerializers_Serialize_method()
		{
			var result = conventions.ExchangeNamingConvention(typeof (TestMessage));
            result.ShouldEqual(typeNameSerializer.Serialize(typeof(TestMessage)));
		}

		[Fact]
		public void The_default_topic_naming_convention_should_return_an_empty_string()
		{
			var result = conventions.TopicNamingConvention(typeof (TestMessage));
			result.ShouldEqual("");
		}

		[Fact]
		public void The_default_queue_naming_convention_should_use_the_TypeNameSerializers_Serialize_method_then_an_underscore_then_the_subscription_id()
		{
			const string subscriptionId = "test";
			var result = conventions.QueueNamingConvention(typeof (TestMessage), subscriptionId);
            result.ShouldEqual(typeNameSerializer.Serialize(typeof(TestMessage)) + "_" + subscriptionId);
		}

        [Fact]
        public void The_default_error_queue_name_should_be()
        {
            var result = conventions.ErrorQueueNamingConvention();
            result.ShouldEqual("EasyNetQ_Default_Error_Queue");
        }

        [Fact]
        public void The_default_error_exchange_name_should_be()
        {
            var info = new MessageReceivedInfo("consumer_tag", 0, false, "exchange", "routingKey", "queue");

            var result = conventions.ErrorExchangeNamingConvention(info);
            result.ShouldEqual("ErrorExchange_routingKey");
        }

        [Fact]
        public void The_default_rpc_request_exchange_name_should_be()
        {
            var result = conventions.RpcRequestExchangeNamingConvention(typeof (object));
            result.ShouldEqual("easy_net_q_rpc");
        }

        [Fact]
        public void The_default_rpc_reply_exchange_name_should_be()
        {
            var result = conventions.RpcResponseExchangeNamingConvention(typeof(object));
            result.ShouldEqual("easy_net_q_rpc");
        }

        [Fact]
        public void The_default_rpc_routingkey_naming_convention_should_use_the_TypeNameSerializers_Serialize_method()
        {
            var result = conventions.RpcRoutingKeyNamingConvention(typeof(TestMessage));
            result.ShouldEqual(typeNameSerializer.Serialize(typeof(TestMessage)));
        }        
	}

    public class When_using_QueueAttribute
    {
        private Conventions conventions;
        private ITypeNameSerializer typeNameSerializer;

        public When_using_QueueAttribute()
        {
            typeNameSerializer = new TypeNameSerializer();
            conventions = new Conventions(typeNameSerializer);
        }

        [Theory]
        [InlineData(typeof(AnnotatedTestMessage))]
        [InlineData(typeof(IAnnotatedTestMessage))]
        public void The_queue_naming_convention_should_use_attribute_queueName_then_an_underscore_then_the_subscription_id(Type messageType)
        {
            const string subscriptionId = "test";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.ShouldEqual("MyQueue" + "_" + subscriptionId);
        }

        [Theory]
        [InlineData(typeof(AnnotatedTestMessage))]
        [InlineData(typeof(IAnnotatedTestMessage))]
        public void And_subscription_id_is_empty_the_queue_naming_convention_should_use_attribute_queueName(Type messageType)
        {
            const string subscriptionId = "";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.ShouldEqual("MyQueue");
        }


        [Theory]
        [InlineData(typeof(EmptyQueueNameAnnotatedTestMessage))]
        [InlineData(typeof(IEmptyQueueNameAnnotatedTestMessage))]
        public void And_queueName_is_empty_should_use_the_TypeNameSerializers_Serialize_method_then_an_underscore_then_the_subscription_id(Type messageType)
        {
            const string subscriptionId = "test";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.ShouldEqual(typeNameSerializer.Serialize(messageType) + "_" + subscriptionId);
        }

        [Theory]
        [InlineData(typeof(AnnotatedTestMessage))]
        [InlineData(typeof(IAnnotatedTestMessage))]
        public void The_exchange_name_convention_should_use_attribute_exchangeName(Type messageType)
        {
            var result = conventions.ExchangeNamingConvention(messageType);
            result.ShouldEqual("MyExchange");
        }

        [Theory]
        [InlineData(typeof(QueueNameOnlyAnnotatedTestMessage))]
        [InlineData(typeof(IQueueNameOnlyAnnotatedTestMessage))]
        public void And_exchangeName_not_specified_the_exchange_name_convention_should_use_the_TypeNameSerializers_Serialize_method(Type messageType)
        {
            var result = conventions.ExchangeNamingConvention(messageType);
            result.ShouldEqual(typeNameSerializer.Serialize(messageType));
        }
    }

	public class When_publishing_a_message : IDisposable
	{
        private MockBuilder mockBuilder;
	    private ITypeNameSerializer typeNameSerializer;

		public When_publishing_a_message()
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

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
		public void Should_use_exchange_name_from_conventions_to_create_the_exchange()
		{
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("CustomExchangeNamingConvention"), 
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(new Dictionary<string, object>())));
		}

		[Fact]
		public void Should_use_exchange_name_from_conventions_as_the_exchange_to_publish_to()
		{
            mockBuilder.Channels[0].Received().BasicPublish(
                    Arg.Is("CustomExchangeNamingConvention"), 
                    Arg.Any<string>(), 
                    Arg.Is(false),
                    Arg.Any<IBasicProperties>(),
                    Arg.Any<byte[]>());
		}

		[Fact]
		public void Should_use_topic_name_from_conventions_as_the_topic_to_publish_to()
		{
            mockBuilder.Channels[0].Received().BasicPublish(
                    Arg.Any<string>(),
                    Arg.Is("CustomTopicNamingConvention"),
                    Arg.Is(false),
                    Arg.Any<IBasicProperties>(),
                    Arg.Any<byte[]>());
        }
	}

    public class When_registering_response_handler : IDisposable
    {
        private MockBuilder mockBuilder;

        public When_registering_response_handler()
        {
            var customConventions = new Conventions(new TypeNameSerializer())
            {
                RpcRequestExchangeNamingConvention = messageType => "CustomRpcExchangeName",
                RpcRoutingKeyNamingConvention = messageType => "CustomRpcRoutingKeyName"
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(_ => customConventions));

            mockBuilder.Bus.Respond<TestMessage, TestMessage>(t => new TestMessage());
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_correctly_bind_using_new_conventions()
        {
            mockBuilder.Channels[0].Received().QueueBind(
                    Arg.Is("CustomRpcRoutingKeyName"),
                    Arg.Is("CustomRpcExchangeName"),
                    Arg.Is("CustomRpcRoutingKeyName"),
                    Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(new Dictionary<string, object>())));
        }

        [Fact]
        public void Should_declare_correct_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("CustomRpcExchangeName"),
                Arg.Is("direct"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(new Dictionary<string, object>())));
        }

    }


    public class When_using_default_consumer_error_strategy
    {
        private DefaultConsumerErrorStrategy errorStrategy;
        private MockBuilder mockBuilder;
        private AckStrategy errorAckStrategy;
        private AckStrategy cancelAckStrategy;

        public When_using_default_consumer_error_strategy()
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
                customConventions,
                new TypeNameSerializer(),
                new DefaultErrorMessageSerializer());

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
                Substitute.For<IBasicConsumer>()
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

        [Fact]
        public void Should_use_exchange_name_from_custom_names_provider()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare("CustomErrorExchangePrefixName.originalRoutingKey", "direct", true);
        }

        [Fact]
        public void Should_use_queue_name_from_custom_names_provider()
        {
            mockBuilder.Channels[0].Received().QueueDeclare("CustomEasyNetQErrorQueueName", true, false, false, null);
        }

        [Fact]
        public void Should_Ack_failed_message()
        {
            Assert.Same(AckStrategies.Ack, errorAckStrategy);
        }
        
        [Fact]
        public void Should_Ack_canceled_message()
        {
            Assert.Same(AckStrategies.Ack, cancelAckStrategy);
        }
    }
}

// ReSharper restore InconsistentNaming