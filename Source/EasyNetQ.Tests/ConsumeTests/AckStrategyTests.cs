using EasyNetQ.Consumer;
using EasyNetQ.Events;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class Ack_strategy
    {
        private IModel model;
        private AckResult result;
        private const ulong deliveryTag = 1234;

        public Ack_strategy()
        {
            model = Substitute.For<IModel>();

            result = AckStrategies.Ack(model, deliveryTag);          
        }

        [Fact]
        public void Should_ack_message()
        {
            model.Received().BasicAck(deliveryTag, false);
        }

        [Fact]
        public void Should_return_Ack()
        {
            Assert.Equal(AckResult.Ack, result);
        } 
    }

    public class NackWithoutRequeue_strategy
    {
        private IModel model;
        private AckResult result;
        private const ulong deliveryTag = 1234;

        public NackWithoutRequeue_strategy()
        {
            model = Substitute.For<IModel>();

            result = AckStrategies.NackWithoutRequeue(model, deliveryTag);
        }


        [Fact]
        public void Should_nack_message_and_not_requeue()
        {
            model.Received().BasicNack(deliveryTag, false, false);
        }

        [Fact]
        public void Should_return_Nack()
        {
            Assert.Equal(AckResult.Nack, result);
        }
    }

    public class NackWithRequeue_strategy
    {
        private IModel model;
        private AckResult result;
        private const ulong deliveryTag = 1234;

        public NackWithRequeue_strategy()
        {
            model = Substitute.For<IModel>();

            result = AckStrategies.NackWithRequeue(model, deliveryTag);
        }


        [Fact]
        public void Should_nack_message_and_requeue()
        {
            model.Received().BasicNack(deliveryTag, false, true);
        }

        [Fact]
        public void Should_return_Nack()
        {
            Assert.Equal(AckResult.Nack, result);
        }
    }
}