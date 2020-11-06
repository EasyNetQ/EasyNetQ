using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Topology
{
    /// <inheritdoc />
    public sealed class Queue : IQueue
    {
        /// <summary>
        ///     Creates Queue
        /// </summary>
        public Queue(string name, bool isDurable = true, bool isExclusive = false, bool isAutoDelete = false,
            IDictionary<string, object> arguments = null)
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

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDurable.GetHashCode();
                hashCode = (hashCode * 397) ^ IsExclusive.GetHashCode();
                hashCode = (hashCode * 397) ^ IsAutoDelete.GetHashCode();
                hashCode = (hashCode * 397) ^ Arguments.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Queue other && Equals(other);
        }

        private bool Equals(Queue other)
        {
            return Name == other.Name
                   && IsDurable == other.IsDurable
                   && IsExclusive == other.IsExclusive
                   && IsAutoDelete == other.IsAutoDelete
                   && Arguments.SequenceEqual(other.Arguments);
        }
    }
}
