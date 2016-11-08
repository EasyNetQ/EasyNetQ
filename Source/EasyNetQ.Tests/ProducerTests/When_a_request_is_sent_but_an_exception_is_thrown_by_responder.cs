﻿using System.Collections.Generic;
using RabbitMQ.Client.Framing;
// ReSharper disable InconsistentNaming
using RabbitMQ.Client;
using NSubstitute;
using System;
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class When_a_request_is_sent_but_an_exception_is_thrown_by_responder
    {
        private MockBuilder mockBuilder;
        private TestRequestMessage requestMessage;
        private string _correlationId;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            requestMessage = new TestRequestMessage();

            mockBuilder.NextModel.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null))
                       .Do(invocation =>
                       {
                           var properties = (IBasicProperties)invocation[3];
                           _correlationId = properties.CorrelationId;
                       });
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_throw_an_EasyNetQResponderException()
        {
            Assert.Throws<EasyNetQResponderException>(() =>
            {
                try
                {
                    var task = mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);
                    DeliverMessage(_correlationId, null);
                    task.Wait(1000);
                }
                catch (AggregateException aggregateException)
                {
                    throw aggregateException.InnerException;
                }
            });
        }

        [Test]
        public void Should_throw_an_EasyNetQResponderException_with_a_specific_exception_message()
        {
            Assert.Throws<EasyNetQResponderException>(() =>
            {

                try
                {
                    var task = mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);
                    DeliverMessage(_correlationId, "Why you are so bad with me?");
                    task.Wait(1000);
                }
                catch (AggregateException aggregateException)
                {
                    throw aggregateException.InnerException;
                }
            },"Why you are so bad with me?");
        }

        protected void DeliverMessage(string correlationId, string exceptionMessage)
        {
            var properties = new BasicProperties
            {
                Type = "EasyNetQ.Tests.TestResponseMessage:EasyNetQ.Tests.Messages",
                CorrelationId = correlationId,
                Headers = new Dictionary<string, object>
                {
                    { "IsFaulted", true }
                }
            };

            if (exceptionMessage != null)
            {
                // strings are implicitly convertered in byte[] from RabbitMQ client
                // but not convertered back in string
                // check the source code in the class RabbitMQ.Client.Impl.WireFormatting
                properties.Headers.Add("ExceptionMessage", Encoding.UTF8.GetBytes(exceptionMessage));
            }

            var body = Encoding.UTF8.GetBytes("{}");

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer_tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                body
                );
        }
    }
}

// ReSharper restore InconsistentNaming