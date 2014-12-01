// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.ConnectionString;
using EasyNetQ.Loggers;
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
            var eventBus = new EventBus();
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var hostSelectionStrategy = new RandomClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(configuration, hostSelectionStrategy);
            connection = new PersistentConnection(connectionFactory, logger, eventBus);
            persistentChannel = new PersistentChannel(connection, logger, configuration, new EventBus());
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
        public void Should_allow_non_disconnect_Amqp_exception_to_bubble_up()
        {
            // run test above first
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("myExchange", "topic"));
        }

        [Test]
        public void Should_reconnect_if_connection_goes_away()
        {
            Helpers.CloseConnection();

            // now try to declare an exchange
            persistentChannel.InvokeChannelAction(x =>
                {
                    Console.Out.WriteLine("Running exchange declare");
                    x.ExchangeDeclare("myExchange", "direct");
                    Console.Out.WriteLine("Ran exchange declare");
                });

            Thread.Sleep(1000);
        }
    }
}

// ReSharper restore InconsistentNaming