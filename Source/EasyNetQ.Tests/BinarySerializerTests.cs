// ReSharper disable InconsistentNaming
using System;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class BinarySerializerTests
    {
        private ISerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new BinarySerializer();
        }

        [Test]
        public void Should_be_able_to_serialize_and_deserialize_a_message()
        {
            var initialMessage = new TestMessage {Text = "Hello!"};
            var serializedMessage = serializer.MessageToBytes(initialMessage);
            var deserializedMessage = serializer.BytesToMessage<TestMessage>(serializedMessage);

            deserializedMessage.Text.ShouldEqual(initialMessage.Text);
        }

        [Serializable]
        private class TestMessage
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming