using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using EasyNetQ.Logging;

namespace EasyNetQ;

/// <inheritdoc />
public delegate void TEventHandler<TEvent>(in TEvent @event) where TEvent : struct;

/// <summary>
///     An internal pub-sub bus to distribute events within EasyNetQ
/// </summary>
public interface IEventBus
{
    /// <summary>
    ///     Publishes the event
    /// </summary>
    /// <param name="event">The event</param>
    /// <typeparam name="TEvent">The event type</typeparam>
    void Publish<TEvent>(in TEvent @event) where TEvent : struct;

    /// <summary>
    ///     Subscribes to the event type
    /// </summary>
    /// <param name="eventHandler">The event handler</param>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <returns>Disposable to unsubscribe</returns>
    IDisposable Subscribe<TEvent>(TEventHandler<TEvent> eventHandler) where TEvent : struct;
}

/// <inheritdoc />
public sealed class EventBus : IEventBus
{
    private readonly ILogger<EventBus> logger;
    private readonly ConcurrentDictionary<Type, object> subscriptions = new();
    private readonly object subscriptionLock = new();

    public EventBus(ILogger<EventBus> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(in TEvent @event) where TEvent : struct
    {
        if (!subscriptions.TryGetValue(typeof(TEvent), out var handlers))
            return;

        ((Handlers<TEvent>)handlers).Handle(@event);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(TEventHandler<TEvent> eventHandler) where TEvent : struct
    {
        if (!subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (subscriptionLock)
            {
                if (!subscriptions.TryGetValue(typeof(TEvent), out handlers))
                {
                    handlers = subscriptions[typeof(TEvent)] = new Handlers<TEvent>(logger);
                }
            }
        }
        var typedHandlers = (Handlers<TEvent>)handlers;
        typedHandlers.Add(eventHandler);
        return new Subscription<TEvent>(typedHandlers, eventHandler);
    }

    private sealed class Handlers<TEvent> where TEvent : struct
    {
        private readonly ILogger logger;
        private readonly object mutex = new();
        private volatile List<TEventHandler<TEvent>> handlers = new();

        public Handlers(ILogger logger)
        {
            this.logger = logger;
        }

        public void Add(TEventHandler<TEvent> eventHandler)
        {
            lock (mutex)
            {
                var newHandlers = new List<TEventHandler<TEvent>>(handlers);
                newHandlers.Add(eventHandler);
                handlers = newHandlers;
            }
        }

        public void Remove(TEventHandler<TEvent> eventHandler)
        {
            lock (mutex)
            {
                var newHandlers = new List<TEventHandler<TEvent>>(handlers);
                newHandlers.Remove(eventHandler);
                handlers = newHandlers;
            }
        }

        public void Handle(in TEvent @event)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var handler in handlers)
                try
                {
                    handler(in @event);
                }
                catch (Exception exception)
                {
                    logger.ErrorException("Failed to handle {event}", exception, @event);
                }
        }
    }

    private sealed class Subscription<TEvent> : IDisposable where TEvent : struct
    {
        private readonly Handlers<TEvent> handlers;
        private readonly TEventHandler<TEvent> eventHandler;

        public Subscription(Handlers<TEvent> handlers, TEventHandler<TEvent> eventHandler)
        {
            this.handlers = handlers;
            this.eventHandler = eventHandler;
        }

        public void Dispose() => handlers.Remove(eventHandler);
    }
}
