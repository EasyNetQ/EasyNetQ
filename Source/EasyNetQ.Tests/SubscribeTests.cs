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
using Rhino.Mocks;

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

            mockBuilder.Bus.Subscribe<MyMessage>(subscriptionId, message => { });
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
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.QueueDeclare(
                    Arg<string>.Is.Equal(queueName),
                    Arg<bool>.Is.Equal(true),  // durable
                    Arg<bool>.Is.Equal(false), // exclusive
                    Arg<bool>.Is.Equal(false), // autoDelete
                    Arg<IDictionary<string, object>>.Is.Anything));
        }

        [Test]
        public void Should_declare_the_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.ExchangeDeclare(
                typeName, "topic", true, false, null));
        }

        [Test]
        public void Should_bind_the_queue_and_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.QueueBind(queueName, typeName, "#"));
        }

        [Test]
        public void Should_set_configured_prefetch_count()
        {
            var connectionConfiguration = new ConnectionConfiguration();
            mockBuilder.Channels[1].AssertWasCalled(x => x.BasicQos(0, connectionConfiguration.PrefetchCount, false));
        }

        [Test]
        public void Should_start_consuming()
        {
            mockBuilder.Channels[1].AssertWasCalled(x =>
                x.BasicConsume(
                    Arg<string>.Is.Equal(queueName),
                    Arg<bool>.Is.Equal(false),
                    Arg<string>.Is.Anything,
                    Arg<bool>.Is.Equal(true),
                    Arg<bool>.Is.Equal(false),
                    Arg<IDictionary<string, object>>.Is.Anything,
                    Arg<IBasicConsumer>.Is.Anything));
        }

        [Test]
        public void Should_register_consumer()
        {
            mockBuilder.Consumers.Count.ShouldEqual(1);
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
            mockBuilder.Channels[1].AssertWasCalled(x => x.BasicAck(deliveryTag, false));
        }

        [Test]
        public void Should_write_debug_message()
        {
            const string expectedMessageFormat =
                "Received \n\tRoutingKey: '{0}'\n\tCorrelationId: '{1}'\n\tConsumerTag: '{2}'" +
                "\n\tDeliveryTag: {3}\n\tRedelivered: {4}";

            mockBuilder.Logger.AssertWasCalled(
                x => x.DebugWrite(expectedMessageFormat, "#", correlationId, consumerTag, deliveryTag, false));
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

            consumerErrorStrategy = MockRepository.GenerateStub<IConsumerErrorStrategy>();
            consumerErrorStrategy.Stub(x => x.HandleConsumerError(null, null))
                .IgnoreArguments()
                .WhenCalled(i =>
                {
                    basicDeliverEventArgs = (ConsumerExecutionContext)i.Arguments[0];
                    raisedException = (Exception) i.Arguments[1];
                }).Return(AckStrategies.Ack);

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

        [Test]
        public void Should_ack()
        {
            mockBuilder.Channels[1].AssertWasCalled(x => x.BasicAck(deliveryTag, false));
        }

        [Test]
        public void Should_write_exception_log_message()
        {
            // to brittle to put exact message here I think
            mockBuilder.Logger.AssertWasCalled(x => x.ErrorWrite(Arg<string>.Is.Anything, Arg<object[]>.Is.Anything));
        }

        [Test]
        public void Should_invoke_the_consumer_error_strategy()
        {
            consumerErrorStrategy.AssertWasCalled(x =>
                x.HandleConsumerError(Arg<ConsumerExecutionContext>.Is.Anything, Arg<Exception>.Is.Anything));
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
}

// ReSharper restore InconsistentNaming