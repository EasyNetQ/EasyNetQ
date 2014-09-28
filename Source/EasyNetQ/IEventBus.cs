using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EasyNetQ
{
    using System.Collections;
    using System.Collections.Immutable;
    using System.Linq;

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
        private readonly ConcurrentDictionary<Type, IImmutableSet<object>> subscriptions = 
            new ConcurrentDictionary<Type, IImmutableSet<object>>();

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
            subscriptions.AddOrUpdate(
                typeof(TEvent),
                t => ImmutableHashSet.Create<object>(eventHandler),
                (t, l) => l.Add(eventHandler));

            return () =>
                {
                    IImmutableSet<object> comparisonValue;
                    while (subscriptions.TryGetValue(typeof(TEvent), out comparisonValue))
                    {
                        if (comparisonValue.Contains(eventHandler))
                        {
                            IImmutableSet<object> newValue = comparisonValue.Remove(eventHandler);
                            if (newValue.Any())
                            {
                                if (subscriptions.TryUpdate(typeof(TEvent), newValue, comparisonValue))
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if (((ICollection<KeyValuePair<Type, IImmutableSet<object>>>)subscriptions).Remove(new KeyValuePair<Type, IImmutableSet<object>>(
                                    typeof(TEvent),
                                    comparisonValue)))
                                {
                                    return;
                                }
                            }
                        }
                    }
                };
        }
    }

    public delegate void CancelSubscription();
}