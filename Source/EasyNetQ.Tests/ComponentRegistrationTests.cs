// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ComponentRegistrationTests
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
        }

        [Test]
        public void Should_be_able_to_decorate_the_serializer()
        {
            var bus = RabbitHutch.CreateBus("host=localhost",
                x => x
                    .Register<IConnectionFactory>(_ => mockBuilder.ConnectionFactory)
                    .Register<ISerializer>(_ => new SerializationDecorator(new JsonSerializer())));

            bus.Advanced.Serializer.ShouldBe<SerializationDecorator>();

            bus.Dispose();
        }
    }

    public class SerializationDecorator : ISerializer
    {
        private readonly ISerializer serializer;

        public SerializationDecorator(ISerializer serializer)
        {
            this.serializer = serializer;
        }

        public byte[] Serialize(object message)
        {
            var unencrypted = serializer.Serialize(message);
            return Encrypt(unencrypted);
        }

        private byte[] Encrypt(byte[] unencrypted)
        {
            // do encryption here
            return new byte[0];
        }

        public object Deserialize(Type messageType, byte[] encryptedBytes)
        {
            var unencryptedBytes = Decrypt(encryptedBytes);
            return serializer.Deserialize(messageType, unencryptedBytes);
        }

        private byte[] Decrypt(byte[] encryptedBytes)
        {
            // do dectyption here
            return new byte[0];
        }
    }
}

// ReSharper restore InconsistentNaming