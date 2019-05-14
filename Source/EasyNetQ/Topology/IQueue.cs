using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    /// <summary>
    /// Represents an AMQP queue
    /// </summary>
    public interface IQueue : IBindable
    {
        /// <summary>
        /// The queue name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// If set the queue remains active when a server restarts.
        /// </summary>
        bool IsDurable { get; }

        /// <summary>
        /// If set the queue may only be accessed by the current connection, and are deleted when that connection closes.
        /// </summary>
        bool IsExclusive { get; }

        /// <summary>
        /// If set the queue is deleted when all consumers have finished using it.
        /// </summary>
        bool IsAutoDelete { get; }

        /// <summary>
        /// The queue arguments.
        /// </summary>
        IDictionary<string, object> Arguments { get; }
    }
}
