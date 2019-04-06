using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public sealed class Queue : IQueue
    {
        public Queue(string name, bool isDurable = true, bool isExclusive = false, bool isAutoDelete = false, IDictionary<string, object> arguments = null)
        {
            Preconditions.CheckNotBlank(name, "name");

            Name = name;
            IsDurable = isDurable;
            IsExclusive = isExclusive;
            IsAutoDelete = isAutoDelete;
            Arguments = arguments ?? new Dictionary<string, object>();
        }

        public string Name { get; }
        public bool IsDurable { get; }
        public bool IsExclusive { get; }
        public bool IsAutoDelete { get; }
        public IDictionary<string, object> Arguments { get; }
    }
}
