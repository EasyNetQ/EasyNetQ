using EasyNetQ.Consumer;
using EasyNetQ.Events;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

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
            model = MockRepository.GenerateStrictMock<IModel>();
            model.Expect(m => m.BasicAck(deliveryTag, false));

            result = AckStrategies.Ack(model, deliveryTag);
        }


        [Test]
        public void Should_ack_message()
        {
            model.VerifyAllExpectations();
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
            model = MockRepository.GenerateStrictMock<IModel>();
            model.Expect(m => m.BasicNack(deliveryTag, false, false));

            result = AckStrategies.NackWithoutRequeue(model, deliveryTag);
        }


        [Test]
        public void Should_nack_message_and_not_requeue()
        {
            model.VerifyAllExpectations();   
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
            model = MockRepository.GenerateStrictMock<IModel>();
            model.Expect(m => m.BasicNack(deliveryTag, false, true));

            result = AckStrategies.NackWithRequeue(model, deliveryTag);
        }


        [Test]
        public void Should_nack_message_and_requeue()
        {
            model.VerifyAllExpectations();
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
            model = MockRepository.GenerateStrictMock<IModel>();

            result = AckStrategies.Nothing(model, deliveryTag);
        }

        [Test]
        public void Should_have_no_interaction_with_model()
        {
            model.VerifyAllExpectations();
        }

        [Test]
        public void Should_return_Nothing()
        {
            Assert.AreEqual(AckResult.Nothing, result);
        }
    }
}