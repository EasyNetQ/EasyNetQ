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
        private readonly IBus bus;
        private readonly MockBuilder mockBuilder;
        private readonly TimeSpan waitTime;

        public ModelCleanupTests()
        {
            mockBuilder = new MockBuilder();
            bus = mockBuilder.Bus;
            waitTime = TimeSpan.FromSeconds(10);
        }

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
            mockBuilder.Dispose();

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

            mockBuilder.Dispose();

            var signalReceived = are.WaitOne(waitTime);
            Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

            mockBuilder.Channels[0].Received().Dispose();
            mockBuilder.Channels[1].Received().Dispose();
        }

        [Fact]
        public void Should_cleanup_respond_model()
        {
            var waiter = new CountdownEvent(1);
            mockBuilder.EventBus.Subscribe<StartConsumingSucceededEvent>(_ => waiter.Signal());

            bus.Rpc.Respond<TestRequestMessage, TestResponseMessage>(x => (TestResponseMessage)null);
            if (!waiter.Wait(5000))
                throw new TimeoutException();

            var are = WaitForConsumerModelDisposedMessage();

            mockBuilder.Dispose();

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

            mockBuilder.Dispose();

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

            mockBuilder.Dispose();

            var signalReceived = are.WaitOne(waitTime);
            Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

            mockBuilder.Channels[0].Received().Dispose();
            mockBuilder.Channels[1].Received().Dispose();
        }
    }
}
