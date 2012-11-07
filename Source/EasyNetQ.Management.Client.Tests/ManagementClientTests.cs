// ReSharper disable InconsistentNaming

using System;
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
        public void Should_get_bindings()
        {
            var bindings = managementClient.GetBindings();

            foreach (Binding binding in bindings)
            {
                Console.Out.WriteLine("binding.destination = {0}", binding.destination);
                Console.Out.WriteLine("binding.source = {0}", binding.source);
            }
        }

        [Test]
        public void Should_get_vhosts()
        {
            var vhosts = managementClient.GetVHosts();

            foreach (Vhost vhost in vhosts)
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