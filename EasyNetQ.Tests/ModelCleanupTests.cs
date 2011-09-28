// ReSharper disable InconsistentNaming

using System;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ModelCleanupTests
    {
        private IBus bus;
        private MockModel model;

        [SetUp]
        public void SetUp()
        {
            model = new MockModel
            {
                BasicPublishAction = (a,b,c,d) => { }
            };
            var busFactory = new TestBusFactory();
            bus = busFactory.CreateBusWithMockAmqpClient();

            ((MockConnection) busFactory.Connection).CreateModelAction = () => {
                Console.Out.WriteLine("Creating Model");
                return model;
            };
        }

        [Test]
        public void Should_cleanup_publish_model()
        {
            var aborted = false;
            model.AbortAction = () => aborted = true;

            bus.Publish(new TestMessage());

            bus.Dispose();

            aborted.ShouldBeTrue();
        }

        [Test]
        public void Should_cleanup_subscribe_model()
        {
            var aborted = false;
            model.AbortAction = () => aborted = true;

            bus.Subscribe<TestMessage>("abc", mgs => {});
            bus.Dispose();

            aborted.ShouldBeTrue();
        }

        [Test]
        public void Should_cleanup_subscribe_async_model()
        {
            var aborted = false;
            model.AbortAction = () => aborted = true;

            bus.SubscribeAsync<TestMessage>("abc", msg => null);
            bus.Dispose();

            aborted.ShouldBeTrue();
        }

        [Test]
        public void Should_cleanup_request_response_model()
        {
            // TODO: Actually creates two IModel instances, should check that both get cleaned up

            var aborted = false;
            model.AbortAction = () => aborted = true;

            bus.Request<TestRequestMessage, TestResponseMessage>(new TestRequestMessage(), response => {});
            bus.Dispose();

            aborted.ShouldBeTrue();
        }

        [Test]
        public void Should_cleanup_respond_model()
        {
            // TODO: Implement this test
        }
    }
}

// ReSharper restore InconsistentNaming