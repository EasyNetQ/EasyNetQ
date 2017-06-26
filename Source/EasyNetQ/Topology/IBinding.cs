namespace EasyNetQ.Topology
{
    using System.Collections.Generic;

    public interface IBinding
    {
        IBindable Bindable { get; }
        IExchange Exchange { get; }
        string RoutingKey { get; }

        IDictionary<string, object> Headers { get; }
    }
}