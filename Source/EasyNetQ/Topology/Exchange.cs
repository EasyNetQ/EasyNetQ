using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    /// <inheritdoc />
    public sealed class Exchange : IExchange
    {
        /// <summary>
        ///     Creates Exchange
        /// </summary>
        public Exchange(string name, string type = ExchangeType.Direct, bool durable = true, bool autoDelete = false, IDictionary<string, object> arguments = null)
        {
            Preconditions.CheckNotNull(name, "name");

            Name = name;
            Type = type;
            IsDurable = durable;
            IsAutoDelete = autoDelete;
            Arguments = arguments ?? new Dictionary<string, object>();
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Type { get; }

        /// <inheritdoc />
        public bool IsDurable { get; }

        /// <inheritdoc />
        public bool IsAutoDelete { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Arguments { get; }

        /// <summary>
        ///     Returns the default exchange
        /// </summary>
        public static IExchange GetDefault()
        {
            return new Exchange("");
        }
    }
}
