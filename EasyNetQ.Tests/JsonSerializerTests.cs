// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class JsonSerializerTests
    {
        private ISerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new JsonSerializer();
        }

        [Test]
        public void Should_be_able_to_serialize_and_deserialize_a_message()
        {
            var message = new MyMessage {Text = "Hello World"};

            var binaryMessage = serializer.MessageToBytes(message);
            var deseralizedMessage = serializer.BytesToMessage<MyMessage>(binaryMessage);

            message.Text.ShouldEqual(deseralizedMessage.Text);
        } 
    }
}

// ReSharper restore InconsistentNaming