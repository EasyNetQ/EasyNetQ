using System.Threading;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    public class SubscribeWithExpiresTests
    {
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        [ExpectedException(typeof(UnexpectedHttpStatusCodeException))]
        public void Queue_should_be_deleted_after_the_expires_ttl()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            var subscriptionId = "TestSubscriptionWithExpires";
            var conventions = new Conventions(new TypeNameSerializer());
            var queueName = conventions.QueueNamingConvention(typeof(MyMessage), subscriptionId);
            var client = new ManagementClient("http://localhost", "guest", "guest");
            var vhost = new Vhost { Name = "/" };

            bus.Subscribe<MyMessage>(subscriptionId, message => { }, x => x.WithExpires(1000));

            var queue = client.GetQueue(queueName, vhost);
            queue.ShouldNotBeNull();
            
            // this will abandon the queue... poor queue!
            bus.Dispose();

            Thread.Sleep(1500);

            queue = client.GetQueue(queueName, vhost);
            queue.ShouldBeNull();
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Queue_should_not_be_deleted_if_expires_is_not_set()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            var subscriptionId = "TestSubscriptionWithoutExpires";
            var conventions = new Conventions(new TypeNameSerializer());
            var queueName = conventions.QueueNamingConvention(typeof(MyMessage), subscriptionId);
            var client = new ManagementClient("http://localhost", "guest", "guest");
            var vhost = new Vhost { Name = "/" };

            bus.Subscribe<MyMessage>(subscriptionId, message => { });

            var queue = client.GetQueue(queueName, vhost);
            queue.ShouldNotBeNull();

            // this will abandon the queue... poor queue!
            bus.Dispose();

            Thread.Sleep(1500);

            queue = client.GetQueue(queueName, vhost);
            queue.ShouldNotBeNull();
        }
    }
}

// ReSharper restore InconsistentNaming