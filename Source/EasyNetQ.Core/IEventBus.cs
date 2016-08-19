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
        CancelSubscription Subscribe<TEvent>(Action<TEvent> eventHandler);
    }

    public class EventBus : IEventBus
    {
        private class Handlers
        {
            private readonly object internalHandlersLock = new object();
            private readonly List<object> internalHandlers = new List<object>(); 

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
        private readonly object subscriptionLock = new object();

        public void Publish<TEvent>(TEvent @event)
        {
            Handlers handlers;
            if (!subscriptions.TryGetValue(typeof (TEvent), out handlers))
                return;
            foreach (var handler in handlers.AsEnumerable())
                ((Action<TEvent>) handler)(@event);
        }

        public CancelSubscription Subscribe<TEvent>(Action<TEvent> eventHandler)
        {
            AddSubscription(eventHandler);
            return GetCancelSubscriptionDelegate(eventHandler);
        }

        private void AddSubscription<TEvent>(Action<TEvent> handler)
        {
            var type = typeof (TEvent);
            Handlers handlers;
            if (!subscriptions.TryGetValue(type, out handlers))
                lock (subscriptionLock)
                    if (!subscriptions.TryGetValue(type, out handlers))
                        subscriptions[type] = handlers = new Handlers();
            handlers.Add(handler);
        }

        private CancelSubscription GetCancelSubscriptionDelegate<TEvent>(Action<TEvent> eventHandler)
        {
            return () =>
                {
                    Handlers handlers;
                    if (!subscriptions.TryGetValue(typeof (TEvent), out handlers))
                        return;
                    handlers.Remove(eventHandler);
                };
        }
    }

    public delegate void CancelSubscription();
}