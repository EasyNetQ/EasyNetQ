using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    /// <summary>
    /// Represents an AMQP exchange
    /// </summary>
    public interface IExchange : IBindable
    {
        /// <summary>
        /// The exchange name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The exchange type.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// If set the exchange remains active when a server restarts.
        /// </summary>
        bool IsDurable { get; }

        /// <summary>
        /// If set the exchange is deleted when all consumers have finished using it.
        /// </summary>
        bool IsAutoDelete { get; }

        /// <summary>
        /// The exchange arguments.
        /// </summary>
        IDictionary<string, object> Arguments { get; }
    }
}
