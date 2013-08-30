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

        private readonly ConnectionConfiguration connectionConfiguration = new ConnectionConfiguration();

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

            mockBuilder.Bus.Subscribe<MyMessage>(subscriptionId, message =>
                {
                    deliveredMessage = message;
                });
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

        [Test]
        public void Should_deliver_message()
        {
            var properties = new BasicProperties
                {
                    Type = typeName,
                    CorrelationId = "some correlation id"
                };

            const string text = "Hello there, I am the text!";
            var originalMessage = new MyMessage {Text = text};
            var serializer = new JsonSerializer();

            var body = serializer.MessageToBytes(originalMessage);

            var consumer = mockBuilder.Consumers[0];
            consumer.HandleBasicDeliver(
                consumerTag,
                0,
                false, // redelivered
                typeName,
                "#",
                properties,
                body);

            Thread.Sleep(100);

            deliveredMessage.ShouldNotBeNull();
            deliveredMessage.Text.ShouldEqual(text);
        }
    }
}

// ReSharper restore InconsistentNaming