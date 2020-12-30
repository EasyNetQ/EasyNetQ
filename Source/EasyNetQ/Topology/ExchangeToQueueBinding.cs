using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EasyNetQ.Topology
{
    /// <summary>
    ///     Binding between exchange and queue
    /// </summary>
    public readonly struct ExchangeToQueueBinding
    {
        /// <summary>
        ///     Creates Binding
        /// </summary>
        public ExchangeToQueueBinding(Exchange source, Queue destination, string routingKey, IDictionary<string, object> arguments = null)
        {
            Preconditions.CheckNotNull(routingKey, "routingKey");

            Destination = destination;
            Source = source;
            RoutingKey = routingKey;
            Arguments = arguments == null ? null : new ReadOnlyDictionary<string, object>(arguments);
        }

        /// <summary>
        ///     Source exchange
        /// </summary>
        public Exchange Source { get; }

        /// <summary>
        ///     Destination queue
        /// </summary>
        public Queue Destination { get; }

        /// <summary>
        ///     The binding routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     The binging arguments
        /// </summary>
        public IReadOnlyDictionary<string, object> Arguments { get; }
    }
}
