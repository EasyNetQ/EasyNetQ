using System.Collections.Generic;
using RabbitMQ.Client.Framing;
// ReSharper disable InconsistentNaming
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using NSubstitute;

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

            mockBuilder.NextModel.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null))
                .Do(invocation =>
                    {
                        var properties = (IBasicProperties)invocation[3];
                        correlationId = properties.CorrelationId;
                    });

            var task = mockBuilder.Bus.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);

            DeliverMessage(correlationId);

            task.Wait();

            responseMessage = task.Result;
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_return_the_response()
        {
            responseMessage.Text.ShouldEqual("Hello World");
        }

        [Test]
        public void Should_publish_request_message()
        {
            mockBuilder.Channels[0].Received().BasicPublish(
                Arg.Is("easy_net_q_rpc"),
                Arg.Is("EasyNetQ.Tests.TestRequestMessage:EasyNetQ.Tests.Messages"),
                Arg.Is(false),
                Arg.Any<IBasicProperties>(),
                Arg.Any<byte[]>());
        }

        [Test]
        public void Should_declare_the_publish_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("easy_net_q_rpc"),
                Arg.Is("direct"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Any<IDictionary<string, object>>());
        }

        [Test]
        public void Should_declare_the_response_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                Arg.Is<string>(arg => arg.StartsWith("easynetq.response.")),
                Arg.Is(false),
                Arg.Is(true),
                Arg.Is(true),
                Arg.Any<IDictionary<string, object>>());
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