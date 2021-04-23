// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Consumer;
using NSubstitute;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            ConsumerErrorStrategy.HandleConsumerCancelledAsync(default)
                                 .ReturnsForAnyArgs(Task.FromResult(AckStrategies.Ack));

            StartConsumer((body, properties, info) =>
                {
                    Cancellation.Cancel();
                    Cancellation.Token.ThrowIfCancellationRequested();
                    return AckStrategies.Ack;
                });
            DeliverMessage();
        }

        [Fact]
        public async Task Should_invoke_the_cancellation_strategy()
        {
            await ConsumerErrorStrategy.Received().HandleConsumerCancelledAsync(
               Arg.Is<ConsumerExecutionContext>(args => args.ReceivedInfo.ConsumerTag == ConsumerTag &&
                                                        args.ReceivedInfo.DeliveryTag == DeliverTag &&
                                                        args.ReceivedInfo.Exchange == "the_exchange" &&
                                                        args.Body.ToArray().SequenceEqual(OriginalBody)),
               Arg.Any<CancellationToken>());
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
