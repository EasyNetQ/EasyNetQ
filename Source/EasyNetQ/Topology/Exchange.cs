using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public sealed class Exchange : IExchange
    {
        public Exchange(string name, string type = ExchangeType.Direct, bool durable = true, bool autoDelete = false, IDictionary<string, object> arguments = null)
        {
            Preconditions.CheckNotNull(name, "name");

            Name = name;
            Type = type;
            IsDurable = durable;
            IsAutoDelete = autoDelete;
            Arguments = arguments ?? new Dictionary<string, object>();
        }

        public string Name { get; }
        public string Type { get; }
        public bool IsDurable { get; }
        public bool IsAutoDelete { get; }
        public IDictionary<string, object> Arguments { get; }

        public static IExchange GetDefault()
        {
            return new Exchange("");
        }
    }
}
