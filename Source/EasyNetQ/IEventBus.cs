﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// An internal pub-sub bus to distribute events within EasyNetQ
    /// </summary>
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent @event);
        IDisposable Subscribe<TEvent>(Action<TEvent> eventHandler);
    }

    public class EventBus : IEventBus
    {
        private class Handlers
        {
            private readonly object internalHandlersLock = new object();
            private readonly List<object> internalHandlers;

            public Handlers()
            {
                internalHandlers = new List<object>();
            }

            public Handlers(params object[] handlers)
            {
                internalHandlers = new List<object>(handlers);
            }

            public void Add(object handler)
            {
                lock (internalHandlersLock)
                    internalHandlers.Add(handler);
            }

            public void Remove(object handler)
            {
                lock (internalHandlersLock)
                    internalHandlers.Remove(handler);
            }

            public IEnumerable<object> AsEnumerable()
            {
                lock (internalHandlersLock)
                    return internalHandlers.ToArray();
            }
        }

        private readonly ConcurrentDictionary<Type, Handlers> subscriptions = new ConcurrentDictionary<Type, Handlers>();

        public void Publish<TEvent>(TEvent @event)
        {
            if (!subscriptions.TryGetValue(typeof (TEvent), out var handlers))
                return;
            foreach (var handler in handlers.AsEnumerable())
                ((Action<TEvent>) handler)(@event);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> eventHandler)
        {
            AddSubscription(eventHandler);
            return GetCancelSubscriptionDelegate(eventHandler);
        }

        private void AddSubscription<TEvent>(Action<TEvent> handler)
        {
            subscriptions.AddOrUpdate(
                typeof (TEvent), 
                new Handlers(handler),
                (key, existingHandlers) => 
                {
                    existingHandlers.Add(handler);
                    return existingHandlers;
                }
            );
        }

        private IDisposable GetCancelSubscriptionDelegate<TEvent>(Action<TEvent> eventHandler)
        {
            return new Subscription(
                () =>
                {
                    if (!subscriptions.TryGetValue(typeof (TEvent), out var handlers))
                        return;
                    handlers.Remove(eventHandler);
                }
            );
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action unsubscribe;

            public Subscription(Action unsubscribe) => this.unsubscribe = unsubscribe;

            public void Dispose() => unsubscribe();
        }
    }
}