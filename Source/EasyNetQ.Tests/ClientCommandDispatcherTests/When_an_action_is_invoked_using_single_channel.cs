﻿// ReSharper disable InconsistentNaming

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
        private readonly IPersistentConnection connection;
        private readonly int actionResult;

        public When_an_action_is_invoked_using_single_channel()
        {
            connection = Substitute.For<IPersistentConnection>();
            channelFactory = Substitute.For<IPersistentChannelFactory>();
            var channel = Substitute.For<IPersistentChannel>();
            var action = Substitute.For<Func<IModel, int>>();
            channelFactory.CreatePersistentChannel(connection).Returns(channel);
            channel.InvokeChannelActionAsync(action).Returns(42);

            dispatcher = new SingleChannelClientCommandDispatcher(connection, channelFactory);

            actionResult = dispatcher.InvokeAsync(action)
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
