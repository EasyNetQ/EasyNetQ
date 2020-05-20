// ReSharper disable InconsistentNaming

using EasyNetQ.ConnectionString;
using EasyNetQ.Producer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using System;
using Xunit;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    public class When_an_action_is_invoked : IDisposable
    {
        private readonly IClientCommandDispatcher dispatcher;
        private readonly IPersistentChannelFactory channelFactory;
        private readonly IPersistentConnection connection;
        private readonly int actionResult;

        public When_an_action_is_invoked()
        {
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            connection = Substitute.For<IPersistentConnection>();
            channelFactory = Substitute.For<IPersistentChannelFactory>();
            var channel = Substitute.For<IPersistentChannel>();
            var action = Substitute.For<Func<IModel, int>>();
            channelFactory.CreatePersistentChannel(connection).Returns(channel);
            channel.InvokeChannelActionAsync(action, default).Returns(42);

            dispatcher = new DefaultClientCommandDispatcher(configuration, connection, channelFactory);

            actionResult = dispatcher.InvokeAsync(action, default)
                .GetAwaiter()
                .GetResult();
        }

        public void Dispose()
        {
            dispatcher.Dispose();
        }

        [Fact]
        public void Should_create_a_persistent_channel()
        {
            channelFactory.Received().CreatePersistentChannel(connection);
        }

        [Fact]
        public void Should_receive_action_result()
        {
            actionResult.Should().Be(42);
        }
    }
}

// ReSharper restore InconsistentNaming
