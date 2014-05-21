// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Consumer;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            ConsumerErrorStrategy.Expect(x => x.HandleConsumerCancelled(null))
                                 .IgnoreArguments()
                                 .Return(AckStrategies.Ack);

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
            ConsumerErrorStrategy.AssertWasCalled(x => x.HandleConsumerCancelled(
                Arg<ConsumerExecutionContext>.Matches(args => args.Info.ConsumerTag == ConsumerTag &&
                                                           args.Info.DeliverTag == DeliverTag &&
                                                           args.Info.Exchange == "the_exchange" &&
                                                           args.Body == OriginalBody)));

            ConsumerErrorStrategy.AssertWasNotCalled(
                x => x.HandleConsumerError(Arg<ConsumerExecutionContext>.Is.Anything, Arg<Exception>.Is.Anything));
        }

        [Test]
        public void Should_ack()
        {
            MockBuilder.Channels[0].AssertWasCalled(x => x.BasicAck(DeliverTag, false));
        }

        [Test]
        public void Should_dispose_of_the_consumer_error_strategy_when_the_bus_is_disposed()
        {
            MockBuilder.Bus.Dispose();

            ConsumerErrorStrategy.AssertWasCalled(x => x.Dispose());
        }
    }
}

// ReSharper restore InconsistentNaming