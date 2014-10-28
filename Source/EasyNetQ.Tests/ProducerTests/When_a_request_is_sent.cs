using System.Collections.Generic;
using RabbitMQ.Client.Framing;
// ReSharper disable InconsistentNaming
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class When_a_request_is_sent
    {
        private MockBuilder mockBuilder;
        private TestRequestMessage requestMessage;
        private TestResponseMessage responseMessage;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            requestMessage = new TestRequestMessage();
            responseMessage = new TestResponseMessage();

            var correlationId = "";

            mockBuilder.NextModel.Stub(x => x.BasicPublish(null, null, false, false, null, null))
                .IgnoreArguments()
                .WhenCalled(invocation =>
                    {
                        var properties = (IBasicProperties)invocation.Arguments[4];
                        correlationId = properties.CorrelationId;
                    });

            var task = mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);

            DeliverMessage(correlationId);

            task.Wait();

            responseMessage = task.Result;
        }

        [Test]
        public void Should_return_the_response()
        {
            responseMessage.Text.ShouldEqual("Hello World");
        }

        [Test]
        public void Should_publish_request_message()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.BasicPublish(
                Arg<string>.Is.Equal("easy_net_q_rpc"),
                Arg<string>.Is.Equal("EasyNetQ.Tests.TestRequestMessage:EasyNetQ.Tests.Messages"),
                Arg<bool>.Is.Equal(false),
                Arg<bool>.Is.Equal(false),
                Arg<IBasicProperties>.Is.Anything,
                Arg<byte[]>.Is.Anything));
        }

        [Test]
        public void Should_declare_the_publish_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.ExchangeDeclare(
                Arg<string>.Is.Equal("easy_net_q_rpc"), 
                Arg<string>.Is.Equal("direct"),
                Arg<bool>.Is.Equal(true),
                Arg<bool>.Is.Equal(false),
                Arg<IDictionary<string, object>>.Is.Anything));
        }

        [Test]
        public void Should_declare_the_response_queue()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.QueueDeclare(
                Arg<string>.Matches(arg => arg.StartsWith("easynetq.response.")),
                Arg<bool>.Is.Equal(false),
                Arg<bool>.Is.Equal(true),
                Arg<bool>.Is.Equal(true),
                Arg<IDictionary<string, object>>.Is.Anything));
        }

        protected void DeliverMessage(string correlationId)
        {
            var properties = new BasicProperties
            {
                Type = "EasyNetQ.Tests.TestResponseMessage:EasyNetQ.Tests.Messages",
                CorrelationId = correlationId
            };
            var body = Encoding.UTF8.GetBytes("{ Id:12, Text:\"Hello World\"}");

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