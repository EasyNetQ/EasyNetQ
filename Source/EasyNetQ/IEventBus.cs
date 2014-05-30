using System;
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
        private readonly ConcurrentDictionary<Type, IList<object>> subscriptions = 
            new ConcurrentDictionary<Type, IList<object>>();

        public void Publish<TEvent>(TEvent @event)
        {
            if (!subscriptions.ContainsKey(typeof (TEvent))) return;

            // Create a local copy of handlers to avoid any interference from
            // handler subscribing to events and modifying collection.
            var handlers = new List<object>(subscriptions[typeof(TEvent)]);
            foreach (var eventHandler in handlers)
            {
                ((Action<TEvent>) eventHandler)(@event);
            }
        }

        public CancelSubscription Subscribe<TEvent>(Action<TEvent> eventHandler)
        {
            CancelSubscription cancelSubscription = null;

            subscriptions.AddOrUpdate(typeof(TEvent),
                    t =>
                    {
                        var l = new List<object> {eventHandler};
                        cancelSubscription = () => l.Remove(eventHandler);
                        return l;
                    },
                    (t, l) =>
                    {
                        l.Add(eventHandler);
                        cancelSubscription = () => l.Remove(eventHandler);
                        return l;
                    }
                );

            return cancelSubscription;
        }
    }

    public delegate void CancelSubscription();
}