// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class EventBusTests
    {
        private readonly IEventBus eventBus;

        public EventBusTests()
        {
            eventBus = new EventBus();
        }

        [Fact]
        public void Should_be_able_to_get_subscribed_to_events()
        {
            Event1? capturedEvent = null;

            eventBus.Subscribe((in Event1 @event) => capturedEvent = @event);

            var publishedEvent = new Event1
            {
                Text = "Hello World"
            };

            eventBus.Publish(publishedEvent);

            capturedEvent.Should().Be(publishedEvent);
        }

        [Fact]
        public void Should_not_get_events_not_subscribed_to()
        {
            Event1? capturedEvent = null;

            eventBus.Subscribe((in Event1 @event) => capturedEvent = @event);

            var publishedEvent = new Event2
            {
                Text = "Hello World"
            };

            eventBus.Publish(publishedEvent);

            capturedEvent.Should().BeNull();
        }

        [Fact]
        public void Should_be_able_to_cancel_an_event()
        {
            var published = new List<Event1>();

            var subscription = eventBus.Subscribe((in Event1 s) => published.Add(s));
            subscription.Should().NotBeNull();

            var publishedEvent = new Event1 { Text = "Before cancellation" };
            eventBus.Publish(publishedEvent);

            subscription.Dispose();

            eventBus.Publish(new Event1 { Text = "Hello World" });

            published.Count.Should().Be(1);
            published[0].Should().Be(publishedEvent);
        }

        [Fact]
        public void Should_handle_resubscription_from_handler()
        {
            Event1? eventFromSubscription = null;

            eventBus.Subscribe((in Event1 @event) =>
                {
                    eventFromSubscription = @event;
                    eventBus.Subscribe((in Event1 _) => { });
                });

            var publishedEvent1 = new Event1
            {
                Text = "Hello World"
            };

            eventBus.Publish(publishedEvent1);

            eventFromSubscription.Should().NotBeNull();
        }

        [Fact]
        public void Should_handle_cancelation_from_handler()
        {
            Event1? eventFromSubscription = null;

            IDisposable subscription = null;

            subscription = eventBus.Subscribe((in Event1 @event) =>
            {
                subscription.Dispose();
                eventFromSubscription = @event;
            });

            var publishedEvent1 = new Event1
            {
                Text = "Hello World"
            };

            eventBus.Publish(publishedEvent1);

            eventFromSubscription.Should().NotBeNull();
        }

        private struct Event1
        {
            public string Text { get; set; }
        }

        private struct Event2
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming
