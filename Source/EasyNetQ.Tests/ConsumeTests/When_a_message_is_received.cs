using RabbitMQ.Client.Framing;
// ReSharper disable InconsistentNaming
using System.Text;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_a_message_is_received
    {
        private MockBuilder mockBuilder;
        private MyMessage deliveredMyMessage;
        private MyOtherMessage deliveredMyOtherMessage;

        [SetUp]
        public void SetUp()
        {
            //mockBuilder = new MockBuilder(x => x.Register<IEasyNetQLogger, ConsoleLogger>());
            mockBuilder = new MockBuilder();

            mockBuilder.Bus.Receive("the_queue", x => x
                .Add<MyMessage>(message => deliveredMyMessage = message)
                .Add<MyOtherMessage>(message => deliveredMyOtherMessage = message));

            DeliverMessage("{ Text: \"Hello World :)\" }", "EasyNetQ.Tests.MyMessage:EasyNetQ.Tests");
            DeliverMessage("{ Text: \"Goodbye Cruel World!\" }", "EasyNetQ.Tests.MyOtherMessage:EasyNetQ.Tests");
            DeliverMessage("{ Text: \"Shoudn't get this\" }", "EasyNetQ.Tests.Unknown:EasyNetQ.Tests");
        }

        [Test]
        public void Should_deliver_MyMessage()
        {
            deliveredMyMessage.ShouldNotBeNull();
            deliveredMyMessage.Text.ShouldEqual("Hello World :)");
        }

        [Test]
        public void Should_deliver_MyOtherMessage()
        {
            deliveredMyOtherMessage.ShouldNotBeNull();
            deliveredMyOtherMessage.Text.ShouldEqual("Goodbye Cruel World!");
        }

        [Test]
        public void Should_put_unrecognised_message_on_error_queue()
        {
            mockBuilder.Logger.AssertWasCalled(x => x.ErrorWrite(
                Arg<string>.Matches(errorMessage => errorMessage.StartsWith("Exception thrown by subscription callback")), 
                Arg<object[]>.Is.Anything));
        }

        private void DeliverMessage(string message, string type)
        {
            var properties = new BasicProperties
            {
                Type = type,
                CorrelationId = "the_correlation_id"
            };
            var body = Encoding.UTF8.GetBytes(message);

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                body
                );

            WaitForMessageDispatchToComplete();
        }

        private void WaitForMessageDispatchToComplete()
        {
            // wait for the subscription thread to handle the message ...
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }        
    }
}

// ReSharper restore InconsistentNaming