// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests
{
    [TestFixture]
    [Explicit("Requires a RabbitMQ server on localhost to work")]
    public class ScenarioTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        /// <summary>
        /// Demonstrate how to create a virtual host, add some users, set permissions
        /// and create exchanges, queues and bindings.
        /// </summary>
        [Test]
        public void Should_be_able_to_provision_a_virtual_host()
        {
            var initial = new ManagementClient("http://localhost", "guest", "guest");

            // first create a new virtual host
            var vhost = initial.CreateVirtualHost("my_virtual_host");

            // next create a user for that virutal host
            var user = initial.CreateUser(new UserInfo("mike", "topSecret"));

            // give the new user all permissions on the virtual host
            initial.CreatePermission(new PermissionInfo(user, vhost));

            // now log in again as the new user
            var management = new ManagementClient("http://localhost", user.Name, "topSecret");

            // test that everything's OK
            management.IsAlive(vhost);

            // create an exchange
            var exchange = management.CreateExchange(new ExchangeInfo("my_exchagne", "direct"), vhost);

            // create a queue
            var queue = management.CreateQueue(new QueueInfo("my_queue"), vhost);

            // bind the exchange to the queue
            management.CreateBinding(exchange, queue, new BindingInfo("my_routing_key"));

            // publish a test message
            management.Publish(exchange, new PublishInfo("my_routing_key", "Hello World!"));

            // get any messages on the queue
            var messages = management.GetMessagesFromQueue(queue, new GetMessagesCriteria(1, false));

            foreach (var message in messages)
            {
                Console.Out.WriteLine("message.payload = {0}", message.Payload);
            }
        }
    }
}

// ReSharper restore InconsistentNaming