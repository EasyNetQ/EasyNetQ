using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EasyNetQ.Topology
{
    /// <summary>
    ///     Represents an AMQP exchange
    /// </summary>
    public readonly struct Exchange
    {
        /// <summary>
        ///     Returns the default exchange
        /// </summary>
        public static Exchange Default { get; } = new Exchange("");

        /// <summary>
        ///     Creates Exchange
        /// </summary>
        public Exchange(
            string name,
            string type = ExchangeType.Direct,
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object> arguments = null
        )
        {
            Preconditions.CheckNotNull(name, "name");

            Name = name;
            Type = type;
            IsDurable = durable;
            IsAutoDelete = autoDelete;
            Arguments = arguments == null ? null : new ReadOnlyDictionary<string, object>(arguments);
        }

        /// <summary>
        ///     The exchange name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The exchange type
        /// </summary>
        public string Type { get; }

        /// <summary>
        ///     If set the exchange remains active when a server restarts
        /// </summary>
        public bool IsDurable { get; }

        /// <summary>
        ///     If set the exchange is deleted when all consumers have finished using it
        /// </summary>
        public bool IsAutoDelete { get; }

        /// <summary>
        /// The exchange arguments.
        /// </summary>
        public IReadOnlyDictionary<string, object> Arguments { get; }

        /// <summary>
        ///     Checks exchanges for equality
        /// </summary>
        public bool Equals(Exchange other)
        {
            return Name == other.Name
                   && Type == other.Type
                   && IsDurable == other.IsDurable
                   && IsAutoDelete == other.IsAutoDelete
                   && Equals(Arguments, other.Arguments);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Exchange other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsDurable.GetHashCode();
                hashCode = (hashCode * 397) ^ IsAutoDelete.GetHashCode();
                return hashCode;
            }
        }
    }
}
