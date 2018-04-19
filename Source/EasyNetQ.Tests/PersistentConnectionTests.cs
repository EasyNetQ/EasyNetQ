﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests
{
    public class PersistentConnectionTests
    {
        [Fact]
        public void If_connects_after_disposal_should_redispose_underlying_connection()
        {
            var eventBus = Substitute.For<IEventBus>();
            var connectionFactory = Substitute.For<IConnectionFactory>();
            var mockConnection = Substitute.For<IConnection>();
            PersistentConnection mockPersistentConnection = Substitute.For<PersistentConnection>(connectionFactory, eventBus);

            // This test is constructed using small delays, such that the IConnectionFactory will return a connection just _after the IPersistentConnection has been disposed.
            TimeSpan shimDelay = TimeSpan.FromSeconds(0.5);
            connectionFactory.CreateConnection().Returns(a =>
            {
                Thread.Sleep(shimDelay.Double());
                return mockConnection;
            });

            Task.Factory.StartNew(() => { mockPersistentConnection.Initialize(); }); // Start the persistent connection attempting to connect.

            Thread.Sleep(shimDelay); // Allow some time for the persistent connection code to try to create a connection.

            // First call to dispose.  Because CreateConnection() is stubbed to delay for shimDelay.Double(), it will not yet have returned a connection.  So when the PersistentConnection is disposed, no underlying IConnection should yet be disposed.
            mockPersistentConnection.Dispose();
            mockConnection.DidNotReceive().Dispose();

            Thread.Sleep(shimDelay.Double()); // Allow time for persistent connection code to _return its connection ...

            // Assert that the connection returned from connectionFactory.CreateConnection() (_after the PersistentConnection was disposed), still gets disposed.
            mockConnection.Received().Dispose();

            // Ensure that PersistentConnection also did not flag (eg to IPersistentChannel) the late-made connection as successful.
            connectionFactory.DidNotReceive().Success();
            // Ensure that PersistentConnection does not retry after was disposed.
            connectionFactory.DidNotReceive().Next();

        }
    }
}
