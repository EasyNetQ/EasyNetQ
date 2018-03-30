using EasyNetQ.Events;
using EasyNetQ.Producer;
using Xunit;    
using NSubstitute;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    public class When_a_ConnectionCreatedEvent_is_published
    {
        private IPersistentConnection persistentConnection;
        private IEventBus eventBus;

        public When_a_ConnectionCreatedEvent_is_published()
        {
            persistentConnection = Substitute.For<IPersistentConnection>();
            var configuration = new ConnectionConfiguration();
            eventBus = new EventBus();
            var persistentChannel = new PersistentChannel(persistentConnection, configuration, eventBus);
        }

        [Fact(Skip = "It seems to be not actual now, discuss it later. Looks like odd optimization")]
        public void Should_not_open_a_channel_when_not_connected()
        {
            eventBus.Publish(new ConnectionCreatedEvent());
            persistentConnection.DidNotReceive().CreateModel();
        }

        [Fact]
        public void Should_open_a_channel_when_connected()
        {
            persistentConnection.IsConnected.Returns(true);
            eventBus.Publish(new ConnectionCreatedEvent());
            persistentConnection.Received().CreateModel();
        }
    }
}