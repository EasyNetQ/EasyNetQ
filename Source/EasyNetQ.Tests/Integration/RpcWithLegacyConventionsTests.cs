using EasyNetQ.Logging;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
using System;
using System.Threading;
using Xunit;

namespace EasyNetQ.Tests.Integration.LegacyConventionsTests
{
    public class RpcWithLegacyConventionsTests : IDisposable
    {
        private class RpcRequest
        {
            public int Value { get; set; }
        }

        private class RpcResponse
        {
            public int Value { get; set; }
        }

        private IBus bus;

        public RpcWithLegacyConventionsTests()
        {
            LogProvider.SetCurrentLogProvider(ConsoleLogProvider.Instance);

            bus = RabbitHutch.CreateBus("host=localhost", x => x.EnableLegacyConventions());
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_be_able_to_publish_and_receive_response()
        {
            bus.Respond<RpcRequest, RpcResponse>(req => new RpcResponse { Value = req.Value });
            var request = new RpcRequest { Value = 5 };
            var response = bus.Request<RpcRequest, RpcResponse>(request);

            Assert.NotNull(response);
            Assert.True(request.Value == response.Value);
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_throw_when_requesting_over_long_message()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                bus.Respond<MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes, RpcRequest>(
                    req => new RpcRequest());

                bus.Request<MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes, RpcRequest>(
                   new MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes());
            });
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_throw_when_responding_to_over_long_message()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                bus.Respond<RpcRequest, MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes>(
                    req => new MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes());

                bus.Request<RpcRequest, MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes>(
                   new RpcRequest());
            });
        }
    }
}
