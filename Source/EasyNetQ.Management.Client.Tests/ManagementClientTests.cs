// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests
{
    [TestFixture]
    [Explicit ("requires a rabbitMQ instance on localhost to run")]
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

            Console.Out.WriteLine("overview.management_version = {0}", overview.management_version);
            foreach (var exchangeType in overview.exchange_types)
            {
                Console.Out.WriteLine("exchangeType.name = {0}", exchangeType.name);
            }
            foreach (var listener in overview.listeners)
            {
                Console.Out.WriteLine("listener.ip_address = {0}", listener.ip_address);
            }

            Console.Out.WriteLine("overview.queue_totals = {0}", overview.queue_totals.messages);

            foreach (var context in overview.contexts)
            {
                Console.Out.WriteLine("context.description = {0}", context.description);
            }
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

            definitions.rabbit_version.ShouldEqual("3.0.0");
        }

        [Test]
        public void Should_get_connections()
        {
            var connections = managementClient.GetConnections();

            foreach (var connection in connections)
            {
                Console.Out.WriteLine("connection.name = {0}", connection.name);
                Console.WriteLine("user:\t{0}", connection.client_properties.user);
                Console.WriteLine("application:\t{0}", connection.client_properties.application);
                Console.WriteLine("client_api:\t{0}", connection.client_properties.client_api);
                Console.WriteLine("application_location:\t{0}", connection.client_properties.application_location);
                Console.WriteLine("connected:\t{0}", connection.client_properties.connected);
                Console.WriteLine("easynetq_version:\t{0}", connection.client_properties.easynetq_version);
                Console.WriteLine("machine_name:\t{0}", connection.client_properties.machine_name);
            }
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

            foreach (var channel in channels)
            {
                Console.Out.WriteLine("channel.name = {0}", channel.name);
                Console.Out.WriteLine("channel.user = {0}", channel.user);
                Console.Out.WriteLine("channel.prefetch_count = {0}", channel.prefetch_count);
            }
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
        public void Should_be_able_to_get_an_individual_exchange_by_name()
        {
            var vhost = new Vhost { name = "/" };
            var exchange = managementClient.GetExchange(testExchange, vhost);

            exchange.name.ShouldEqual(testExchange);
        }

        [Test]
        public void Should_be_able_to_create_an_exchange()
        {
            var vhost = new Vhost {name = "/"};

            var exchangeInfo = new ExchangeInfo(testExchange, "direct");
            var exchange = managementClient.CreateExchange(exchangeInfo, vhost);
            exchange.name.ShouldEqual(testExchange);
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
        public void Should_be_able_to_get_a_queue_by_name()
        {
            var vhost = new Vhost { name = "/" };
            var queue = managementClient.GetQueue(testQueue, vhost);
            queue.name.ShouldEqual(testQueue);
        }

        [Test]
        public void Should_be_able_to_create_a_queue()
        {
            var vhost = managementClient.GetVhost("/");
            var queueInfo = new QueueInfo(testQueue);
            var queue = managementClient.CreateQueue(queueInfo, vhost);
            queue.name.ShouldEqual(testQueue);
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

            var defaultExchange = new Exchange { name = "amq.default", vhost = "/" };

            var properties = new Dictionary<string, string>
            {
                { "app_id", "management-test"}
            };

            var publishInfo = new PublishInfo(properties, testQueue, "Hello World", "string");

            managementClient.Publish(defaultExchange, publishInfo);

            var criteria = new GetMessagesCriteria(1, false);
            var messages = managementClient.GetMessagesFromQueue(queue, criteria);

            foreach (var message in messages)
            {
                Console.Out.WriteLine("message.payload = {0}", message.payload);
                foreach (var property in message.properties)
                {
                    Console.Out.WriteLine("key: '{0}', value: '{1}'", property.Key, property.Value);
                }
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
            var vhost = managementClient.GetVhost("/");
            var queue = managementClient.GetQueue(testQueue, vhost);
            var exchange = managementClient.GetExchange(testExchange, vhost);

            var bindingInfo = new BindingInfo(testQueue);

            managementClient.CreateBinding(exchange, queue, bindingInfo);
        }

        [Test]
        public void Should_delete_binding()
        {
            var vhost = managementClient.GetVhost("/");
            var queue = managementClient.GetQueue(testQueue, vhost);
            var exchange = managementClient.GetExchange(testExchange, vhost);

            var bindings = managementClient.GetBindings(exchange, queue);

            foreach (var binding in bindings)
            {
                managementClient.DeleteBinding(binding);
            }
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

        private const string testVHost = "management_test_virtual_host";

        [Test]
        public void Should_be_able_to_get_an_individual_vhost()
        {
            var vhost = managementClient.GetVhost(testVHost);
            vhost.name.ShouldEqual(testVHost);
        }

        [Test]
        public void Should_create_a_virtual_host()
        {
            var vhost = managementClient.CreateVirtualHost(testVHost);
            vhost.name.ShouldEqual(testVHost);
        }

        [Test]
        public void Should_delete_a_virtual_host()
        {
            var vhost = managementClient.GetVhost(testVHost);
            managementClient.DeleteVirtualHost(vhost);
        }

        [Test]
        public void Should_get_users()
        {
            var users = managementClient.GetUsers();

            foreach (var user in users)
            {
                Console.Out.WriteLine("user.name = {0}", user.name);
            }
        }

        private const string testUser = "mikey";

        [Test]
        public void Should_be_able_to_get_a_user_by_name()
        {
            var user = managementClient.GetUser(testUser);
            user.name.ShouldEqual(testUser);
        }

        [Test]
        public void Should_be_able_to_create_a_user()
        {
            var userInfo = new UserInfo(testUser, "topSecret").AddTag("administrator");

            var user = managementClient.CreateUser(userInfo);
            user.name.ShouldEqual(testUser);
        }

        [Test]
        public void Should_be_able_to_delete_a_user()
        {
            var user = managementClient.GetUser(testUser);
            managementClient.DeleteUser(user);
        }

        [Test]
        public void Should_get_permissions()
        {
            var permissions = managementClient.GetPermissions();

            foreach (var permission in permissions)
            {
                Console.Out.WriteLine("permission.user = {0}", permission.user);
                Console.Out.WriteLine("permission.vhost = {0}", permission.vhost);
                Console.Out.WriteLine("permission.configure = {0}", permission.configure);
                Console.Out.WriteLine("permission.read = {0}", permission.read);
                Console.Out.WriteLine("permission.write = {0}", permission.write);
            }
        }

        [Test]
        public void Should_be_able_to_create_permissions()
        {
            var user = managementClient.GetUsers().SingleOrDefault(x => x.name == testUser);
            if (user == null)
            {
                throw new ApplicationException(string.Format("user '{0}' hasn't been created", testUser));
            }
            var vhost = managementClient.GetVHosts().SingleOrDefault(x => x.name == testVHost);
            if (vhost == null)
            {
                throw new ApplicationException(string.Format("Test vhost: '{0}' has not been created", testVHost));
            }

            var permissionInfo = new PermissionInfo(user, vhost);
            managementClient.CreatePermission(permissionInfo);
        }

        [Test]
        public void Should_be_able_to_delete_permissions()
        {
            var permission = managementClient.GetPermissions()
                .SingleOrDefault(x => x.user == testUser && x.vhost == testVHost);

            if (permission == null)
            {
                throw new ApplicationException(string.Format("No permission for vhost: {0} and user: {1}",
                    testVHost, testUser));
            }

            managementClient.DeletePermission(permission);
        }

        [Test]
        public void Should_check_that_the_broker_is_alive()
        {
            var vhost = managementClient.GetVHosts().SingleOrDefault(x => x.name == testVHost);
            if (vhost == null)
            {
                throw new ApplicationException(string.Format("Test vhost: '{0}' has not been created", testVHost));
            }
            managementClient.IsAlive(vhost).ShouldBeTrue();
        }
    }
}

// ReSharper restore InconsistentNaming