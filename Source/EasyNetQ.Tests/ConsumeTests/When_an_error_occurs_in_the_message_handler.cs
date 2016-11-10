// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Consumer;
using NUnit.Framework;
using NSubstitute;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_an_error_occurs_in_the_message_handler : ConsumerTestBase
    {
        private Exception exception;

        protected override void AdditionalSetUp()
        {
            ConsumerErrorStrategy.HandleConsumerError(null, null)
                     .ReturnsForAnyArgs(AckStrategies.Ack);

            exception = new Exception("I've had a bad day :(");
            StartConsumer((body, properties, info) =>
                {
                    throw exception;
                });
            DeliverMessage();
        }

        [Test]
        public void Should_write_an_error_message_to_the_log()
        {
            const string errorMessage = "Exception thrown by subscription callback.";

            MockBuilder.Logger.Received().ErrorWrite(
                Arg.Is<string>(msg => msg.StartsWith(errorMessage)),
                Arg.Any<object[]>());
        }

        [Test]
        public void Should_invoke_the_error_strategy()
        {
            ConsumerErrorStrategy.Received().HandleConsumerError(
                Arg.Is<ConsumerExecutionContext>(args => args.Info.ConsumerTag == ConsumerTag &&
                                                           args.Info.DeliverTag == DeliverTag &&
                                                           args.Info.Exchange == "the_exchange" &&
                                                           args.Body == OriginalBody),
                Arg.Is<Exception>(ex => ex.InnerException == exception));

            ConsumerErrorStrategy.DidNotReceive()
                .HandleConsumerCancelled(Arg.Any<ConsumerExecutionContext>());
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