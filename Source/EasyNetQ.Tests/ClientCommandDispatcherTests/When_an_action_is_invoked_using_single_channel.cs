// ReSharper disable InconsistentNaming

using EasyNetQ.Producer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using System;
using Xunit;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    public class When_an_action_is_invoked_using_single_channel : IDisposable
    {
        private readonly IClientCommandDispatcher dispatcher;
        private readonly IPersistentChannelFactory channelFactory;
        private readonly int actionResult;

        public When_an_action_is_invoked_using_single_channel()
        {
            channelFactory = Substitute.For<IPersistentChannelFactory>();
            var channel = Substitute.For<IPersistentChannel>();
            var action = Substitute.For<Func<IModel, int>>();
            channelFactory.CreatePersistentChannel(new PersistentChannelOptions()).Returns(channel);
            channel.InvokeChannelActionAsync(action).Returns(42);

            dispatcher = new SingleChannelClientCommandDispatcher(channelFactory);

            actionResult = dispatcher.InvokeAsync(action, ChannelDispatchOptions.Default)
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
            channelFactory.Received().CreatePersistentChannel(new PersistentChannelOptions());
        }

        [Fact]
        public void Should_receive_action_result()
        {
            actionResult.Should().Be(42);
        }
    }
}

// ReSharper restore InconsistentNaming
