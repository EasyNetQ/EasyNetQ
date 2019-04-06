using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public interface IExchange : IBindable
    {
        string Name { get; }
        string Type { get; }
        bool IsDurable { get; }
        bool IsAutoDelete { get; }
        IDictionary<string, object> Arguments { get; }
    }
}
