// ReSharper disable InconsistentNaming

using System.Threading;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Events;
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
            var eventBus = new EventBus();
            var configuration = new ConnectionConfiguration();

            var shutdownArgs = new ShutdownEventArgs(
                ShutdownInitiator.Peer, 
                AmqpException.ConnectionClosed,
                "connection closed by peer");
            var exception = new OperationInterruptedException(shutdownArgs);

            persistentConnection.Stub(x => x.CreateModel()).Throw(exception).Repeat.Once();
            persistentConnection.Stub(x => x.CreateModel()).Return(channel).Repeat.Any();

            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, eventBus);

            new Timer(_ => eventBus.Publish(new ConnectionCreatedEvent())).Change(10, Timeout.Infinite);

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        [Test]
        public void Should_run_action_on_channel()
        {
            Thread.Sleep(100);
            channel.AssertWasCalled(x => x.ExchangeDeclare("MyExchange", "direct"));
        }
    }
}

// ReSharper restore InconsistentNaming