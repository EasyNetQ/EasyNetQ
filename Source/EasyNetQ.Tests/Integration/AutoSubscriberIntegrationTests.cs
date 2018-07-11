// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using System.Threading;
using EasyNetQ.AutoSubscribe;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a RabbitMQ broker on localhost.")]
    public class AutoSubscriberIntegrationTests : IDisposable
    {
        private IBus bus;

        public AutoSubscriberIntegrationTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
            var subscriber = new AutoSubscriber(bus, "autosub.integration");

            subscriber.Subscribe(GetType().GetTypeInfo().Assembly);
        }

        public void Dispose()
        {
            // give the message a chance to get delivered
            Thread.Sleep(500);
            bus.Dispose();
        }

        [Fact]
        [Explicit("Requires a RabbitMQ broker on localhost.")]
        public void PublishWithTopic()
        {
            bus.Publish(new AutoSubMessage{ Text = "With topic" }, "mytopic");
        }

        [Fact]
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