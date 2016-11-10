using EasyNetQ.Consumer;
using EasyNetQ.Events;
using NUnit.Framework;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class Ack_strategy
    {
        private IModel model;
        private AckResult result;
        private const ulong deliveryTag = 1234;

        [SetUp]
        public void Setup()
        {
            model = Substitute.For<IModel>();

            result = AckStrategies.Ack(model, deliveryTag);          
        }

        [Test]
        public void Should_ack_message()
        {
            model.Received().BasicAck(deliveryTag, false);
        }

        [Test]
        public void Should_return_Ack()
        {
            Assert.AreEqual(AckResult.Ack, result);
        } 
    }

    [TestFixture]
    public class NackWithoutRequeue_strategy
    {
        private IModel model;
        private AckResult result;
        private const ulong deliveryTag = 1234;

        [SetUp]
        public void Setup()
        {
            model = Substitute.For<IModel>();

            result = AckStrategies.NackWithoutRequeue(model, deliveryTag);
        }


        [Test]
        public void Should_nack_message_and_not_requeue()
        {
            model.Received().BasicNack(deliveryTag, false, false);
        }

        [Test]
        public void Should_return_Nack()
        {
            Assert.AreEqual(AckResult.Nack, result);
        }
    }

    [TestFixture]
    public class NackWithRequeue_strategy
    {
        private IModel model;
        private AckResult result;
        private const ulong deliveryTag = 1234;

        [SetUp]
        public void Setup()
        {
            model = Substitute.For<IModel>();

            result = AckStrategies.NackWithRequeue(model, deliveryTag);
        }


        [Test]
        public void Should_nack_message_and_requeue()
        {
            model.Received().BasicNack(deliveryTag, false, true);
        }

        [Test]
        public void Should_return_Nack()
        {
            Assert.AreEqual(AckResult.Nack, result);
        }
    }

    [TestFixture]
    public class Nothing_strategy
    {
        private IModel model;
        private AckResult result;
        private const ulong deliveryTag = 1234;

        [SetUp]
        public void Setup()
        {
            model = Substitute.For<IModel>();

            result = AckStrategies.Nothing(model, deliveryTag);
        }

        [Test]
        public void Should_have_no_interaction_with_model()
        {
            var rec = model.ReceivedCalls();
            Assert.AreEqual(rec.GetEnumerator().MoveNext(), false);
        }

        [Test]
        public void Should_return_Nothing()
        {
            Assert.AreEqual(AckResult.Nothing, result);
        }
    }
}