// ReSharper disable InconsistentNaming

using System.Threading;
using EasyNetQ.ConnectionString;
using EasyNetQ.Loggers;
using EasyNetQ.Management.Client;
using EasyNetQ.Producer;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture, Explicit("Requires a broker on localhost.")]
    public class PersistentChannelTests
    {
        private IPersistentConnection connection;
        private IPersistentChannel persistentChannel;

        [SetUp]
        public void SetUp()
        {
            var logger = new ConsoleLogger();
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var hostSelectionStrategy = new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(configuration, hostSelectionStrategy);
            connection = new PersistentConnection(connectionFactory, logger);
            persistentChannel = new PersistentChannel(connection, logger);
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        public void Should_be_able_to_run_channel_actions()
        {
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("myExchange", "direct"));
        }

        [Test]
        public void Should_recover_from_failure()
        {
            // run test above first
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("myExchange", "topic"));
        }

        [Test]
        public void Should_reconnect_if_connection_goes_away()
        {
            // close the connection
            var client = new ManagementClient("http://localhost", "guest", "guest", 15672);
            foreach (var clientConnection in client.GetConnections())
            {
                client.CloseConnection(clientConnection);
            }
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("myExchange", "direct"));

            Thread.Sleep(1000);
        }
    }
}

// ReSharper restore InconsistentNaming