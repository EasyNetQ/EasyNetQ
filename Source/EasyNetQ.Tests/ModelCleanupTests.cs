using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ModelCleanupTests
    {
        private IBus bus;
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
            bus = mockBuilder.Bus;
        }

        [Test]
        public void Should_cleanup_publish_model()
        {
            bus.Publish(new TestMessage());
            bus.Dispose();

            mockBuilder.Channels[0].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_subscribe_model()
        {
            bus.Subscribe<TestMessage>("abc", mgs => {});
            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            are.WaitOne();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_subscribe_async_model()
        {
            bus.SubscribeAsync<TestMessage>("abc", msg => null);
            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            are.WaitOne();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_request_response_model()
        {
            bus.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            are.WaitOne();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_respond_model()
        {
            bus.Respond<TestRequestMessage, TestResponseMessage>(x => null);
            var are = WaitForConsumerModelDisposedMessage();

            bus.Dispose();

            are.WaitOne();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }

        private AutoResetEvent WaitForConsumerModelDisposedMessage()
        {
            var are = new AutoResetEvent(false);

            mockBuilder.EventBus.Subscribe<ConsumerModelDisposedEvent>(x => are.Set());

            return are;
        }
    }
}