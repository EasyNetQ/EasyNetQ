// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Consumer;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_an_error_occurs_in_the_message_handler : ConsumerTestBase
    {
        private Exception exception;

        protected override void AdditionalSetUp()
        {
            ConsumerErrorStrategy.Expect(x => x.HandleConsumerError(null, null))
                     .IgnoreArguments()
                     .Return(AckStrategies.Ack);

            exception = new ApplicationException("I've had a bad day :(");
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

            MockBuilder.Logger.AssertWasCalled(x => x.ErrorWrite(
                Arg<string>.Matches(msg => msg.StartsWith(errorMessage)),
                Arg<object[]>.Is.Anything));
        }

        [Test]
        public void Should_invoke_the_error_strategy()
        {
            ConsumerErrorStrategy.AssertWasCalled(x => x.HandleConsumerError(
                Arg<ConsumerExecutionContext>.Matches(args => args.Info.ConsumerTag == ConsumerTag &&
                                                           args.Info.DeliverTag == DeliverTag &&
                                                           args.Info.Exchange == "the_exchange" &&
                                                           args.Body == OriginalBody),
                Arg<Exception>.Matches(ex => ex.InnerException == exception)));

            ConsumerErrorStrategy.AssertWasNotCalled(
                x => x.HandleConsumerCancelled(Arg<ConsumerExecutionContext>.Is.Anything));
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