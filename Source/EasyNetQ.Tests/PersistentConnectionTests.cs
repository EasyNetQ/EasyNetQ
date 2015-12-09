using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class PersistentConnectionTests
    {
        [Test]
        public void If_connects_after_disposal_should_redispose_underlying_connection()
        {
            var logger = MockRepository.GenerateMock<IEasyNetQLogger>();
            var eventBus = MockRepository.GenerateMock<IEventBus>();
            var connectionFactory = MockRepository.GenerateMock<IConnectionFactory>();
            var mockConnection = MockRepository.GenerateMock<IConnection>();
            PersistentConnection mockPersistentConnection = MockRepository.GenerateMock<PersistentConnection>(connectionFactory, logger, eventBus);

            // This test is constructed using small delays, such that the IConnectionFactory will return a connection just _after the IPersistentConnection has been disposed.
            TimeSpan shimDelay = TimeSpan.FromSeconds(0.5); 
            connectionFactory.Expect(cf => cf.CreateConnection()).WhenCalled(a =>
            {
                Thread.Sleep(shimDelay.Double());
                a.ReturnValue = mockConnection;
            }).Repeat.Once();

            Task.Factory.StartNew(() => { mockPersistentConnection.Initialize(); }); // Start the persistent connection attempting to connect.

            Thread.Sleep(shimDelay); // Allow some time for the persistent connection code to try to create a connection.

            // First call to dispose.  Because CreateConnection() is stubbed to delay for shimDelay.Double(), it will not yet have returned a connection.  So when the PersistentConnection is disposed, no underlying IConnection should yet be disposed.
            mockPersistentConnection.Dispose();
            mockConnection.AssertWasNotCalled(underlyingConnection => underlyingConnection.Dispose());

            Thread.Sleep(shimDelay.Double()); // Allow time for persistent connection code to _return its connection ...

            // Assert that the connection returned from connectionFactory.CreateConnection() (_after the PersistentConnection was disposed), still gets disposed.
            mockConnection.AssertWasCalled(latedCreatedUnderlyingConnection => latedCreatedUnderlyingConnection.Dispose());

            // Ensure that PersistentConnection also did not flag (eg to IPersistentChannel) the late-made connection as successful.
            connectionFactory.AssertWasNotCalled(c => c.Success());
            // Ensure that PersistentConnection does not retry after was disposed.
            connectionFactory.AssertWasNotCalled(c => c.Next());

        }
    }
}
