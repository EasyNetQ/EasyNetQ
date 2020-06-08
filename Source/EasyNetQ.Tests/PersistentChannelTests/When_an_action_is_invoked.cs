// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    public class When_an_action_is_invoked: IDisposable
    {
        public When_an_action_is_invoked()
        {
            persistentConnection = Substitute.For<IPersistentConnection>();
            channel = Substitute.For<IModel, IRecoverable>();

            persistentConnection.CreateModel().Returns(channel);

            persistentChannel = new PersistentChannel(
                new PersistentChannelOptions(), persistentConnection, Substitute.For<IEventBus>()
            );

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        private readonly IPersistentChannel persistentChannel;
        private readonly IPersistentConnection persistentConnection;
        private readonly IModel channel;

        [Fact]
        public void Should_open_a_channel()
        {
            persistentConnection.Received().CreateModel();
        }

        [Fact]
        public void Should_run_action_on_channel()
        {
            channel.Received().ExchangeDeclare("MyExchange", "direct");
        }

        public void Dispose()
        {
            persistentChannel.Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
