using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    /// <inheritdoc />
    public sealed class Queue : IQueue
    {
        /// <summary>
        ///     Creates Queue
        /// </summary>
        public Queue(string name, bool isDurable = true, bool isExclusive = false, bool isAutoDelete = false, IDictionary<string, object> arguments = null)
        {
            Preconditions.CheckNotBlank(name, "name");

            Name = name;
            IsDurable = isDurable;
            IsExclusive = isExclusive;
            IsAutoDelete = isAutoDelete;
            Arguments = arguments ?? new Dictionary<string, object>();
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsDurable { get; }

        /// <inheritdoc />
        public bool IsExclusive { get; }

        /// <inheritdoc />
        public bool IsAutoDelete { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Arguments { get; }
    }
}
