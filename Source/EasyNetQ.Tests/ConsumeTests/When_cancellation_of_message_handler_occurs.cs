// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Consumer;
using NUnit.Framework;
using NSubstitute;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            ConsumerErrorStrategy.HandleConsumerCancelled(null)
                                 .ReturnsForAnyArgs(AckStrategies.Ack);

            StartConsumer((body, properties, info) =>
                {
                    Cancellation.Cancel();
                    Cancellation.Token.ThrowIfCancellationRequested();
                });
            DeliverMessage();
        }

        [Test]
        public void Should_invoke_the_cancellation_strategy()
        {
            ConsumerErrorStrategy.Received().HandleConsumerCancelled(
               Arg.Is<ConsumerExecutionContext>(args => args.Info.ConsumerTag == ConsumerTag &&
                                                        args.Info.DeliverTag == DeliverTag &&
                                                        args.Info.Exchange == "the_exchange" &&
                                                        args.Body == OriginalBody));

            ConsumerErrorStrategy.DidNotReceive().HandleConsumerError(Arg.Any<ConsumerExecutionContext>(), Arg.Any<Exception>());
        }

        [Test]
        public void Should_ack()
        {
            MockBuilder.Channels[0].Received().BasicAck(DeliverTag, false);
        }

        [Test]
        public void Should_dispose_of_the_consumer_error_strategy_when_the_bus_is_disposed()
        {
            MockBuilder.Bus.Dispose();

            ConsumerErrorStrategy.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming