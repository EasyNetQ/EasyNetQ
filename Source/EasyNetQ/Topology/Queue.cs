using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EasyNetQ.Topology
{
    /// <summary>
    ///     Represents an AMQP queue
    /// </summary>
    public readonly struct Queue
    {
        /// <summary>
        ///     Creates Queue
        /// </summary>
        public Queue(
            string name,
            bool isDurable = true,
            bool isExclusive = false,
            bool isAutoDelete = false,
            IDictionary<string, object> arguments = null
        )
        {
            Preconditions.CheckNotBlank(name, "name");

            Name = name;
            IsDurable = isDurable;
            IsExclusive = isExclusive;
            IsAutoDelete = isAutoDelete;
            Arguments = arguments == null ? null : new ReadOnlyDictionary<string, object>(arguments);
        }

        /// <summary>
        ///     The queue name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     If set the queue remains active when a server restarts
        /// </summary>
        public bool IsDurable { get; }

        /// <summary>
        ///     If set the queue may only be accessed by the current connection, and are deleted when that connection closes
        /// </summary>
        public bool IsExclusive { get; }

        /// <summary>
        ///     If set the queue is deleted when all consumers have finished using it
        /// </summary>
        public bool IsAutoDelete { get; }

        /// <summary>
        ///     The queue arguments
        /// </summary>
        public IReadOnlyDictionary<string, object> Arguments { get; }
    }
}
