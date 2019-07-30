﻿using System;
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
            using (mockBuilder.Bus)
            {
                mockBuilder.ConnectionFactory.CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>()).Returns(c => throw new Exception("Test"));
                using (var connection = new PersistentConnection(new ConnectionConfiguration(), mockBuilder.ConnectionFactory, mockBuilder.EventBus))
                {
                    connection.Initialize();

                    connection.IsConnected.Should().BeFalse();
                    mockBuilder.ConnectionFactory.Received().CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>());
                }
            }
        }

        [Fact]
        public void Should_establish_connection_when_persistent_connection_created()
        {
            var mockBuilder = new MockBuilder();
            using (mockBuilder.Bus)
            using (var connection = new PersistentConnection(new ConnectionConfiguration(), mockBuilder.ConnectionFactory, mockBuilder.EventBus))
            {
                connection.Initialize();

                connection.IsConnected.Should().BeTrue();
                mockBuilder.ConnectionFactory.Received(2).CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>());
            }
        }
    }
}
