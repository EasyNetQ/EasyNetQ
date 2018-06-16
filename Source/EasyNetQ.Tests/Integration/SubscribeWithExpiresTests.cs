using System.Threading;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class SubscribeWithExpiresTests
    {
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Queue_should_be_deleted_after_the_expires_ttl()
        {
            Assert.Throws<UnexpectedHttpStatusCodeException>(() =>
            {
                var bus = RabbitHutch.CreateBus("host=localhost");

                var subscriptionId = "TestSubscriptionWithExpires";
                var conventions = new Conventions(new DefaultTypeNameSerializer());
                var queueName = conventions.QueueNamingConvention(typeof(MyMessage), subscriptionId);
                var client = new ManagementClient("http://localhost", "guest", "guest");
                var vhost = new Vhost { Name = "/" };

                bus.Subscribe<MyMessage>(subscriptionId, message => { }, x => x.WithExpires(1000));

                var queue = client.GetQueue(queueName, vhost);
                queue.Should().NotBeNull();

                // this will abandon the queue... poor queue!
                bus.Dispose();

                Thread.Sleep(1500);

                queue = client.GetQueue(queueName, vhost);
                queue.Should().BeNull();
            });
        }

        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Queue_should_not_be_deleted_if_expires_is_not_set()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            var subscriptionId = "TestSubscriptionWithoutExpires";
            var conventions = new Conventions(new DefaultTypeNameSerializer());
            var queueName = conventions.QueueNamingConvention(typeof(MyMessage), subscriptionId);
            var client = new ManagementClient("http://localhost", "guest", "guest");
            var vhost = new Vhost { Name = "/" };

            bus.Subscribe<MyMessage>(subscriptionId, message => { });

            var queue = client.GetQueue(queueName, vhost);
            queue.Should().NotBeNull();

            // this will abandon the queue... poor queue!
            bus.Dispose();

            Thread.Sleep(1500);

            queue = client.GetQueue(queueName, vhost);
            queue.Should().NotBeNull();
        }
    }
}

// ReSharper restore InconsistentNaming