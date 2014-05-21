// ReSharper disable InconsistentNaming

using EasyNetQ.Events;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    [TestFixture]
    public class When_a_channel_action_is_invoked
    {
        private IPersistentChannel persistentChannel;
        private IPersistentConnection persistentConnection;
        private IModel channel;
        private IEventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            persistentConnection = MockRepository.GenerateStub<IPersistentConnection>();
            channel = MockRepository.GenerateStub<IModel>();
            var configuration = new ConnectionConfiguration();
            eventBus = MockRepository.GenerateStub<IEventBus>();

            persistentConnection.Stub(x => x.CreateModel()).Return(channel);
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, eventBus);

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        [Test]
        public void Should_open_a_channel()
        {
            persistentConnection.AssertWasCalled(x => x.CreateModel());
        }

        [Test]
        public void Should_run_action_on_channel()
        {
            channel.AssertWasCalled(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        [Test]
        public void Should_raise_a_PublishChannelCreatedEvent()
        {
            eventBus.AssertWasCalled(x => x.Publish(Arg<PublishChannelCreatedEvent>.Is.Anything));
        }
    }
}

// ReSharper restore InconsistentNaming