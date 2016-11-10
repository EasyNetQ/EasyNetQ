// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using NSubstitute;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    [TestFixture]
    public class When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again
    {
        private IPersistentChannel persistentChannel;
        private IPersistentConnection persistentConnection;

        [SetUp]
        public void SetUp()
        {
            persistentConnection = Substitute.For<IPersistentConnection>();
            var eventBus = Substitute.For<IEventBus>();

            var configuration = new ConnectionConfiguration
                {
                    Timeout = 1
                };

            var shutdownArgs = new ShutdownEventArgs(
                ShutdownInitiator.Peer,
                AmqpException.ConnectionClosed,
                "connection closed by peer");
            var exception = new OperationInterruptedException(shutdownArgs);

            persistentConnection.When(x => x.CreateModel()).Do(x =>
                {
                    throw exception;
                });

            var logger = Substitute.For<IEasyNetQLogger>();

            persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, eventBus);

        }

        [Test]
        public void Should_throw_timeout_exception()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
            });
        }
    }
}

// ReSharper restore InconsistentNaming