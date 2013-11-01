// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class When_a_request_is_sent_but_the_connection_closes_before_a_reply_is_received
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
        }

        [Test]
        [ExpectedException(typeof(EasyNetQException))]
        public void Should_throw_an_EasyNetQException()
        {
            try
            {
                var task = mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
                mockBuilder.Connection.Raise(x => x.ConnectionShutdown += null, null, null);
                task.Wait();
            }
            catch (AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }
        }         
    }
}

// ReSharper restore InconsistentNaming