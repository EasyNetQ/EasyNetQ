using System.Collections.Generic;
// ReSharper disable InconsistentNaming
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

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

            mockBuilder.NextModel.Stub(x => x.BasicPublish(null, null, false, false, null, null))
                .IgnoreArguments()
                .WhenCalled(x =>
                    {
                        properties = (IBasicProperties) x.Arguments[4];
                        publishedMessage = (byte[]) x.Arguments[5];
                    });

            mockBuilder.Bus.Publish<IMyMessageInterface>(message);
        }

        [Test]
        public void Should_name_exchange_after_interface()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.ExchangeDeclare(
                Arg<string>.Is.Equal(interfaceTypeName), 
                Arg<string>.Is.Equal("topic"), 
                Arg<bool>.Is.Equal(true), 
                Arg<bool>.Is.Equal(false), 
                Arg<IDictionary<string, object>>.Is.Anything));
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
            mockBuilder.Channels[0].AssertWasCalled(x => x.BasicPublish(
                    Arg<string>.Is.Equal(interfaceTypeName), 
                    Arg<string>.Is.Equal(""), 
                    Arg<bool>.Is.Equal(false), 
                    Arg<bool>.Is.Equal(false), 
                    Arg<IBasicProperties>.Is.Anything, 
                    Arg<byte[]>.Is.Anything 
                ));
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