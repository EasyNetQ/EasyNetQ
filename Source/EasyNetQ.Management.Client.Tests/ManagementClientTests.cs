// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests
{
    [TestFixture]
    public class ManagementClientTests
    {
        private IManagementClient managementClient;

        private const string hostUrl = "http://localhost";
        private const string username = "guest";
        private const string password = "guest";

        [SetUp]
        public void SetUp()
        {
            managementClient = new ManagementClient(hostUrl, username, password);
        }

        [Test]
        public void Should_get_overview()
        {
            var overview = managementClient.GetOverview();

            overview.management_version.ShouldEqual("2.8.6");
            overview.exchange_types[0].name.ShouldEqual("topic");
        }

        [Test]
        public void Should_get_nodes()
        {
            var nodes = managementClient.GetNodes();

            nodes.Count().ShouldEqual(1);
            nodes.First().name.ShouldEqual("rabbit@THOMAS");
        }

        [Test]
        public void Should_get_definitions()
        {
            var definitions = managementClient.GetDefinitions();

            definitions.rabbit_version.ShouldEqual("2.8.6");
        }

        [Test]
        public void Should_get_connections()
        {
            var connections = managementClient.GetConnections();

            connections.Count().ShouldEqual(1);
            connections.First().name.ShouldEqual("[::1]:57775 -> [::1]:5672");
        }

        [Test]
        public void Should_be_able_to_close_connection()
        {
            // first get a connection
            var connections = managementClient.GetConnections();

            // then close it
            managementClient.CloseConnection(connections.First());
        }

        [Test, ExpectedException(typeof(EasyNetQManagementException))]
        public void Should_throw_when_trying_to_close_unknown_connection()
        {
            var connection = new Connection {name = "unknown"};
            managementClient.CloseConnection(connection);
        }

        [Test]
        public void Should_get_channels()
        {
            var channels = managementClient.GetChannels();

            channels.Count().ShouldEqual(1);
            channels.First().consumer_count.ShouldEqual(1);
        }

        [Test]
        public void Should_get_exchanges()
        {
            var exchanges = managementClient.GetExchanges();

            foreach (Exchange exchange in exchanges)
            {
                Console.Out.WriteLine("exchange.name = {0}", exchange.name);
            }
        }

        private const string testExchange = "management_api_test_exchange";

        [Test]
        public void Should_be_able_to_create_an_exchange()
        {
            var vhost = new Vhost {name = "/"};

            var exchangeInfo = new ExchangeInfo(testExchange, "direct");
            managementClient.CreateExchange(exchangeInfo, vhost);
        }

        [Test]
        public void Should_be_able_to_delete_an_exchange()
        {
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            managementClient.DeleteExchange(exchange);
        }

        [Test]
        public void Should_get_all_bindings_for_which_the_exchange_is_the_source()
        {
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var bindings = managementClient.GetBindingsWithSource(exchange);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.routing_key = {0}", binding.routing_key);
            }
        }

        [Test]
        public void Should_get_all_bindings_for_which_the_exchange_is_the_destination()
        {
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var bindings = managementClient.GetBindingsWithDestination(exchange);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.routing_key = {0}", binding.routing_key);
            }
        }

        [Test]
        public void Should_be_able_to_publish_to_an_exchange()
        {
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var publishInfo = new PublishInfo(testQueue, "Hello World");

            var result = managementClient.Publish(exchange, publishInfo);

            // the testExchange isn't bound to a queue
            result.routed.ShouldBeFalse();
        }

        private const string testQueue = "management_api_test_queue";

        [Test]
        public void Should_get_queues()
        {
            var queues = managementClient.GetQueues();

            foreach (Queue queue in queues)
            {
                Console.Out.WriteLine("queue.name = {0}", queue.name);
            }
        }

        [Test]
        public void Should_be_able_to_create_a_queue()
        {
            var queueInfo = new QueueInfo(testQueue);
            var vhost = new Vhost {name = "/"};

            managementClient.CreateQueue(queueInfo, vhost);
        }

        [Test]
        public void Should_be_able_to_delete_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            managementClient.DeleteQueue(queue);
        }

        [Test]
        public void Should_be_able_to_get_all_the_bindings_for_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            var bindings = managementClient.GetBindingsForQueue(queue);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.routing_key = {0}", binding.routing_key);
            }
        }

        [Test]
        public void Should_purge_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            managementClient.Purge(queue);
        }

        [Test]
        public void Should_be_able_to_get_messages_from_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            var criteria = new GetMessagesCriteria(1, true);

            var messages = managementClient.GetMessagesFromQueue(queue, criteria);

            foreach (var message in messages)
            {
                Console.Out.WriteLine("message.payload = {0}", message.payload);
            }
        }

        [Test]
        public void Should_get_bindings()
        {
            var bindings = managementClient.GetBindings();

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.destination = {0}", binding.destination);
                Console.Out.WriteLine("binding.source = {0}", binding.source);
                Console.Out.WriteLine("binding.properties_key = {0}", binding.properties_key);
            }
        }

        [Test]
        public void Should_be_able_to_get_a_list_of_bindings_between_an_exchange_and_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var bindings = managementClient.GetBindings(exchange, queue);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.routing_key = {0}", binding.routing_key);
                Console.Out.WriteLine("binding.properties_key = {0}", binding.properties_key);
            }
        }

        [Test]
        public void Should_create_binding()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var bindingInfo = new BindingInfo(testQueue);

            managementClient.CreateBinding(exchange, queue, bindingInfo);
        }

        [Test]
        public void Should_delete_binding()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var binding = managementClient.GetBindings(exchange, queue).FirstOrDefault();
            if (binding == null)
            {
                throw new ApplicationException("Test binding has not been created");
            }

            managementClient.DeleteBinding(binding);
        }

        [Test]
        public void Should_get_vhosts()
        {
            var vhosts = managementClient.GetVHosts();

            foreach (var vhost in vhosts)
            {
                Console.Out.WriteLine("vhost.name = {0}", vhost.name);
            }
        }

        [Test]
        public void Should_get_users()
        {
            var users = managementClient.GetUsers();

            foreach (User user in users)
            {
                Console.Out.WriteLine("user.name = {0}", user.name);
            }
        }

        [Test]
        public void Should_get_permissions()
        {
            var permissions = managementClient.GetPermissions();

            foreach (Permission permission in permissions)
            {
                Console.Out.WriteLine("permission.user = {0}", permission.user);
            }
        }
    }
}

// ReSharper restore InconsistentNaming