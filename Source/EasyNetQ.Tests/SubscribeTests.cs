// ReSharper disable InconsistentNaming

using System.Collections;
using System.Text;
using System.Threading;
using EasyNetQ.Loggers;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class When_subscribe_is_called
    {
        private MockBuilder mockBuilder;

        private const string typeName = "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string queueName = typeName + "_" + subscriptionId;
        private const string consumerTag = "the_consumer_tag";

        [SetUp]
        public void SetUp()
        {
            var conventions = new Conventions
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
            mockBuilder.Channels.Count.ShouldEqual(1);
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
                    Arg<IDictionary>.Is.Anything));
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
            mockBuilder.Channels[0].AssertWasCalled(x => x.BasicQos(0, connectionConfiguration.PrefetchCount, false));
        }

        [Test]
        public void Should_start_consuming()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.BasicConsume(
                    Arg<string>.Is.Equal(queueName),
                    Arg<bool>.Is.Equal(false),
                    Arg<string>.Is.Anything,
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

        private const string typeName = "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests";
        private const string subscriptionId = "the_subscription_id";
        private const string consumerTag = "the_consumer_tag";
        private const ulong deliveryTag = 123;

        private MyMessage originalMessage;
        private MyMessage deliveredMessage;

        [SetUp]
        public void SetUp()
        {
            var conventions = new Conventions
            {
                ConsumerTagConvention = () => consumerTag
            };

            mockBuilder = new MockBuilder(x => x
                .Register<IConventions>(_ => conventions)
                //.Register<IEasyNetQLogger>(_ => new ConsoleLogger())
                );

            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.Bus.Subscribe<MyMessage>(subscriptionId, message =>
            {
                deliveredMessage = message;
                autoResetEvent.Set();
            });


            const string text = "Hello there, I am the text!";
            originalMessage = new MyMessage { Text = text };

            var body = new JsonSerializer().MessageToBytes(originalMessage);

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
                    CorrelationId = "some correlation id"
                },
                body);

            // wait for the subscription thread to handle the message ...
            autoResetEvent.WaitOne(1000);
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
            mockBuilder.Channels[0].AssertWasCalled(x => x.BasicAck(deliveryTag, false));
        }
    }
}

// ReSharper restore InconsistentNaming