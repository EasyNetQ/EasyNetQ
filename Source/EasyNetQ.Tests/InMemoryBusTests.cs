// ReSharper disable InconsistentNaming

using EasyNetQ.Tests.Sagas;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class InMemoryBusTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = new InMemoryBus();
        }

        [Test]
        public void SubscribePublishSaga_should_publish_when_request_is_consumed()
        {
            new SubscribePublishSaga().Initialize(bus);

            const string text = "some text";
            var requestMessage = new TestRequestMessage {Text = text};
            TestResponseMessage responseMessage = null;

            bus.Subscribe<TestResponseMessage>("id", response =>
            {
                responseMessage = response;
            });

            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(requestMessage);
            }

            Assert.IsNotNull(responseMessage);
            Assert.AreEqual(text, responseMessage.Text);
        } 

        [Test]
        public void AsyncSubscribePublishSaga_should_publish_when_request_is_consumed()
        {
            new AsyncSubscribePublishSaga().Initialize(bus);

            const string text = "some text";
            var requestMessage = new TestRequestMessage {Text = text};

            TestResponseMessage responseMessage = null;
            bus.Subscribe<TestResponseMessage>("id", response =>
            {
                responseMessage = response;
            });

            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(requestMessage);
            }

            Assert.IsNotNull(responseMessage);
            Assert.AreEqual(text, responseMessage.Text);
        }

        [Test]
        public void RequestResponseSaga_should_make_a_request_get_the_response_and_end()
        {
            new RequestResponseSaga().Initialize(bus);

            const string text = "some text";
            var startMessage = new StartMessage {Text = text};

            // imitate the responding service
            bus.Respond<TestRequestMessage, TestResponseMessage>(request => 
                new TestResponseMessage { Text = request.Text });

            // set up the subscription for the end message
            EndMessage endMessage = null;
            bus.Subscribe<EndMessage>("id", end =>
            {
                endMessage = end;
            });

            // now publish the start message to kick the saga off
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(startMessage);
            }

            // assert that the end message was assigned and has the correct text
            Assert.IsNotNull(endMessage);
            Assert.AreEqual(text, endMessage.Text);
        }
    }
}

// ReSharper restore InconsistentNaming