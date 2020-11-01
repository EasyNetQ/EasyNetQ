using System;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests
{
    public class ModelCleanupTests
    {
        public ModelCleanupTests()
        {
            mockBuilder = new MockBuilder();
            bus = mockBuilder.Bus;
            waitTime = TimeSpan.FromSeconds(10);
        }

        private readonly IBus bus;
        private readonly MockBuilder mockBuilder;
        private readonly TimeSpan waitTime;

        private AutoResetEvent WaitForConsumerModelDisposedMessage()
        {
            var are = new AutoResetEvent(false);

            mockBuilder.EventBus.Subscribe<ConsumerModelDisposedEvent>(x => are.Set());

            return are;
        }

        [Fact]
        public void Should_cleanup_publish_model()
        {
            bus.PubSub.Publish(new TestMessage());
            bus.Dispose();

            mockBuilder.Channels[0].Received().Dispose();
        }

        [Fact]
        public void Should_cleanup_request_response_model()
        {
            var waiter = new CountdownEvent(2);

            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(_ => waiter.Signal());
            mockBuilder.EventBus.Subscribe<StartConsumingSucceededEvent>(_ => waiter.Signal());

            bus.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
            if (!waiter.Wait(5000))
                throw new TimeoutException();

            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            var signalReceived = are.WaitOne(waitTime);
            Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

            mockBuilder.Channels[0].Received().Dispose();
            mockBuilder.Channels[1].Received().Dispose();
        }

        [Fact]
        public void Should_cleanup_respond_model()
        {
            bus.Rpc.Respond<TestRequestMessage, TestResponseMessage>(x => (TestResponseMessage) null);
            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            var signalReceived = are.WaitOne(waitTime);
            Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

            mockBuilder.Channels[0].Received().Dispose();
            mockBuilder.Channels[1].Received().Dispose();
        }

        [Fact]
        public void Should_cleanup_subscribe_async_model()
        {
            bus.PubSub.Subscribe<TestMessage>("abc", msg => { });
            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            var signalReceived = are.WaitOne(waitTime);
            Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

            mockBuilder.Channels[0].Received().Dispose();
            mockBuilder.Channels[1].Received().Dispose();
        }

        [Fact]
        public void Should_cleanup_subscribe_model()
        {
            bus.PubSub.Subscribe<TestMessage>("abc", mgs => { });
            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            var signalReceived = are.WaitOne(waitTime);
            Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

            mockBuilder.Channels[0].Received().Dispose();
            mockBuilder.Channels[1].Received().Dispose();
        }
    }
}
