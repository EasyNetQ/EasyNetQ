﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
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
            bus.Rpc.Respond<RpcRequest, RpcResponse>(req => new RpcResponse { Value = req.Value });
            var request = new RpcRequest { Value = 5 };
            var response = bus.Rpc.Request<RpcRequest, RpcResponse>(request);

            Assert.NotNull(response);
            Assert.True(request.Value == response.Value);
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_throw_when_requesting_over_long_message()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                bus.Rpc.Respond<MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes, RpcRequest>(
                req => new RpcRequest());

                bus.Rpc.Request<MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes, RpcRequest>(
                    new MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes());
            });
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_throw_when_responding_to_over_long_message()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                bus.Rpc.Respond<RpcRequest, MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes>(
                req => new MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes());

                bus.Rpc.Request<RpcRequest, MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes>(
                    new RpcRequest());
            });
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_reply_with_the_exception_using_classes_with_parameterless_constructor_as_message()
        {
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                bus.Rpc.Respond<RpcRequest, RpcResponse>(req => Task.FromException<RpcResponse>(new Exception("Simulated Exception!")));
                var request = new RpcRequest { Value = 5 };

                var response = bus.Rpc.Request<RpcRequest, RpcResponse>(request);

                Thread.Sleep(2000);
            });
            Assert.IsType<EasyNetQResponderException>(ex);
            Assert.Equal("Simulated Exception!", ex.Message);
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_reply_with_the_exception_using_string_as_message()
        {
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                bus.Rpc.Respond<string, string>(req => Task.FromException<string>(new Exception("Simulated Exception!")));
                var request = "Hello";

                var response = bus.Rpc.Request<string, string>(request);

                Thread.Sleep(2000);
            });
            Assert.IsType<EasyNetQResponderException>(ex);
            Assert.Equal("Simulated Exception!", ex.Message);
        }

        [Fact, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_reply_with_the_exception_using_classes_without_parameterless_constructor_as_message()
        {
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                bus.Rpc.Respond<RpcRequest, RpcResponseWithoutParameterlessConstructor>(req => Task.FromException<RpcResponseWithoutParameterlessConstructor>(new Exception("Simulated Exception!")));
                var request = new RpcRequest { Value = 5 };

                var response = bus.Rpc.Request<RpcRequest, RpcResponseWithoutParameterlessConstructor>(request);

                Thread.Sleep(2000);
            });
            Assert.IsType<EasyNetQResponderException>(ex);
            Assert.Equal("Simulated Exception!", ex.Message);
        }
    }
}