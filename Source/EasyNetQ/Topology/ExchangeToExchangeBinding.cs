using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EasyNetQ.Topology
{
    /// <summary>
    ///     Binding between exchange and exchange
    /// </summary>
    public readonly struct ExchangeToExchangeBinding
    {
        /// <summary>
        ///     Creates Binding
        /// </summary>
        public ExchangeToExchangeBinding(Exchange source, Exchange destination, string routingKey, IDictionary<string, object> arguments = null)
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
        ///     Destination exchange
        /// </summary>
        public Exchange Destination { get; }

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
