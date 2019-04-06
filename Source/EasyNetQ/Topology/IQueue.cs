using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    /// <summary>
    /// Represents an AMQP queue
    /// </summary>
    public interface IQueue : IBindable
    {
        string Name { get; }
        bool IsDurable { get; }
        bool IsExclusive { get; }
        bool IsAutoDelete { get; }
        IDictionary<string, object> Arguments { get; }
    }
}
