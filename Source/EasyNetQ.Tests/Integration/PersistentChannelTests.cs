// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading;
using EasyNetQ.ConnectionString;
using EasyNetQ.Producer;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a broker on localhost.")]
    public class PersistentChannelTests : IDisposable
    {
        private IPersistentConnection connection;
        private IPersistentChannel persistentChannel;

        public PersistentChannelTests()
        {
            var eventBus = new EventBus();
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var hostSelectionStrategy = new RandomClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(configuration, hostSelectionStrategy);
            connection = new PersistentConnection(connectionFactory, eventBus);
            persistentChannel = new PersistentChannel(connection, configuration, new EventBus());
            connection.Initialize();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        [Fact]
        public void Should_be_able_to_run_channel_actions()
        {
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("myExchange", "direct", true, false, new Dictionary<string, object>()));
        }

        [Fact]
        public void Should_allow_non_disconnect_Amqp_exception_to_bubble_up()
        {
            // run test above first
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("myExchange", "topic", true, false, new Dictionary<string, object>()));
        }

        [Fact]
        public void Should_reconnect_if_connection_goes_away()
        {
            Helpers.CloseConnection();

            // now try to declare an exchange
            persistentChannel.InvokeChannelAction(x =>
                {
                    Console.Out.WriteLine("Running exchange declare");
                    x.ExchangeDeclare("myExchange", "direct", true, false, new Dictionary<string, object>());
                    Console.Out.WriteLine("Ran exchange declare");
                });

            Thread.Sleep(1000);
        }
    }
}

// ReSharper restore InconsistentNaming