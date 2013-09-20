// ReSharper disable InconsistentNaming

using System.Collections;
using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.RequestResponseTests
{
    [TestFixture]
    public class When_a_request_is_made : RequestResponseTestBase
    {
        protected override void AdditionalSetup()
        {
            MakeRequest();
        }

        [Test]
        public void Should_declare_request_exchange()
        {
            mockBuilder.Channels[3].AssertWasCalled(x => x.ExchangeDeclare(
                Arg<string>.Is.Equal("rpc_exchange"),
                Arg<string>.Is.Equal("direct"),
                Arg<bool>.Is.Equal(true),
                Arg<bool>.Is.Equal(false),
                Arg<IDictionary>.Is.Anything));
        }

        [Test]
        public void Should_declare_respond_queue()
        {
            mockBuilder.Channels[1].AssertWasCalled(x => x.QueueDeclare(
                Arg<string>.Is.Equal("rpc_return_queue"),
                Arg<bool>.Is.Equal(false),  // durable
                Arg<bool>.Is.Equal(true),   // exclusive
                Arg<bool>.Is.Equal(true),   // autoDelete
                Arg<IDictionary>.Is.Anything));
        }

        [Test]
        public void Should_start_a_consumer_on_the_return_queue()
        {
            mockBuilder.Channels[2].AssertWasCalled(x => x.BasicConsume(
                Arg<string>.Is.Equal("rpc_return_queue"),
                Arg<bool>.Is.Equal(false),                  // NoAck
                Arg<string>.Is.Equal("the_consumer_tag"),   // consumer tag
                Arg<IBasicConsumer>.Is.Anything
                ));
        }

        [Test]
        public void Should_publish_the_request()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.BasicPublish(
                Arg<string>.Is.Equal("rpc_exchange"),
                Arg<string>.Is.Equal("EasyNetQ_Tests_MyMessage:EasyNetQ_Tests"),
                Arg<IBasicProperties>.Is.Anything,
                Arg<byte[]>.Matches(body => "{\"Text\":\"Hello World!\"}" == Encoding.UTF8.GetString(body))
                ));
        }
    }
}

// ReSharper restore InconsistentNaming