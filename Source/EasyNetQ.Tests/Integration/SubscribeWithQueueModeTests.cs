using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using FluentAssertions;
using System;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class SubscribeWithQueueModeTests : IDisposable
    {
        private IBus bus;

        public SubscribeWithQueueModeTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            if (bus != null) bus.Dispose();
        }

        [Fact]
        [Explicit("Requires RabbitMQ 3.6+ to run on localhost")]
        public void SubscribeWithDefaultQueueMode()
        {
            var subscriptionId = "QueueModeTest1";
            var conventions = new Conventions(new DefaultTypeNameSerializer());
            var queueName = conventions.QueueNamingConvention(typeof(MyMessage), subscriptionId);
            var client = new ManagementClient("http://localhost", "guest", "guest");

            bus.PubSub.Subscribe<MyMessage>(subscriptionId, msg => { }, x => x.WithQueueMode(QueueMode.Default));

            var queue = client.GetQueue(queueName, new Vhost { Name = "/" });
            queue.Should().NotBeNull();
            queue.Arguments.Should().ContainKey("x-queue-mode");
            queue.Arguments["x-queue-mode"].Should().Be("default");
        }

        [Fact]
        [Explicit("Requires RabbitMQ 3.6+ to run on localhost")]
        public void SubscribeWithLazyQueueMode()
        {
            var subscriptionId = "QueueModeTest2";
            var conventions = new Conventions(new DefaultTypeNameSerializer());
            var queueName = conventions.QueueNamingConvention(typeof(MyMessage), subscriptionId);
            var client = new ManagementClient("http://localhost", "guest", "guest");

            bus.PubSub.Subscribe<MyMessage>(subscriptionId, msg => { }, x => x.WithQueueMode(QueueMode.Lazy));

            var queue = client.GetQueue(queueName, new Vhost { Name = "/" });
            queue.Should().NotBeNull();
            queue.Arguments.Should().ContainKey("x-queue-mode");
            queue.Arguments["x-queue-mode"].Should().Be("lazy");
        }

        [Fact]
        [Explicit("Requires RabbitMQ to run on localhost")]
        public void SubscribeWithInvalidQueueMode()
        {
            Assert.ThrowsAny<Exception>(() => bus.PubSub.Subscribe<MyMessage>("QueueModeTest3", msg => { }, x => x.WithQueueMode("some_queue_mode")) );
        }
    }
}
