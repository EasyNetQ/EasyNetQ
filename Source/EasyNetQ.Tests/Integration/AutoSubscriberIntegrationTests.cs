// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using System.Threading;
using EasyNetQ.AutoSubscribe;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Requires a RabbitMQ broker on localhost.")]
    public class AutoSubscriberIntegrationTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
            var subscriber = new AutoSubscriber(bus, "autosub.integration");

            subscriber.Subscribe(Assembly.GetExecutingAssembly());
        }

        [TearDown]
        public void TearDown()
        {
            // give the message a chance to get devlivered
            Thread.Sleep(500);
            bus.Dispose();
        }

        [Test]
        [Explicit("Requires a RabbitMQ broker on localhost.")]
        public void PublishWithTopic()
        {
            bus.Publish(new AutoSubMessage{ Text = "With topic" }, "mytopic");
        }

        [Test]
        [Explicit("Requires a RabbitMQ broker on localhost.")]
        public void PublishWithoutTopic()
        {
            bus.Publish(new AutoSubMessage{ Text = "Without topic" });
        }
    }

    public class AutoSubMessage
    {
        public string Text { get; set; }
    }

    public class MyConsumer : IConsume<AutoSubMessage>
    {
        [ForTopic("mytopic")]
        public void Consume(AutoSubMessage message)
        {
            Console.Out.WriteLine("Autosubscriber got message: {0}", message.Text);
        }
    }
}

// ReSharper restore InconsistentNaming