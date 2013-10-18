// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class EventBusTests
    {
        private IEventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            eventBus = new EventBus();
        }

        [Test]
        public void Should_be_able_to_get_subscribed_to_events()
        {
            Event1 capturedEvent = null;

            eventBus.Subscribe<Event1>(@event => capturedEvent = @event);

            var publishedEvent = new Event1
                {
                    Text = "Hello World"
                };

            eventBus.Publish(publishedEvent);

            capturedEvent.ShouldNotBeNull();
            capturedEvent.ShouldBeTheSameAs(publishedEvent);
        }

        [Test]
        public void Should_not_get_events_not_subscribed_to()
        {
            Event1 capturedEvent = null;

            eventBus.Subscribe<Event1>(@event => capturedEvent = @event);

            var publishedEvent = new Event2
            {
                Text = "Hello World"
            };

            eventBus.Publish(publishedEvent);

            capturedEvent.ShouldBeNull();
        }

        private class Event1
        {
            public string Text { get; set; }
        }

        private class Event2
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming