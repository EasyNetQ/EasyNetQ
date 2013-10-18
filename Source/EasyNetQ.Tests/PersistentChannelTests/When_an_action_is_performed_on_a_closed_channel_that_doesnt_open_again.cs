// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Rhino.Mocks;

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
            persistentConnection = MockRepository.GenerateStub<IPersistentConnection>();
            var eventBus = MockRepository.GenerateStub<IEventBus>();

            var configuration = new ConnectionConfiguration
                {
                    Timeout = 1
                };

            var shutdownArgs = new ShutdownEventArgs(
                ShutdownInitiator.Peer,
                AmqpException.ConnectionClosed,
                "connection closed by peer");
            var exception = new OperationInterruptedException(shutdownArgs);

            persistentConnection.Stub(x => x.CreateModel()).WhenCalled(x =>
                {
                    throw exception;
                });

            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, eventBus);

        }

        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public void Should_throw_timeout_exception()
        {
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }
    }
}

// ReSharper restore InconsistentNaming