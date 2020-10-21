// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_request_is_sent_but_an_exception_is_thrown_by_responder : IDisposable
    {
        private readonly MockBuilder mockBuilder;
        private readonly TestRequestMessage requestMessage;
        private readonly string correlationId;

        public When_a_request_is_sent_but_an_exception_is_thrown_by_responder()
        {
            correlationId = Guid.NewGuid().ToString();
            mockBuilder = new MockBuilder(
                c => c.Register<ICorrelationIdGenerationStrategy>(
                    _ => new StaticCorrelationIdGenerationStrategy(correlationId)
                )
            );

            requestMessage = new TestRequestMessage();
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public async Task Should_throw_an_EasyNetQResponderException()
        {
            await Assert.ThrowsAsync<EasyNetQResponderException>(async () =>
            {
                var waiter = new CountdownEvent(2);

                mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(_ => waiter.Signal());
                mockBuilder.EventBus.Subscribe<StartConsumingSucceededEvent>(_ => waiter.Signal());

                var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);
                if (!waiter.Wait(5000))
                    throw new TimeoutException();

                DeliverMessage(null);
                await task;
            });
        }

        [Fact]
        public async Task Should_throw_an_EasyNetQResponderException_with_a_specific_exception_message()
        {
            await Assert.ThrowsAsync<EasyNetQResponderException>(async () =>
            {
                var waiter = new CountdownEvent(2);

                mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(_ => waiter.Signal());
                mockBuilder.EventBus.Subscribe<StartConsumingSucceededEvent>(_ => waiter.Signal());

                var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);
                if (!waiter.Wait(5000))
                    throw new TimeoutException();

                DeliverMessage("Why you are so bad with me?");

                await task;
            }); // ,"Why you are so bad with me?"
        }

        private void DeliverMessage(string exceptionMessage)
        {
            var properties = new BasicProperties
            {
                Type = "EasyNetQ.Tests.TestResponseMessage, EasyNetQ.Tests",
                CorrelationId = correlationId,
                Headers = new Dictionary<string, object>
                {
                    { "IsFaulted", true }
                }
            };

            if (exceptionMessage != null)
            {
                // strings are implicitly converted in byte[] from RabbitMQ client
                // but not converted back in string
                // check the source code in the class RabbitMQ.Client.Impl.WireFormatting
                properties.Headers.Add("ExceptionMessage", Encoding.UTF8.GetBytes(exceptionMessage));
            }

            var body = Encoding.UTF8.GetBytes("{}");

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer_tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                body
            );
        }
    }
}

// ReSharper restore InconsistentNaming
