using System;
using System.Collections.Generic;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests
{
    public class PersistentConnectionTests
    {
        [Fact]
        public void Should_be_not_connected_if_connection_not_established()
        {
            var mockBuilder = new MockBuilder();
            mockBuilder.ConnectionFactory.CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>())
                .Returns(c => throw new Exception("Test"));

            using var connection = new PersistentConnection(
                new ConnectionConfiguration(), mockBuilder.ConnectionFactory, mockBuilder.EventBus
            );

            Assert.Throws<Exception>(() => connection.CreateModel());

            connection.IsConnected.Should().BeFalse();
            mockBuilder.ConnectionFactory.Received().CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>());
        }

        [Fact]
        public void Should_establish_connection_when_persistent_connection_created()
        {
            var mockBuilder = new MockBuilder();
            using var connection = new PersistentConnection(
                new ConnectionConfiguration(), mockBuilder.ConnectionFactory, mockBuilder.EventBus
            );

            connection.CreateModel();

            connection.IsConnected.Should().BeTrue();
            mockBuilder.ConnectionFactory.Received(1).CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>());
        }

        [Fact]
        public void Should_use_same_connection() {
            ushort maxAvailableServerChannels = 1;
            var mockBuilder = new MockBuilder();
            mockBuilder.Connection.ChannelMax.Returns(maxAvailableServerChannels);

            using var connection = new PersistentConnection(
                new ConnectionConfiguration(), mockBuilder.ConnectionFactory, mockBuilder.EventBus
            );

            var model1 = connection.CreateModel();
            var model2 = connection.CreateModel();

            Assert.True(model1.ChannelNumber == model2.ChannelNumber);
        }

        [Fact]
        public void Should_use_different_connections() {
            ushort maxAvailableServerChannels = 10;
            var mockBuilder = new MockBuilder();
            mockBuilder.Connection.ChannelMax.Returns(maxAvailableServerChannels);

            using var connection = new PersistentConnection(
                new ConnectionConfiguration(), mockBuilder.ConnectionFactory, mockBuilder.EventBus
            );

            var model1 = connection.CreateModel();
            var model2 = connection.CreateModel();

            Assert.True(model1.ChannelNumber != model2.ChannelNumber);
        }

        [Fact]
        public void Should_create_new_connection_after_disposal_of_old() {
            ushort maxAvailableServerChannels = 1;
            var mockBuilder = new MockBuilder();
            mockBuilder.Connection.ChannelMax.Returns(maxAvailableServerChannels);

            using var connection = new PersistentConnection(
                new ConnectionConfiguration(), mockBuilder.ConnectionFactory, mockBuilder.EventBus
            );

            var model1 = connection.CreateModel();
            model1.Close();
            var model2 = connection.CreateModel();

            Assert.True(model1.ChannelNumber != model2.ChannelNumber);
        }
    }
}
