// ReSharper disable InconsistentNaming

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
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(new TestMessage());
            }

            mockBuilder.Channel.AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_cleanup_subscribe_model()
        {
            bus.Subscribe<TestMessage>("abc", mgs => {});
            bus.Dispose();

            mockBuilder.Channel.AssertWasCalled(x => x.Close());
        }

        [Test]
        public void Should_cleanup_subscribe_async_model()
        {
            bus.SubscribeAsync<TestMessage>("abc", msg => null);
            bus.Dispose();

            mockBuilder.Channel.AssertWasCalled(x => x.Close());
        }

        [Test]
        public void Should_cleanup_request_response_model()
        {
            // TODO: Actually creates two IModel instances, should check that both get cleaned up

            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Request<TestRequestMessage, TestResponseMessage>(new TestRequestMessage(), response => { });
            }
            bus.Dispose();

            mockBuilder.Channel.AssertWasCalled(x => x.Close());
        }

        [Test]
        public void Should_cleanup_respond_model()
        {
            bus.Respond<TestRequestMessage, TestResponseMessage>(x => null);
            bus.Dispose();

            mockBuilder.Channel.AssertWasCalled(x => x.Close());
        }
    }
}

// ReSharper restore InconsistentNaming