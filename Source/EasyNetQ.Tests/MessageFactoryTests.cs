using System;
using Xunit;

namespace EasyNetQ.Tests
{
    public class MessageFactoryTests
    {
        [Fact]
        public void Should_correctly_create_generic_message()
        {
            var message = new MyMessage { Text = "Hello World" };

            var genericMessage = MessageFactory.CreateInstance(typeof(MyMessage), message);

            Assert.NotNull(genericMessage);
            Assert.IsType<Message<MyMessage>>(genericMessage);
            Assert.IsType<MyMessage>(genericMessage.GetBody());
            Assert.True(genericMessage.MessageType == typeof(MyMessage));
            Assert.True(((Message<MyMessage>)genericMessage).Body.Text == message.Text);

            var properties = new MessageProperties { CorrelationId = Guid.NewGuid().ToString() };
            var genericMessageWithProperties = MessageFactory.CreateInstance(typeof(MyMessage), message, properties);

            Assert.NotNull(genericMessageWithProperties);
            Assert.IsType<Message<MyMessage>>(genericMessageWithProperties);
            Assert.IsType<MyMessage>(genericMessageWithProperties.GetBody());
            Assert.True(genericMessageWithProperties.MessageType == typeof(MyMessage));
            Assert.True(((Message<MyMessage>)genericMessageWithProperties).Body.Text == message.Text);
            Assert.True(((Message<MyMessage>)genericMessageWithProperties).Properties.CorrelationId == properties.CorrelationId);
        }

        [Fact]
        public void Should_fail_to_create_generic_message_with_null_argument()
        {
            Assert.Throws<ArgumentNullException>(() => MessageFactory.CreateInstance(typeof(MyMessage), null));
            Assert.Throws<ArgumentNullException>(() => MessageFactory.CreateInstance(typeof(MyMessage), new MyMessage(), null));
        }

        [Fact]
        public void Should_support_struct_message_body()
        {
            MessageFactory.CreateInstance(typeof(Guid), Guid.NewGuid());
        }
    }
}