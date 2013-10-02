// ReSharper disable InconsistentNaming

using System;
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
            bus.Dispose();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_subscribe_async_model()
        {
            bus.SubscribeAsync<TestMessage>("abc", msg => null);
            bus.Dispose();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_request_response_model()
        {
            bus.Request<TestRequestMessage, TestResponseMessage>(new TestRequestMessage(), response => { });
            bus.Dispose();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_respond_model()
        {
            bus.Respond<TestRequestMessage, TestResponseMessage>(x => null);
            bus.Dispose();

            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }
    }
}

// ReSharper restore InconsistentNaming