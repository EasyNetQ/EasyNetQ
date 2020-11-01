using EasyNetQ.Consumer;
using EasyNetQ.Events;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class Ack_strategy
    {
        public Ack_strategy()
        {
            model = Substitute.For<IModel, IRecoverable>();

            result = AckStrategies.Ack(model, deliveryTag);
        }

        private readonly IModel model;
        private readonly AckResult result;
        private const ulong deliveryTag = 1234;

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
        public NackWithoutRequeue_strategy()
        {
            model = Substitute.For<IModel, IRecoverable>();

            result = AckStrategies.NackWithoutRequeue(model, deliveryTag);
        }

        private readonly IModel model;
        private readonly AckResult result;
        private const ulong deliveryTag = 1234;

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
        public NackWithRequeue_strategy()
        {
            model = Substitute.For<IModel, IRecoverable>();

            result = AckStrategies.NackWithRequeue(model, deliveryTag);
        }

        private readonly IModel model;
        private readonly AckResult result;
        private const ulong deliveryTag = 1234;

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
