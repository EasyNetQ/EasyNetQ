﻿// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using NSubstitute;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_request_is_sent_but_the_connection_closes_before_a_reply_is_received
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_throw_an_EasyNetQException()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                try
                {
                    var task = mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
                    mockBuilder.Connection.ConnectionShutdown += Raise.EventWith(null, new ShutdownEventArgs(new ShutdownInitiator(), 0, null));
                    task.Wait();
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