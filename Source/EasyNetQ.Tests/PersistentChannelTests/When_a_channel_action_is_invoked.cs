// ReSharper disable InconsistentNaming

using EasyNetQ.Events;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using NSubstitute;

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
            persistentConnection = Substitute.For<IPersistentConnection>();
            channel = Substitute.For<IModel>();
            var configuration = new ConnectionConfiguration();
            eventBus = Substitute.For<IEventBus>();

            persistentConnection.CreateModel().Returns(channel);
            var logger = Substitute.For<IEasyNetQLogger>();

            persistentChannel = new PersistentChannel(persistentConnection, logger, configuration, eventBus);

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        [Test]
        public void Should_open_a_channel()
        {
            persistentConnection.Received().CreateModel();
        }

        [Test]
        public void Should_run_action_on_channel()
        {
            channel.Received().ExchangeDeclare("MyExchange", "direct");
        }

        [Test]
        public void Should_raise_a_PublishChannelCreatedEvent()
        {
            eventBus.Received().Publish(Arg.Any<PublishChannelCreatedEvent>());
        }
    }
}

// ReSharper restore InconsistentNaming