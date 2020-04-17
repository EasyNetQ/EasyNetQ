// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Integration test, requires a RabbitMQ instance on localhost")]
    public class PublisherConfirmsIntegrationTests : IDisposable
    {
        public PublisherConfirmsIntegrationTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost;publisherConfirms=true;timeout=10");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private readonly IBus bus;

        [Fact]
        public async Task PublisherConfirmShouldNotTimeOut()
        {
            var resetEvent = new AutoResetEvent(false);

            await bus.PubSub.SubscribeAsync<MyMessage>("my_subscription_id", msg => { resetEvent.Set(); });
            await bus.PubSub.PublishAsync(new MyMessage {Text = "WUHU"});
            var signalReceived = resetEvent.WaitOne(TimeSpan.FromSeconds(15));

            Assert.True(signalReceived);
        }

        [Fact(Skip = "Needs to be verified manually")]
        public void Should_be_able_to_interrupt_publishing()
        {
            // while we publish with publisher confirms on, we should be able to kill the
            // RabbitMQ connection and see the publish successfully continue.

            while (true)
                bus.PubSub.Publish(new MyMessage
                {
                    Text = "Hello World!"
                });
        }

        [Fact]
        public void Should_be_able_to_publish_asynchronously()
        {
            var count = 0;
            while (count++ < 10000)
                bus.PubSub.PublishAsync(new MyMessage
                {
                    Text = string.Format("Message {0}", count)
                }).ContinueWith(task =>
                {
                    if (task.IsCompleted)
                    {
                        //Console.Out.WriteLine("{0} Completed", count);
                    }

                    if (task.IsFaulted)
                    {
                        Console.Out.WriteLine("\n\n");
                        Console.Out.WriteLine(task.Exception);
                        Console.Out.WriteLine("\n\n");
                    }
                });
            //Thread.Sleep(1);

            Thread.Sleep(10000);
        }

        [Fact]
        public void Subscribe()
        {
            bus.PubSub.Subscribe<MyMessage>("publish_confirms", message => { });
        }
    }
}

// ReSharper restore InconsistentNaming
