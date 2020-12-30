using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    /// <summary>
    ///     Binding between exchange and bindable entity
    /// </summary>
    public readonly struct Binding<TBindable> where TBindable : struct, IBindable
    {
        /// <summary>
        ///     Creates Binding
        /// </summary>
        public Binding(Exchange source, TBindable destination, string routingKey, IDictionary<string, object> arguments = null)
        {
            Preconditions.CheckNotNull(routingKey, "routingKey");

            Source = source;
            Destination = destination;
            RoutingKey = routingKey;
            Arguments = arguments;
        }

        /// <summary>
        ///     Source exchange
        /// </summary>
        public Exchange Source { get; }

        /// <summary>
        ///     Destination bindable instance
        /// </summary>
        public TBindable Destination { get; }

        /// <summary>
        ///     The binding routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     The binging arguments
        /// </summary>
        public IDictionary<string, object> Arguments { get; }
    }
}
