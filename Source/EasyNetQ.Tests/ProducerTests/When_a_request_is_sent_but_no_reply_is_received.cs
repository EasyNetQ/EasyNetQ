// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class When_a_request_is_sent_but_no_reply_is_received
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder("host=localhost;timeout=1");
        }

        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public void Should_throw_a_timeout_exception()
        {
            try
            {
                mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage()).Wait();
            }
            catch (AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }
        }         
    }
}

// ReSharper restore InconsistentNaming