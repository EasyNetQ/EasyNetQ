using EasyNetQ.Events;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    [TestFixture]
    public class When_a_ConnectionCreatedEvent_is_published
    {
        private IPersistentConnection persistentConnection;
        private IModel channel;
        private IEventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            persistentConnection = MockRepository.GenerateStub<IPersistentConnection>();
            persistentConnection.Stub(x => x.CreateModel());
            var configuration = new ConnectionConfiguration();
            eventBus = new EventBus();
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            var persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, eventBus);
        }

        [Test]
        [Ignore("It seems to be not actual now, discuss it later. Looks like odd optimization")]
        public void Should_not_open_a_channel_when_not_connected()
        {
            eventBus.Publish(new ConnectionCreatedEvent());
            persistentConnection.AssertWasNotCalled(x => x.CreateModel());
        }

        [Test]
        public void Should_open_a_channel_when_connected()
        {
            persistentConnection.Stub(x => x.IsConnected).Return(true);
            eventBus.Publish(new ConnectionCreatedEvent());
            persistentConnection.AssertWasCalled(x => x.CreateModel());
        }
    }
}