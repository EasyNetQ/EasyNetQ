using EasyNetQ.Events;
using EasyNetQ.Producer;
using NUnit.Framework;    
using NSubstitute;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    [TestFixture]
    public class When_a_ConnectionCreatedEvent_is_published
    {
        private IPersistentConnection persistentConnection;
        private IEventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            persistentConnection = Substitute.For<IPersistentConnection>();
            var configuration = new ConnectionConfiguration();
            eventBus = new EventBus();
            var logger = Substitute.For<IEasyNetQLogger>();

            var persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, eventBus);
        }

        [Test]
        [Ignore("It seems to be not actual now, discuss it later. Looks like odd optimization")]
        public void Should_not_open_a_channel_when_not_connected()
        {
            eventBus.Publish(new ConnectionCreatedEvent());
            persistentConnection.DidNotReceive().CreateModel();
        }

        [Test]
        public void Should_open_a_channel_when_connected()
        {
            persistentConnection.IsConnected.Returns(true);
            eventBus.Publish(new ConnectionCreatedEvent());
            persistentConnection.Received().CreateModel();
        }
    }
}