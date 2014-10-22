// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Producer;
using EasyNetQ.Producer.Waiters;
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

            var reconnectionWaiterFactory = MockRepository.GenerateStub<IReconnectionWaiterFactory>();
            var waiter = MockRepository.GenerateStub<IReconnectionWaiter>();
            reconnectionWaiterFactory.Stub(x => x.GetWaiter()).Return(waiter);
            persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, reconnectionWaiterFactory, eventBus);

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