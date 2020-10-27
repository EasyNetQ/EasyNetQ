// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.Producer;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests
{
    public class When_using_default_conventions
    {
        public When_using_default_conventions()
        {
            typeNameSerializer = new DefaultTypeNameSerializer();
            conventions = new Conventions(typeNameSerializer);
        }

        private Conventions conventions;
        private ITypeNameSerializer typeNameSerializer;

        [Fact]
        public void The_default_error_exchange_name_should_be()
        {
            var info = new MessageReceivedInfo("consumer_tag", 0, false, "exchange", "routingKey", "queue");

            var result = conventions.ErrorExchangeNamingConvention(info);
            result.Should().Be("ErrorExchange_routingKey");
        }

        [Fact]
        public void The_default_error_queue_name_should_be()
        {
            var result = conventions.ErrorQueueNamingConvention(null);
            result.Should().Be("EasyNetQ_Default_Error_Queue");
        }

        [Fact]
        public void The_default_exchange_naming_convention_should_use_the_TypeNameSerializers_Serialize_method()
        {
            var result = conventions.ExchangeNamingConvention(typeof(TestMessage));
            result.Should().Be(typeNameSerializer.Serialize(typeof(TestMessage)));
        }

        [Fact]
        public void The_default_queue_naming_convention_should_use_the_TypeNameSerializers_Serialize_method_then_an_underscore_then_the_subscription_id()
        {
            const string subscriptionId = "test";
            var result = conventions.QueueNamingConvention(typeof(TestMessage), subscriptionId);
            result.Should().Be(typeNameSerializer.Serialize(typeof(TestMessage)) + "_" + subscriptionId);
        }

        [Fact]
        public void The_default_rpc_reply_exchange_name_should_be()
        {
            var result = conventions.RpcResponseExchangeNamingConvention(typeof(object));
            result.Should().Be("easy_net_q_rpc");
        }

        [Fact]
        public void The_default_rpc_request_exchange_name_should_be()
        {
            var result = conventions.RpcRequestExchangeNamingConvention(typeof(object));
            result.Should().Be("easy_net_q_rpc");
        }

        [Fact]
        public void The_default_rpc_routingkey_naming_convention_should_use_the_TypeNameSerializers_Serialize_method()
        {
            var result = conventions.RpcRoutingKeyNamingConvention(typeof(TestMessage));
            result.Should().Be(typeNameSerializer.Serialize(typeof(TestMessage)));
        }

        [Fact]
        public void The_default_topic_naming_convention_should_return_an_empty_string()
        {
            var result = conventions.TopicNamingConvention(typeof(TestMessage));
            result.Should().Be("");
        }
    }

    public class When_using_QueueAttribute
    {
        private Conventions conventions;
        private ITypeNameSerializer typeNameSerializer;

        public When_using_QueueAttribute()
        {
            typeNameSerializer = new DefaultTypeNameSerializer();
            conventions = new Conventions(typeNameSerializer);
        }

        [Theory]
        [InlineData(typeof(AnnotatedTestMessage))]
        [InlineData(typeof(IAnnotatedTestMessage))]
        public void The_queue_naming_convention_should_use_attribute_queueName_then_an_underscore_then_the_subscription_id(Type messageType)
        {
            const string subscriptionId = "test";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.Should().Be("MyQueue" + "_" + subscriptionId);
        }

        [Theory]
        [InlineData(typeof(AnnotatedTestMessage))]
        [InlineData(typeof(IAnnotatedTestMessage))]
        public void And_subscription_id_is_empty_the_queue_naming_convention_should_use_attribute_queueName(Type messageType)
        {
            const string subscriptionId = "";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.Should().Be("MyQueue");
        }

        [Theory]
        [InlineData(typeof(EmptyQueueNameAnnotatedTestMessage))]
        [InlineData(typeof(IEmptyQueueNameAnnotatedTestMessage))]
        public void And_queueName_is_empty_should_use_the_TypeNameSerializers_Serialize_method_then_an_underscore_then_the_subscription_id(Type messageType)
        {
            const string subscriptionId = "test";
            var result = conventions.QueueNamingConvention(messageType, subscriptionId);
            result.Should().Be(typeNameSerializer.Serialize(messageType) + "_" + subscriptionId);
        }

        [Theory]
        [InlineData(typeof(AnnotatedTestMessage))]
        [InlineData(typeof(IAnnotatedTestMessage))]
        public void The_exchange_name_convention_should_use_attribute_exchangeName(Type messageType)
        {
            var result = conventions.ExchangeNamingConvention(messageType);
            result.Should().Be("MyExchange");
        }

        [Theory]
        [InlineData(typeof(QueueNameOnlyAnnotatedTestMessage))]
        [InlineData(typeof(IQueueNameOnlyAnnotatedTestMessage))]
        public void And_exchangeName_not_specified_the_exchange_name_convention_should_use_the_TypeNameSerializers_Serialize_method(Type messageType)
        {
            var result = conventions.ExchangeNamingConvention(messageType);
            result.Should().Be(typeNameSerializer.Serialize(messageType));
        }
    }

    public class When_publishing_a_message : IDisposable
    {
        public When_publishing_a_message()
        {
            typeNameSerializer = new DefaultTypeNameSerializer();
            var customConventions = new Conventions(typeNameSerializer)
            {
                ExchangeNamingConvention = x => "CustomExchangeNamingConvention",
                QueueNamingConvention = (x, y) => "CustomQueueNamingConvention",
                TopicNamingConvention = x => "CustomTopicNamingConvention"
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(customConventions));
            mockBuilder.PubSub.Publish(new TestMessage());
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private readonly MockBuilder mockBuilder;
        private readonly ITypeNameSerializer typeNameSerializer;

        [Fact]
        public void Should_use_exchange_name_from_conventions_as_the_exchange_to_publish_to()
        {
            mockBuilder.Channels[1].Received().BasicPublish(
                Arg.Is("CustomExchangeNamingConvention"),
                Arg.Any<string>(),
                Arg.Is(false),
                Arg.Any<IBasicProperties>(),
                Arg.Any<ReadOnlyMemory<byte>>()
            );
        }

        [Fact]
        public void Should_use_exchange_name_from_conventions_to_create_the_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("CustomExchangeNamingConvention"),
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is((IDictionary<string, object>)null)
            );
        }

        [Fact]
        public void Should_use_topic_name_from_conventions_as_the_topic_to_publish_to()
        {
            mockBuilder.Channels[1].Received().BasicPublish(
                Arg.Any<string>(),
                Arg.Is("CustomTopicNamingConvention"),
                Arg.Is(false),
                Arg.Any<IBasicProperties>(),
                Arg.Any<ReadOnlyMemory<byte>>()
            );
        }
    }

    public class When_registering_response_handler : IDisposable
    {
        public When_registering_response_handler()
        {
            var customConventions = new Conventions(new DefaultTypeNameSerializer())
            {
                RpcRequestExchangeNamingConvention = messageType => "CustomRpcExchangeName",
                RpcRoutingKeyNamingConvention = messageType => "CustomRpcRoutingKeyName"
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(customConventions));

            mockBuilder.Rpc.Respond<TestMessage, TestMessage>(t => new TestMessage());
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;

        [Fact]
        public void Should_correctly_bind_using_new_conventions()
        {
            mockBuilder.Channels[0].Received().QueueBind(
                Arg.Is("CustomRpcRoutingKeyName"),
                Arg.Is("CustomRpcExchangeName"),
                Arg.Is("CustomRpcRoutingKeyName"),
                Arg.Is((IDictionary<string, object>)null)
            );
        }

        [Fact]
        public void Should_declare_correct_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("CustomRpcExchangeName"),
                Arg.Is("direct"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is((IDictionary<string, object>)null));
        }
    }
}

// ReSharper restore InconsistentNaming
