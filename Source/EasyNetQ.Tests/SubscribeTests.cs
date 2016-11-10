using System.Collections.Generic;
// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using NSubstitute;
using System.Linq;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class When_subscribe_is_called
    {
        private MockBuilder mockBuilder;

        private const string typeName = "EasyNetQ.Tests.MyMessage:EasyNetQ.Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string queueName = typeName + "_" + subscriptionId;
        private const string consumerTag = "the_consumer_tag";

        private ISubscriptionResult subscriptionResult; 

        [SetUp]
        public void SetUp()
        {
            var conventions = new Conventions(new TypeNameSerializer())
                {
                    ConsumerTagConvention = () => consumerTag
                };

            mockBuilder = new MockBuilder(x => x
                .Register<IConventions>(_ => conventions)
                //.Register<IEasyNetQLogger>(_ => new ConsoleLogger())
                );

            subscriptionResult = mockBuilder.Bus.Subscribe<MyMessage>(subscriptionId, message => { });
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_create_a_new_channel_for_the_consumer()
        {
            // A channel is created for running client originated commands,
            // a second channel is created for the consumer.
            mockBuilder.Channels.Count.ShouldEqual(2);
        }

        [Test]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                    Arg.Is(queueName),
                    Arg.Is(true),  // durable
                    Arg.Is(false), // exclusive
                    Arg.Is(false), // autoDelete
                    Arg.Any<IDictionary<string, object>>());
        }

        [Test]
        public void Should_declare_the_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is(typeName),
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(new Dictionary<string, object>())));
        }

        [Test]
        public void Should_bind_the_queue_and_exchange()
        {
            mockBuilder.Channels[0].Received().QueueBind(
                Arg.Is(queueName), 
                Arg.Is(typeName), 
                Arg.Is("#"),
                Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(new Dictionary<string, object>())));
        }

        [Test]
        public void Should_set_configured_prefetch_count()
        {
            var connectionConfiguration = new ConnectionConfiguration();
            mockBuilder.Channels[1].Received().BasicQos(0, connectionConfiguration.PrefetchCount, false);
        }

        [Test]
        public void Should_start_consuming()
        {
            mockBuilder.Channels[1].Received().BasicConsume(
                    Arg.Is(queueName),
                    Arg.Is(false),
                    Arg.Any<string>(),
                    Arg.Is(true),
                    Arg.Is(false),
                    Arg.Any<IDictionary<string, object>>(),
                    Arg.Any<IBasicConsumer>());
        }

        [Test]
        public void Should_register_consumer()
        {
            mockBuilder.Consumers.Count.ShouldEqual(1);
        }

        [Test]
        public void Should_return_non_null_and_with_expected_values_result()
        {
            Assert.IsNotNull(subscriptionResult);
            Assert.IsNotNull(subscriptionResult.Exchange);
            Assert.IsNotNull(subscriptionResult.Queue);
            Assert.IsNotNull(subscriptionResult.ConsumerCancellation);
            Assert.IsTrue(subscriptionResult.Exchange.Name == typeName);
            Assert.IsTrue(subscriptionResult.Queue.Name == queueName);
        }
    }

    [TestFixture]
    public class When_a_message_is_delivered
    {
        private MockBuilder mockBuilder;

        private const string typeName = "EasyNetQ.Tests.MyMessage:EasyNetQ.Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string correlationId = "the_correlation_id";
        private const string consumerTag = "the_consumer_tag";
        private const ulong deliveryTag = 123;

        private MyMessage originalMessage;
        private MyMessage deliveredMessage;

        [SetUp]
        public void SetUp()
        {
            var conventions = new Conventions(new TypeNameSerializer())
            {
                ConsumerTagConvention = () => consumerTag
            };

            mockBuilder = new MockBuilder(x => x
                .Register<IConventions>(_ => conventions)
                //.Register<IEasyNetQLogger>(_ => new ConsoleLogger())
                );

            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());

            mockBuilder.Bus.Subscribe<MyMessage>(subscriptionId, message =>
            {
                deliveredMessage = message;
            });

            const string text = "Hello there, I am the text!";
            originalMessage = new MyMessage { Text = text };

            var body = new JsonSerializer(new TypeNameSerializer()).MessageToBytes(originalMessage);

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

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_build_bus_successfully()
        {
            // just want to run SetUp()
        }

        [Test]
        public void Should_deliver_message()
        {
            deliveredMessage.ShouldNotBeNull();
            deliveredMessage.Text.ShouldEqual(originalMessage.Text);
        }

        [Test]
        public void Should_ack_the_message()
        {
            mockBuilder.Channels[1].Received().BasicAck(deliveryTag, false);
        }

        [Test]
        public void Should_write_debug_message()
        {
            const string expectedMessageFormat =
                "Received \n\tRoutingKey: '{0}'\n\tCorrelationId: '{1}'\n\tConsumerTag: '{2}'" +
                "\n\tDeliveryTag: {3}\n\tRedelivered: {4}";

            mockBuilder.Logger.Received().DebugWrite(expectedMessageFormat, "#", correlationId, consumerTag, deliveryTag, false);
        }
    }

    [TestFixture]
    public class When_the_handler_throws_an_exception
    {
        private MockBuilder mockBuilder;
        private IConsumerErrorStrategy consumerErrorStrategy;

        private const string typeName = "EasyNetQ.Tests.MyMessage:EasyNetQ.Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string correlationId = "the_correlation_id";
        private const string consumerTag = "the_consumer_tag";
        private const ulong deliveryTag = 123;

        private MyMessage originalMessage;
        private readonly Exception originalException = new Exception("Some exception message");
        private ConsumerExecutionContext basicDeliverEventArgs;
        private Exception raisedException;

        [SetUp]
        public void SetUp()
        {
            var conventions = new Conventions(new TypeNameSerializer())
            {
                ConsumerTagConvention = () => consumerTag
            };

            consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            consumerErrorStrategy.HandleConsumerError(null, null)
                .ReturnsForAnyArgs(i =>
                {
                    basicDeliverEventArgs = (ConsumerExecutionContext)i[0];
                    raisedException = (Exception)i[1];
                    return AckStrategies.Ack;
                });

            mockBuilder = new MockBuilder(x => x
                .Register<IConventions>(_ => conventions)
                .Register(_ => consumerErrorStrategy)
                //.Register<IEasyNetQLogger>(_ => new ConsoleLogger())
                );

            mockBuilder.Bus.Subscribe<MyMessage>(subscriptionId, message =>
            {
                throw originalException;
            });


            const string text = "Hello there, I am the text!";
            originalMessage = new MyMessage { Text = text };

            var body = new JsonSerializer(new TypeNameSerializer()).MessageToBytes(originalMessage);

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
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_ack()
        {
            mockBuilder.Channels[1].Received().BasicAck(deliveryTag, false);
        }

        [Test]
        public void Should_write_exception_log_message()
        {
            // to brittle to put exact message here I think
            mockBuilder.Logger.Received().ErrorWrite(Arg.Any<string>(), Arg.Any<object[]>());
        }

        [Test]
        public void Should_invoke_the_consumer_error_strategy()
        {
            consumerErrorStrategy.Received().HandleConsumerError(Arg.Any<ConsumerExecutionContext>(), Arg.Any<Exception>());
        }

        [Test]
        public void Should_pass_the_exception_to_consumerErrorStrategy()
        {
            raisedException.ShouldNotBeNull();
            raisedException.InnerException.ShouldNotBeNull();
            raisedException.InnerException.ShouldBeTheSameAs(originalException);
        }

        [Test]
        public void Should_pass_the_deliver_args_to_the_consumerErrorStrategy()
        {
            basicDeliverEventArgs.ShouldNotBeNull();
            basicDeliverEventArgs.Info.ConsumerTag.ShouldEqual(consumerTag);
            basicDeliverEventArgs.Info.DeliverTag.ShouldEqual(deliveryTag);
            basicDeliverEventArgs.Info.RoutingKey.ShouldEqual("#");
        }
    }

    [TestFixture]
    public class When_a_subscription_is_cancelled_by_the_user
    {
        private MockBuilder mockBuilder;
        private const string subscriptionId = "the_subscription_id";
        private const string consumerTag = "the_consumer_tag";

        [SetUp]
        public void SetUp()
        {
            var conventions = new Conventions(new TypeNameSerializer())
            {
                ConsumerTagConvention = () => consumerTag
            };

            mockBuilder = new MockBuilder(x => x.Register<IConventions>(_ => conventions));
            var subscriptionResult = mockBuilder.Bus.Subscribe<MyMessage>(subscriptionId, message => { });
             var are = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<ConsumerModelDisposedEvent>(x => are.Set());
            subscriptionResult.Dispose();
            are.WaitOne(500);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_dispose_the_model()
        {
            mockBuilder.Consumers[0].Model.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming