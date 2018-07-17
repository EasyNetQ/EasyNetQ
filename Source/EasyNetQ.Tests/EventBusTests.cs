// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class EventBusTests
    {
        private IEventBus eventBus;

        public EventBusTests()
        {
            eventBus = new EventBus();
        }

        [Fact]
        public void Should_be_able_to_get_subscribed_to_events()
        {
            Event1 capturedEvent = null;

            eventBus.Subscribe<Event1>(@event => capturedEvent = @event);

            var publishedEvent = new Event1
                {
                    Text = "Hello World"
                };

            eventBus.Publish(publishedEvent);

            capturedEvent.Should().NotBeNull();
            capturedEvent.Should().BeSameAs(publishedEvent);
        }

        [Fact]
        public void Should_not_get_events_not_subscribed_to()
        {
            Event1 capturedEvent = null;

            eventBus.Subscribe<Event1>(@event => capturedEvent = @event);

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
            var stringsPublished = new List<string>();

            var subscription = eventBus.Subscribe<string>(stringsPublished.Add);
            subscription.Should().NotBeNull();

            eventBus.Publish("Before cancellation");

            subscription.Dispose();

            eventBus.Publish("Hello World");

            stringsPublished.Count.Should().Be(1);
            stringsPublished[0].Should().Be("Before cancellation");
        }

        [Fact]
        public void Should_handle_resubscription_from_handler()
        {
            Event1 eventFromSubscription = null;

            eventBus.Subscribe<Event1>(@event =>
                {
                    eventFromSubscription = @event;
                    eventBus.Subscribe<Event1>(x => { });
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
            Event1 eventFromSubscription = null;

            IDisposable subscription = null;

            subscription = eventBus.Subscribe<Event1>(@event =>
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