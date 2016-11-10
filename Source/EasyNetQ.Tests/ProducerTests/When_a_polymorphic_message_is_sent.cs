using System.Collections.Generic;
// ReSharper disable InconsistentNaming
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class When_a_polymorphic_message_is_sent
    {
        private MockBuilder mockBuilder;
        private const string interfaceTypeName = "EasyNetQ.Tests.ProducerTests.IMyMessageInterface:EasyNetQ.Tests";
        private const string implementationTypeName = "EasyNetQ.Tests.ProducerTests.MyImplementation:EasyNetQ.Tests";
        private byte[] publishedMessage;
        private IBasicProperties properties;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            var message = new MyImplementation
                {
                    Text = "Hello Polymorphs!",
                    NotInInterface = "Hi"
                };

            mockBuilder.NextModel.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null))
               .Do(x =>
               {
                   properties = (IBasicProperties)x[3];
                   publishedMessage = (byte[])x[4];
               });

            mockBuilder.Bus.Publish<IMyMessageInterface>(message);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_name_exchange_after_interface()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is(interfaceTypeName),
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false), 
                Arg.Any<IDictionary<string, object>>());
        }

        [Test]
        public void Should_name_type_as_actual_object_type()
        {
            properties.Type.ShouldEqual(implementationTypeName);
        }

        [Test]
        public void Should_correctly_serialize_implementation()
        {
            var json = Encoding.UTF8.GetString(publishedMessage);
            json.ShouldEqual("{\"Text\":\"Hello Polymorphs!\",\"NotInInterface\":\"Hi\"}");
        }

        [Test]
        public void Should_publish_to_correct_exchange()
        {
            mockBuilder.Channels[0].Received().BasicPublish(
                    Arg.Is(interfaceTypeName),
                    Arg.Is(""),
                    Arg.Is(false), 
                    Arg.Any<IBasicProperties>(), 
                    Arg.Any<byte[]>() 
                );
        }
    }

    public interface IMyMessageInterface
    {
        string Text { get; set; }
    }

    public class MyImplementation : IMyMessageInterface
    {
        public string Text { get; set; }
        public string NotInInterface { get; set; }
    }
}

// ReSharper restore InconsistentNaming