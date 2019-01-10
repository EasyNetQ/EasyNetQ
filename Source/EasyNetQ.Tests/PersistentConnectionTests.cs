using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests
{
    public class PersistentConnectionTests
    {
        [Fact]
        [Explicit("Explicit as this sometimes fails on AppVeyor (but works locally), possibly because of Thread.Sleep")]
        public void If_connects_after_disposal_should_redispose_underlying_connection()
        {
            var eventBus = Substitute.For<IEventBus>();
            var connectionFactory = Substitute.For<IConnectionFactory>();
            var mockConnection = Substitute.For<IConnection>();
            var mockPersistentConnection = Substitute.For<PersistentConnection>(connectionFactory, eventBus);

            // This test is constructed using small delays, such that the IConnectionFactory will return a connection just _after the IPersistentConnection has been disposed.
            var shimDelayMs = 500;
            connectionFactory.CreateConnection().Returns(a =>
            {
                Thread.Sleep(shimDelayMs * 2);
                return mockConnection;
            });

            Task.Factory.StartNew(() => { mockPersistentConnection.Initialize(); }); // Start the persistent connection attempting to connect.

            Thread.Sleep(shimDelayMs); // Allow some time for the persistent connection code to try to create a connection.

            // First call to dispose.  Because CreateConnection() is stubbed to delay for shimDelay.Double(), it will not yet have returned a connection.  So when the PersistentConnection is disposed, no underlying IConnection should yet be disposed.
            mockPersistentConnection.Dispose();
            mockConnection.DidNotReceive().Dispose();

            Thread.Sleep(shimDelayMs * 2); // Allow time for persistent connection code to _return its connection ...

            // Assert that the connection returned from connectionFactory.CreateConnection() (_after the PersistentConnection was disposed), still gets disposed.
            mockConnection.Received().Dispose();

            // Ensure that PersistentConnection also did not flag (eg to IPersistentChannel) the late-made connection as successful.
            connectionFactory.DidNotReceive().Success();
            // Ensure that PersistentConnection does not retry after was disposed.
            connectionFactory.DidNotReceive().Next();

        }

        private void ThrowException(Type exceptionType)
        {
            if (exceptionType == typeof(Exception))
            {
                throw new Exception("Test");
            }
            if (exceptionType == typeof(BrokerUnreachableException))
            {
                throw new BrokerUnreachableException(new Exception("Test"));
            }
            if (exceptionType == typeof(SocketException))
            {
                throw new SocketException();
            }
            if (exceptionType == typeof(TimeoutException))
            {
                throw new TimeoutException();
            }
        }

        [InlineData(typeof(BrokerUnreachableException))]
        [InlineData(typeof(SocketException))]
        [Theory]
        public void Should_retry_connection_for_expected_connection_types_CreateConnection(Type exceptionType)
        {
            var mockBuilder = new MockBuilder();
            using (mockBuilder.Bus)
            {
                mockBuilder.ConnectionFactory.Configuration.ConnectIntervalAttempt = TimeSpan.FromSeconds(1);
                mockBuilder.ConnectionFactory.Succeeded.Returns(false, true);
                mockBuilder.ConnectionFactory.CreateConnection().Returns(c =>
                {
                    ThrowException(exceptionType);
                    return null;
                }, c => mockBuilder.Connection);
                var connection = new PersistentConnection(mockBuilder.ConnectionFactory, mockBuilder.EventBus);
                connection.Initialize();
                connection.IsConnected.Should().BeFalse();
                Thread.Sleep(TimeSpan.FromSeconds(2));
                connection.IsConnected.Should().BeTrue();
                mockBuilder.ConnectionFactory.Received(3).CreateConnection();
            }
        }

        [InlineData(typeof(TimeoutException))]
        [Theory]
        public void Should_retry_connection_for_expected_connection_types_OnConnected(Type exceptionType)
        {
            var mockBuilder = new MockBuilder();

            using (mockBuilder.Bus)
            {
                var first = true;
                var succeeded = false;
                mockBuilder.ConnectionFactory.Succeeded.Returns(c => succeeded);
                mockBuilder.ConnectionFactory.When(x => x.Success()).Do(c => succeeded = true);
                mockBuilder.ConnectionFactory.Configuration.ConnectIntervalAttempt = TimeSpan.FromSeconds(1);          
                var connection = new PersistentConnection(mockBuilder.ConnectionFactory, mockBuilder.EventBus);
                mockBuilder.EventBus.Subscribe<ConnectionCreatedEvent>(e => {
                    if (first)
                    {
                        first = false;
                        ThrowException(exceptionType);
                    }
                });

                connection.Initialize();
                Thread.Sleep(TimeSpan.FromSeconds(2));
                mockBuilder.ConnectionFactory.Received(3).CreateConnection();
                first.Should().BeFalse();
            }
        }

        [Fact]
        public void Should_not_retry_connection_for_unexpected_connection_types()
        {
            var mockBuilder = new MockBuilder();
            using (mockBuilder.Bus)
            {
                mockBuilder.ConnectionFactory.Configuration.ConnectIntervalAttempt = TimeSpan.FromSeconds(1);
                mockBuilder.ConnectionFactory.CreateConnection().Returns(c =>
                {
                    ThrowException(typeof(Exception));
                    return null;
                });
                var connection = new PersistentConnection(mockBuilder.ConnectionFactory, mockBuilder.EventBus);
                Assert.Throws<Exception>(() => connection.Initialize());
            }
        }
    }
}
