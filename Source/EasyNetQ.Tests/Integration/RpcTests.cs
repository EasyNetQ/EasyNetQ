using System;
using System.Threading;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class RpcTests : IDisposable
    {
        private class RpcRequest
        {
            public int Value { get; set; }
        }

        private class RpcResponse
        {
            public int Value { get; set; }
        }

        private class RpcResponseWithoutParameterlessConstructor
        {
            private readonly int _value;

            public RpcResponseWithoutParameterlessConstructor(int value)
            {
                _value = value;
            }
        }

        private IBus bus;

        public RpcTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
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

                Thread.Sleep(2000);
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

                Thread.Sleep(2000);
            });
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_reply_with_the_exception_using_classes_with_parameterless_constructor_as_message()
        {
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                bus.Respond<RpcRequest, RpcResponse>(req =>
                {
                    throw new Exception("Simulated Exception!");
                });
                var request = new RpcRequest { Value = 5 };

                var response = bus.Request<RpcRequest, RpcResponse>(request);

                Thread.Sleep(2000);
            });
            Assert.IsType<AggregateException>(ex);
            Assert.NotNull(ex.InnerException);
            Assert.Equal("Simulated Exception!", ex.InnerException.Message);
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_reply_with_the_exception_using_string_as_message()
        {
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                bus.Respond<string, string>(req =>
                {
                    throw new Exception("Simulated Exception!");
                });
                var request = "Hello";

                var response = bus.Request<string, string>(request);

                Thread.Sleep(2000);
            });
            Assert.IsType<AggregateException>(ex);
            Assert.NotNull(ex.InnerException);
            Assert.Equal("Simulated Exception!", ex.InnerException.Message);
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_reply_with_the_exception_using_classes_without_parameterless_constructor_as_message()
        {
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                bus.Respond<RpcRequest, RpcResponseWithoutParameterlessConstructor>(req =>
                {
                    throw new Exception("Simulated Exception!");
                });
                var request = new RpcRequest { Value = 5 };

                var response = bus.Request<RpcRequest, RpcResponseWithoutParameterlessConstructor>(request);

                Thread.Sleep(2000);
            });
            Assert.IsType<AggregateException>(ex);
            Assert.NotNull(ex.InnerException);
            Assert.Equal("Simulated Exception!", ex.InnerException.Message);
        }
    }
}