using System;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class MessageFactoryTests
    {
        [Test]
        public void Should_correctly_create_generic_message()
        {
            var message = new MyMessage { Text = "Hello World" };

            var genericMessage = MessageFactory.CreateInstance(typeof(MyMessage), message);

            Assert.IsNotNull(genericMessage);
            Assert.IsInstanceOf<Message<MyMessage>>(genericMessage);
            Assert.IsInstanceOf<MyMessage>(genericMessage.GetBody());
            Assert.IsTrue(genericMessage.MessageType == typeof(MyMessage));
            Assert.IsTrue(genericMessage.CastTo<Message<MyMessage>>().Body.Text == message.Text);

            var properties = new MessageProperties { CorrelationId = Guid.NewGuid().ToString() };
            var genericMessageWithProperties = MessageFactory.CreateInstance(typeof(MyMessage), message, properties);

            Assert.IsNotNull(genericMessageWithProperties);
            Assert.IsInstanceOf<Message<MyMessage>>(genericMessageWithProperties);
            Assert.IsInstanceOf<MyMessage>(genericMessageWithProperties.GetBody());
            Assert.IsTrue(genericMessageWithProperties.MessageType == typeof(MyMessage));
            Assert.IsTrue(genericMessageWithProperties.CastTo<Message<MyMessage>>().Body.Text == message.Text);
            Assert.IsTrue(genericMessageWithProperties.CastTo<Message<MyMessage>>().Properties.CorrelationId == properties.CorrelationId);
        }

        [Test]
        public void Should_fail_to_create_generic_message_with_null_argument()
        {
            Assert.Throws<ArgumentNullException>(() => MessageFactory.CreateInstance(typeof(MyMessage), null));
            Assert.Throws<ArgumentNullException>(() => MessageFactory.CreateInstance(typeof(MyMessage), new MyMessage(), null));
        }
    }
}