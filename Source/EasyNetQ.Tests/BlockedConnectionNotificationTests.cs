using System;
// ReSharper disable InconsistentNaming
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class When_a_connection_becomes_blocked
    {
        private MockBuilder mockBuilder;
        private IConnection connection;
        private IAdvancedBus advancedBus;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            connection = mockBuilder.Connection;
            advancedBus = mockBuilder.Bus.Advanced;
        }

        [Test]
        public void Should_raise_blocked_event()
        {
            var blocked = false;
            advancedBus.Blocked += (s,e) => blocked = true;
            connection.Raise(r => r.ConnectionBlocked += (s, e) => { }, connection, new ConnectionBlockedEventArgs("some reason"));

            Assert.That(blocked, Is.True);
        }
    }

    [TestFixture]
    public class When_a_connection_becomes_unblocked
    {
        private MockBuilder mockBuilder;
        private IConnection connection;
        private IAdvancedBus advancedBus;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            connection = mockBuilder.Connection;
            advancedBus = mockBuilder.Bus.Advanced;
        }

        [Test]
        public void Should_raise_unblocked_event()
        {
            var blocked = true;
            advancedBus.Unblocked += (s,e) => blocked = false;
            connection.Raise(r => r.ConnectionUnblocked += (s, e) => { }, connection, new EventArgs());

            Assert.That(blocked, Is.False);
        }
    }
}
