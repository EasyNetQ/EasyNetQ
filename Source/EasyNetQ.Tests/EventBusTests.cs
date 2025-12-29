using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Tests;

public class EventBusTests
{
    private readonly IEventBus eventBus = new EventBus(Substitute.For<ILogger<EventBus>>());

    [Fact]
    public async Task Should_be_able_to_get_subscribed_to_events()
    {
        Event1? capturedEvent = null;

#pragma warning disable IDISP004
        eventBus.Subscribe((Event1 @event) => { capturedEvent = @event; return Task.CompletedTask; });
#pragma warning restore IDISP004

        var publishedEvent = new Event1
        {
            Text = "Hello World"
        };

        await eventBus.PublishAsync(publishedEvent);

        capturedEvent.Should().Be(publishedEvent);
    }

    [Fact]
    public async Task Should_not_get_events_not_subscribed_to()
    {
        Event1? capturedEvent = null;

#pragma warning disable IDISP004
        eventBus.Subscribe((Event1 @event) => { capturedEvent = @event; return Task.CompletedTask; });
#pragma warning restore IDISP004

        var publishedEvent = new Event2
        {
            Text = "Hello World"
        };

        await eventBus.PublishAsync(publishedEvent);

        capturedEvent.Should().BeNull();
    }

    [Fact]
    public async Task Should_be_able_to_cancel_an_event()
    {
        var published = new List<Event1>();

        var publishedEvent = new Event1 { Text = "Before cancellation" };

        {
            using var subscription = eventBus.Subscribe((Event1 s) => { published.Add(s); return Task.CompletedTask; });
            subscription.Should().NotBeNull();

            await eventBus.PublishAsync(publishedEvent);
        }

        await eventBus.PublishAsync(new Event1 { Text = "Hello World" });

        published.Count.Should().Be(1);
        published[0].Should().Be(publishedEvent);
    }

    [Fact]
    public async Task Should_handle_resubscription_from_handler()
    {
        Event1? eventFromSubscription = null;

#pragma warning disable IDISP004
        eventBus.Subscribe((Event1 @event) =>
#pragma warning restore IDISP004
        {
            eventFromSubscription = @event;
#pragma warning disable IDISP004
            eventBus.Subscribe((Event1 _) => { return Task.CompletedTask; });
            return Task.CompletedTask;
#pragma warning restore IDISP004
        });

        var publishedEvent1 = new Event1
        {
            Text = "Hello World"
        };

        await eventBus.PublishAsync(publishedEvent1);

        eventFromSubscription.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_handle_cancelation_from_handler()
    {
        Event1? eventFromSubscription = null;

        IDisposable subscription = null;

        subscription = eventBus.Subscribe((Event1 @event) =>
        {
            subscription.Dispose();
            eventFromSubscription = @event;
            return Task.CompletedTask;
        });

        var publishedEvent1 = new Event1
        {
            Text = "Hello World"
        };

        await eventBus.PublishAsync(publishedEvent1);

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
