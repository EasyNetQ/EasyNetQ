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
        void Subscribe<TEvent>(Action<TEvent> eventHandler);
    }

    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, IList<object>> subscriptions = 
            new ConcurrentDictionary<Type, IList<object>>();

        public void Publish<TEvent>(TEvent @event)
        {
            if (!subscriptions.ContainsKey(typeof (TEvent))) return;
            foreach (var eventHandler in subscriptions[typeof(TEvent)])
            {
                ((Action<TEvent>) eventHandler)(@event);
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> eventHandler)
        {
            subscriptions.AddOrUpdate(typeof(TEvent),
                    t => new List<object> { eventHandler },
                    (t, l) =>
                    {
                        l.Add(eventHandler);
                        return l;
                    }
                );
        }
    }
}