using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        public ExchangeToExchangeBinding(Exchange source, Exchange destination, string routingKey, IDictionary<string, object> arguments)
        {
            Preconditions.CheckNotNull(routingKey, "routingKey");

            Destination = destination;
            Source = source;
            RoutingKey = routingKey;
            Arguments = new ReadOnlyDictionary<string, object>(arguments ?? new Dictionary<string, object>());
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

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ExchangeToExchangeBinding other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Destination.GetHashCode();
                hashCode = (hashCode * 397) ^ Source.GetHashCode();
                hashCode = (hashCode * 397) ^ RoutingKey.GetHashCode();
                return hashCode;
            }
        }

        private bool Equals(ExchangeToExchangeBinding other)
        {
            return Destination.Equals(other.Destination)
                   && Source.Equals(other.Source)
                   && RoutingKey == other.RoutingKey
                   && Arguments.SequenceEqual(other.Arguments);
        }
    }
}
