// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_an_error_occurs_in_the_message_handler : ConsumerTestBase
    {
        private Exception exception;

        protected override void AdditionalSetUp()
        {
            ConsumerErrorStrategy.HandleConsumerErrorAsync(default, null)
                     .ReturnsForAnyArgs(Task.FromResult(AckStrategies.Ack));

            exception = new Exception("I've had a bad day :(");
            StartConsumer((body, properties, info) => throw exception);
            DeliverMessage();
        }

        [Fact]
        public async Task Should_invoke_the_error_strategy()
        {
            await ConsumerErrorStrategy.Received().HandleConsumerErrorAsync(
                Arg.Is<ConsumerExecutionContext>(args => args.ReceivedInfo.ConsumerTag == ConsumerTag &&
                                                           args.ReceivedInfo.DeliveryTag == DeliverTag &&
                                                           args.ReceivedInfo.Exchange == "the_exchange" &&
                                                           args.Body.ToArray().SequenceEqual(OriginalBody)),
                Arg.Is<Exception>(e => e == exception),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public void Should_ack()
        {
            MockBuilder.Channels[0].Received().BasicAck(DeliverTag, false);
        }

        [Fact]
        public void Should_dispose_of_the_consumer_error_strategy_when_the_bus_is_disposed()
        {
            MockBuilder.Dispose();

            ConsumerErrorStrategy.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
