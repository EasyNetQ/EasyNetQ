// ReSharper disable InconsistentNaming

using EasyNetQ.Events;
using EasyNetQ.Producer;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    public class When_a_channel_action_is_invoked
    {
        public When_a_channel_action_is_invoked()
        {
            persistentConnection = Substitute.For<IPersistentConnection>();
            channel = Substitute.For<IModel, IRecoverable>();
            var configuration = new ConnectionConfiguration();
            eventBus = Substitute.For<IEventBus>();

            persistentConnection.CreateModel().Returns(channel);

            persistentChannel = new PersistentChannel(persistentConnection, configuration, eventBus);

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        private IPersistentChannel persistentChannel;
        private IPersistentConnection persistentConnection;
        private IModel channel;
        private IEventBus eventBus;

        [Fact]
        public void Should_open_a_channel()
        {
            persistentConnection.Received().CreateModel();
        }

        [Fact]
        public void Should_raise_a_PublishChannelCreatedEvent()
        {
            eventBus.Received().Publish(Arg.Any<PublishChannelCreatedEvent>());
        }

        [Fact]
        public void Should_run_action_on_channel()
        {
            channel.Received().ExchangeDeclare("MyExchange", "direct");
        }
    }
}

// ReSharper restore InconsistentNaming
