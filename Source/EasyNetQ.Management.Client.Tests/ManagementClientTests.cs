// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

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

            Console.Out.WriteLine("overview.ManagementVersion = {0}", overview.ManagementVersion);
            foreach (var exchangeType in overview.ExchangeTypes)
            {
                Console.Out.WriteLine("exchangeType.Name = {0}", exchangeType.Name);
            }
            foreach (var listener in overview.Listeners)
            {
                Console.Out.WriteLine("listener.IpAddress = {0}", listener.IpAddress);
            }

            Console.Out.WriteLine("overview.Messages = {0}", overview.QueueTotals.Messages);

            foreach (var context in overview.Contexts)
            {
                Console.Out.WriteLine("context.Description = {0}", context.Description);
            }
        }

        [Test]
        public void Should_get_nodes()
        {
            var nodes = managementClient.GetNodes();

            nodes.Count().ShouldEqual(1);
            nodes.First().Name.ShouldEqual("rabbit@THOMAS");
        }

        [Test]
        public void Should_get_definitions()
        {
            var definitions = managementClient.GetDefinitions();

            definitions.RabbitVersion.ShouldEqual("3.0.0");
        }

        [Test]
        public void Should_get_connections()
        {
            var connections = managementClient.GetConnections();

            foreach (var connection in connections)
            {
                Console.Out.WriteLine("connection.Name = {0}", connection.Name);

                ClientProperties clientProperties = connection.ClientProperties;

                Console.WriteLine("User:\t{0}", clientProperties.User);
                Console.WriteLine("Application:\t{0}", clientProperties.Application);
                Console.WriteLine("ClientApi:\t{0}", clientProperties.ClientApi);
                Console.WriteLine("ApplicationLocation:\t{0}", clientProperties.ApplicationLocation);
                Console.WriteLine("Connected:\t{0}", clientProperties.Connected);
                Console.WriteLine("EasynetqVersion:\t{0}", clientProperties.EasynetqVersion);
                Console.WriteLine("MachineName:\t{0}", clientProperties.MachineName);

                //Test the dynamic nature
                Console.WriteLine("Copyright:\t{0}", ((dynamic)clientProperties).Copyright);
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

        [Test, ExpectedException(typeof(UnexpectedHttpStatusCodeException))]
        public void Should_throw_when_trying_to_close_unknown_connection()
        {
            var connection = new Connection {Name = "unknown"};
            managementClient.CloseConnection(connection);
        }

        [Test]
        public void Should_get_channels()
        {
            var channels = managementClient.GetChannels();

            foreach (var channel in channels)
            {
                Console.Out.WriteLine("channel.Name = {0}", channel.Name);
                Console.Out.WriteLine("channel.User = {0}", channel.User);
                Console.Out.WriteLine("channel.PrefetchCount = {0}", channel.PrefetchCount);
            }
        }

        [Test]
        public void Should_get_exchanges()
        {
            var exchanges = managementClient.GetExchanges();

            foreach (Exchange exchange in exchanges)
            {
                Console.Out.WriteLine("exchange.Name = {0}", exchange.Name);
            }
        }

        private const string testExchange = "management_api_test_exchange";

        [Test]
        public void Should_be_able_to_get_an_individual_exchange_by_name()
        {
            var vhost = new Vhost { Name = "/" };
            var exchange = managementClient.GetExchange(testExchange, vhost);

            exchange.Name.ShouldEqual(testExchange);
        }

        [Test]
        public void Should_be_able_to_create_an_exchange()
        {
            var vhost = new Vhost {Name = "/"};

            var exchangeInfo = new ExchangeInfo(testExchange, "direct");
            var exchange = managementClient.CreateExchange(exchangeInfo, vhost);
            exchange.Name.ShouldEqual(testExchange);
        }

        [Test]
        public void Should_be_able_to_delete_an_exchange()
        {
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.Name == testExchange);
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
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.Name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var bindings = managementClient.GetBindingsWithSource(exchange);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.RoutingKey = {0}", binding.RoutingKey);
            }
        }

        [Test]
        public void Should_get_all_bindings_for_which_the_exchange_is_the_destination()
        {
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.Name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var bindings = managementClient.GetBindingsWithDestination(exchange);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.RoutingKey = {0}", binding.RoutingKey);
            }
        }

        [Test]
        public void Should_be_able_to_publish_to_an_exchange()
        {
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.Name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var publishInfo = new PublishInfo(testQueue, "Hello World");
            var result = managementClient.Publish(exchange, publishInfo);

            // the testExchange isn't bound to a queue
            result.Routed.ShouldBeFalse();
        }

        private const string testQueue = "management_api_test_queue";

        [Test]
        public void Should_get_queues()
        {
            var queues = managementClient.GetQueues();

            foreach (Queue queue in queues)
            {
                Console.Out.WriteLine("queue.Name = {0}", queue.Name);
            }
        }

        [Test]
        public void Should_be_able_to_get_a_queue_by_name()
        {
            var vhost = new Vhost { Name = "/" };
            var queue = managementClient.GetQueue(testQueue, vhost);
            queue.Name.ShouldEqual(testQueue);
        }

        [Test]
        public void Should_be_able_to_create_a_queue()
        {
            var vhost = managementClient.GetVhost("/");
            var queueInfo = new QueueInfo(testQueue);
            var queue = managementClient.CreateQueue(queueInfo, vhost);
            queue.Name.ShouldEqual(testQueue);
        }

        [Test]
        public void Should_be_able_to_delete_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.Name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            managementClient.DeleteQueue(queue);
        }

        [Test]
        public void Should_be_able_to_get_all_the_bindings_for_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.Name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            var bindings = managementClient.GetBindingsForQueue(queue);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.RoutingKey = {0}", binding.RoutingKey);
            }
        }

        [Test]
        public void Should_purge_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.Name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            managementClient.Purge(queue);
        }

        [Test]
        public void Should_be_able_to_get_messages_from_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.Name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }

            var defaultExchange = new Exchange { Name = "amq.default", Vhost = "/" };

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
                Console.Out.WriteLine("message.Payload = {0}", message.Payload);
                foreach (var property in message.Properties)
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
                Console.Out.WriteLine("binding.Destination = {0}", binding.Destination);
                Console.Out.WriteLine("binding.Source = {0}", binding.Source);
                Console.Out.WriteLine("binding.PropertiesKey = {0}", binding.PropertiesKey);
            }
        }

        [Test]
        public void Should_be_able_to_get_a_list_of_bindings_between_an_exchange_and_a_queue()
        {
            var queue = managementClient.GetQueues().SingleOrDefault(x => x.Name == testQueue);
            if (queue == null)
            {
                throw new ApplicationException("Test queue has not been created");
            }
            var exchange = managementClient.GetExchanges().SingleOrDefault(x => x.Name == testExchange);
            if (exchange == null)
            {
                throw new ApplicationException(
                    string.Format("Test exchange '{0}' hasn't been created", testExchange));
            }

            var bindings = managementClient.GetBindings(exchange, queue);

            foreach (var binding in bindings)
            {
                Console.Out.WriteLine("binding.RoutingKey = {0}", binding.RoutingKey);
                Console.Out.WriteLine("binding.PropertiesKey = {0}", binding.PropertiesKey);
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
                Console.Out.WriteLine("vhost.Name = {0}", vhost.Name);
            }
        }

        private const string testVHost = "management_test_virtual_host";

        [Test]
        public void Should_be_able_to_get_an_individual_vhost()
        {
            var vhost = managementClient.GetVhost(testVHost);
            vhost.Name.ShouldEqual(testVHost);
        }

        [Test]
        public void Should_create_a_virtual_host()
        {
            var vhost = managementClient.CreateVirtualHost(testVHost);
            vhost.Name.ShouldEqual(testVHost);
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
                Console.Out.WriteLine("user.Name = {0}", user.Name);
            }
        }

        private const string testUser = "mikey";

        [Test]
        public void Should_be_able_to_get_a_user_by_name()
        {
            var user = managementClient.GetUser(testUser);
            user.Name.ShouldEqual(testUser);
        }

        [Test]
        public void Should_be_able_to_create_a_user()
        {
            var userInfo = new UserInfo(testUser, "topSecret").AddTag("administrator");

            var user = managementClient.CreateUser(userInfo);
            user.Name.ShouldEqual(testUser);
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
                Console.Out.WriteLine("permission.User = {0}", permission.User);
                Console.Out.WriteLine("permission.Vhost = {0}", permission.Vhost);
                Console.Out.WriteLine("permission.Configure = {0}", permission.Configure);
                Console.Out.WriteLine("permission.Read = {0}", permission.Read);
                Console.Out.WriteLine("permission.Write = {0}", permission.Write);
            }
        }

        [Test]
        public void Should_be_able_to_create_permissions()
        {
            var user = managementClient.GetUsers().SingleOrDefault(x => x.Name == testUser);
            if (user == null)
            {
                throw new ApplicationException(string.Format("user '{0}' hasn't been created", testUser));
            }
            var vhost = managementClient.GetVHosts().SingleOrDefault(x => x.Name == testVHost);
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
                .SingleOrDefault(x => x.User == testUser && x.Vhost == testVHost);

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
            var vhost = managementClient.GetVHosts().SingleOrDefault(x => x.Name == testVHost);
            if (vhost == null)
            {
                throw new ApplicationException(string.Format("Test vhost: '{0}' has not been created", testVHost));
            }
            managementClient.IsAlive(vhost).ShouldBeTrue();
        }
    }
}

// ReSharper restore InconsistentNaming