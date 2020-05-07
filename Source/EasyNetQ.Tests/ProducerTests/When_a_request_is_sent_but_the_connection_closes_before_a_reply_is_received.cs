// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Producer;
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_request_is_sent_but_the_connection_closes_before_a_reply_is_received : IDisposable
    {
        public When_a_request_is_sent_but_the_connection_closes_before_a_reply_is_received()
        {
            mockBuilder = new MockBuilder();
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;

        [Fact]
        public void Should_throw_an_EasyNetQException()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
                mockBuilder.Connection.ConnectionShutdown += Raise.EventWith(null, new ShutdownEventArgs(ShutdownInitiator.Application, 0, null));
                (mockBuilder.Connection as IAutorecoveringConnection).RecoverySucceeded += Raise.EventWith(null, new EventArgs());
                task.GetAwaiter().GetResult();
            });
        }
    }
}

// ReSharper restore InconsistentNaming
