// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.Mocking;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_request_is_sent_but_no_reply_is_received : IDisposable
    {
        private MockBuilder mockBuilder;

        public When_a_request_is_sent_but_no_reply_is_received()
        {
            mockBuilder = new MockBuilder("host=localhost;timeout=1");
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_throw_a_timeout_exception()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                try
                {
                    mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage()).Wait();
                }
                catch (AggregateException aggregateException)
                {
                    throw aggregateException.InnerException;
                }
            });
        }         
    }
}

// ReSharper restore InconsistentNaming