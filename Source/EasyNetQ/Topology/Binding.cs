using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EasyNetQ.Topology
{
    /// <summary>
    ///     Binding between exchange and exchange
    /// </summary>
    public readonly struct Binding<TDestination> where TDestination : IBindable
    {
        /// <summary>
        ///     Creates Binding
        /// </summary>
        public Binding(Exchange source, TDestination destination, string routingKey, IDictionary<string, object> arguments = null)
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
        ///     Destination bindable instance
        /// </summary>
        public TDestination Destination { get; }

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
