// ReSharper disable InconsistentNaming

using System.Threading;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Loggers;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Rhino.Mocks;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    [TestFixture]
    public class When_an_action_is_performed_on_a_closed_channel_that_then_opens
    {
        private IPersistentChannel persistentChannel;
        private IPersistentConnection persistentConnection;
        private IModel channel;

        [SetUp]
        public void SetUp()
        {
            persistentConnection = MockRepository.GenerateStub<IPersistentConnection>();
            channel = MockRepository.GenerateStub<IModel>();
            var configuration = new ConnectionConfiguration();

            var shutdownArgs = new ShutdownEventArgs(
                ShutdownInitiator.Peer, 
                AmqpException.ConnectionClosed,
                "connection closed by peer");
            var exception = new OperationInterruptedException(shutdownArgs);
            var first = true;

            persistentConnection.Stub(x => x.CreateModel()).WhenCalled(x =>
                {
                    if (first)
                    {
                        first = false;
                        throw exception;
                    }
                    x.ReturnValue = channel;
                });

            var logger = new ConsoleLogger();

            persistentChannel = new PersistentChannel(persistentConnection, logger, configuration);

            new Timer(_ => 
                persistentConnection.Raise(x => x.Connected += () => { })).Change(10, Timeout.Infinite);

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        [Test]
        public void Should_run_action_on_channel()
        {
            channel.AssertWasCalled(x => x.ExchangeDeclare("MyExchange", "direct"));
        }
    }
}

// ReSharper restore InconsistentNaming