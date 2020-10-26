// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using NSubstitute;
using System.Linq;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            ConsumerErrorStrategy.HandleConsumerCancelled(default)
                                 .ReturnsForAnyArgs(AckStrategies.Ack);

            StartConsumer((body, properties, info) =>
                {
                    Cancellation.Cancel();
                    Cancellation.Token.ThrowIfCancellationRequested();
                    return AckStrategies.Ack;
                });
            DeliverMessage();
        }

        [Fact]
        public void Should_invoke_the_cancellation_strategy()
        {
            ConsumerErrorStrategy.Received().HandleConsumerCancelled(
               Arg.Is<ConsumerExecutionContext>(args => args.ReceivedInfo.ConsumerTag == ConsumerTag &&
                                                        args.ReceivedInfo.DeliveryTag == DeliverTag &&
                                                        args.ReceivedInfo.Exchange == "the_exchange" &&
                                                        args.Body.SequenceEqual(OriginalBody)));
        }

        [Fact]
        public void Should_ack()
        {
            MockBuilder.Channels[0].Received().BasicAck(DeliverTag, false);
        }

        [Fact]
        public void Should_dispose_of_the_consumer_error_strategy_when_the_bus_is_disposed()
        {
            MockBuilder.Bus.Dispose();

            ConsumerErrorStrategy.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
