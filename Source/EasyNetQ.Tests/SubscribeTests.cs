// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using Xunit;

namespace EasyNetQ.Tests
{
    public class When_subscribe_is_called : IDisposable
    {
        private MockBuilder mockBuilder;

        private const string typeName = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string queueName = typeName + "_" + subscriptionId;
        private const string consumerTag = "the_consumer_tag";

        private ISubscriptionResult subscriptionResult;

        public When_subscribe_is_called()
        {
            var conventions = new Conventions(new DefaultTypeNameSerializer())
                {
                    ConsumerTagConvention = () => consumerTag
                };

            mockBuilder = new MockBuilder(x => x
                .Register<IConventions>(conventions)
                );

            subscriptionResult = mockBuilder.PubSub.Subscribe<MyMessage>(subscriptionId, message => { });
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_create_a_new_channel_for_the_consumer()
        {
            // A channel is created for running client originated commands,
            // a second channel is created for the consumer.
            mockBuilder.Channels.Count.Should().Be(2);
        }

        [Fact]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                    Arg.Is(queueName),
                    Arg.Is(true),  // durable
                    Arg.Is(false), // exclusive
                    Arg.Is(false), // autoDelete
                    Arg.Any<IDictionary<string, object>>());
        }

        [Fact]
        public void Should_declare_the_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is(typeName),
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is((IDictionary<string, object>)null));
        }

        [Fact]
        public void Should_bind_the_queue_and_exchange()
        {
            mockBuilder.Channels[0].Received().QueueBind(
                Arg.Is(queueName),
                Arg.Is(typeName),
                Arg.Is("#"),
                Arg.Is((IDictionary<string, object>)null));
        }

        [Fact]
        public void Should_set_configured_prefetch_count()
        {
            var connectionConfiguration = new ConnectionConfiguration();
            mockBuilder.Channels[1].Received().BasicQos(0, connectionConfiguration.PrefetchCount, false);
        }

        [Fact]
        public void Should_start_consuming()
        {
            mockBuilder.Channels[1].Received().BasicConsume(
                    Arg.Is(queueName),
                    Arg.Is(false),
                    Arg.Any<string>(),
                    Arg.Is(true),
                    Arg.Is(false),
                    Arg.Any<IDictionary<string, object>>(),
                    Arg.Any<IBasicConsumer>()
            );
        }

        [Fact]
        public void Should_register_consumer()
        {
            mockBuilder.Consumers.Count.Should().Be(1);
        }

        [Fact]
        public void Should_return_non_null_and_with_expected_values_result()
        {
            Assert.NotNull(subscriptionResult);
            Assert.NotNull(subscriptionResult.Exchange);
            Assert.NotNull(subscriptionResult.Queue);
            Assert.NotNull(subscriptionResult.ConsumerCancellation);
            Assert.True(subscriptionResult.Exchange.Name == typeName);
            Assert.True(subscriptionResult.Queue.Name == queueName);
        }
    }

    public class When_subscribe_with_configuration_is_called
    {
        [InlineData("ttt", true, 99, 999, 10, true, (byte)11, false, "qqq", 1001, 10001)]
        [InlineData(null, false, 0, 0, null, false, null, true, "qqq", null, null)]
        [Theory]
        public void Queue_should_be_declared_with_correct_options(
            string topic,
            bool autoDelete,
            int priority,
            ushort prefetchCount,
            int? expires,
            bool isExclusive,
            byte? maxPriority,
            bool durable,
            string queueName,
            int? maxLength,
            int? maxLengthBytes)
        {
            var mockBuilder = new MockBuilder();
            using (mockBuilder.Bus)
            {
                // Configure subscription
                mockBuilder.PubSub.Subscribe<MyMessage>(
                    "x",
                    m => { },
                    c =>
                    {
                        c.WithAutoDelete(autoDelete)
                            .WithPriority(priority)
                            .WithPrefetchCount(prefetchCount)
                            .AsExclusive(isExclusive)
                            .WithDurable(durable)
                            .WithQueueName(queueName);

                        if (topic != null)
                        {
                            c.WithTopic(topic);
                        }
                        if (maxPriority.HasValue)
                        {
                            c.WithMaxPriority(maxPriority.Value);
                        }
                        if (expires.HasValue)
                        {
                            c.WithExpires(expires.Value);
                        }
                        if (maxLength.HasValue)
                        {
                            c.WithMaxLength(maxLength.Value);
                        }
                        if (maxLengthBytes.HasValue)
                        {
                            c.WithMaxLengthBytes(maxLengthBytes.Value);
                        }
                    }
                );
            }

            // Assert that queue got declared correctly
            mockBuilder.Channels[0].Received().QueueDeclare(
                    Arg.Is(queueName ?? "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests_x"),
                    Arg.Is(durable),
                    Arg.Is(false), // IsExclusive is set on the Consume call
                    Arg.Is(autoDelete),
                    Arg.Is<IDictionary<string, object>>(
                        x =>
                        (!expires.HasValue || expires.Value == (int)x["x-expires"]) &&
                        (!maxPriority.HasValue || maxPriority.Value == (int)x["x-max-priority"]) &&
                        (!maxLength.HasValue || maxLength.Value == (int)x["x-max-length"]) &&
                        (!maxLengthBytes.HasValue || maxLengthBytes.Value == (int)x["x-max-length-bytes"]))
                    );

            // Assert that consumer was created correctly
            mockBuilder.Channels[1].Received().BasicConsume(
                    Arg.Is(queueName ?? "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests_x"),
                    Arg.Is(false),
                    Arg.Any<string>(),
                    Arg.Is(true),
                    Arg.Is(isExclusive),
                    Arg.Is<IDictionary<string, object>>(x => priority == (int)x["x-priority"]),
                    Arg.Any<IBasicConsumer>());

            // Assert that QoS got configured correctly
            mockBuilder.Channels[1].Received().BasicQos(0, prefetchCount, false);

            // Assert that binding got configured correctly
            mockBuilder.Channels[0].Received().QueueBind(
                Arg.Is(queueName),
                Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
                Arg.Is(topic ?? "#"),
                Arg.Is((IDictionary<string, object>)null));
        }
    }

    public class When_a_message_is_delivered : IDisposable
    {
        private MockBuilder mockBuilder;

        private const string typeName = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string correlationId = "the_correlation_id";
        private const string consumerTag = "the_consumer_tag";
        private const ulong deliveryTag = 123;

        private MyMessage originalMessage;
        private MyMessage deliveredMessage;

        public When_a_message_is_delivered()
        {
            var conventions = new Conventions(new DefaultTypeNameSerializer())
            {
                ConsumerTagConvention = () => consumerTag
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(conventions));

            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());

            mockBuilder.PubSub.Subscribe<MyMessage>(subscriptionId, message =>
            {
                deliveredMessage = message;
            });

            const string text = "Hello there, I am the text!";
            originalMessage = new MyMessage { Text = text };

            var body = new JsonSerializer().MessageToBytes(typeof(MyMessage), originalMessage);

            // deliver a message
            mockBuilder.Consumers[0].HandleBasicDeliver(
                consumerTag,
                deliveryTag,
                false, // redelivered
                typeName,
                "#",
                new BasicProperties
                {
                    Type = typeName,
                    CorrelationId = correlationId
                },
                body);

            // wait for the subscription thread to handle the message ...
            autoResetEvent.WaitOne(1000);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_build_bus_successfully()
        {
            // just want to run SetUp()
        }

        [Fact]
        public void Should_deliver_message()
        {
            deliveredMessage.Should().NotBeNull();
            deliveredMessage.Text.Should().Be(originalMessage.Text);
        }

        [Fact]
        public void Should_ack_the_message()
        {
            mockBuilder.Channels[1].Received().BasicAck(deliveryTag, false);
        }
    }

    public class When_the_handler_throws_an_exception : IDisposable
    {
        private MockBuilder mockBuilder;
        private IConsumerErrorStrategy consumerErrorStrategy;

        private const string typeName = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string correlationId = "the_correlation_id";
        private const string consumerTag = "the_consumer_tag";
        private const ulong deliveryTag = 123;

        private readonly MyMessage originalMessage;
        private readonly Exception originalException = new Exception("Some exception message");
        private ConsumerExecutionContext basicDeliverEventArgs;
        private Exception raisedException;

        public When_the_handler_throws_an_exception()
        {
            var conventions = new Conventions(new DefaultTypeNameSerializer())
            {
                ConsumerTagConvention = () => consumerTag
            };

            consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            consumerErrorStrategy.HandleConsumerError(default, null)
                .ReturnsForAnyArgs(i =>
                {
                    basicDeliverEventArgs = (ConsumerExecutionContext)i[0];
                    raisedException = (Exception)i[1];
                    return AckStrategies.Ack;
                });

            mockBuilder = new MockBuilder(x => x
                .Register<IConventions>(conventions)
                .Register(consumerErrorStrategy)
                );

            mockBuilder.PubSub.Subscribe<MyMessage>(subscriptionId, message => throw originalException);

            const string text = "Hello there, I am the text!";
            originalMessage = new MyMessage { Text = text };

            var body = new JsonSerializer().MessageToBytes(typeof(MyMessage), originalMessage);

            // deliver a message
            mockBuilder.Consumers[0].HandleBasicDeliver(
                consumerTag,
                deliveryTag,
                false, // redelivered
                typeName,
                "#",
                new BasicProperties
                {
                    Type = typeName,
                    CorrelationId = correlationId
                },
                body
            );

            // wait for the subscription thread to handle the message ...
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_ack()
        {
            mockBuilder.Channels[1].Received().BasicAck(deliveryTag, false);
        }

        [Fact]
        public void Should_invoke_the_consumer_error_strategy()
        {
            consumerErrorStrategy.Received().HandleConsumerError(Arg.Any<ConsumerExecutionContext>(), Arg.Any<Exception>());
        }

        [Fact]
        public void Should_pass_the_exception_to_consumerErrorStrategy()
        {
            raisedException.Should().BeSameAs(originalException);
        }

        [Fact]
        public void Should_pass_the_deliver_args_to_the_consumerErrorStrategy()
        {
            basicDeliverEventArgs.Should().NotBeNull();
            basicDeliverEventArgs.ReceivedInfo.ConsumerTag.Should().Be(consumerTag);
            basicDeliverEventArgs.ReceivedInfo.DeliveryTag.Should().Be(deliveryTag);
            basicDeliverEventArgs.ReceivedInfo.RoutingKey.Should().Be("#");
        }
    }

    public class When_a_subscription_is_cancelled_by_the_user : IDisposable
    {
        private MockBuilder mockBuilder;
        private const string subscriptionId = "the_subscription_id";
        private const string consumerTag = "the_consumer_tag";

        public When_a_subscription_is_cancelled_by_the_user()
        {
            var conventions = new Conventions(new DefaultTypeNameSerializer())
            {
                ConsumerTagConvention = () => consumerTag
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(conventions));
            var subscriptionResult = mockBuilder.PubSub.Subscribe<MyMessage>(subscriptionId, message => { });
            var are = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<ConsumerModelDisposedEvent>(x => are.Set());
            subscriptionResult.Dispose();
            are.WaitOne(500);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_dispose_the_model()
        {
            mockBuilder.Consumers[0].Model.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
