using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public sealed class Binding : IBinding
    {
        public Binding(IBindable bindable, IExchange exchange, string routingKey, IDictionary<string, object> arguments)
        {
            Preconditions.CheckNotNull(bindable, "bindable");
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(routingKey, "routingKey");

            Bindable = bindable;
            Exchange = exchange;
            RoutingKey = routingKey;
            Arguments = arguments;
        }

        public IBindable Bindable { get; }
        public IExchange Exchange { get; }
        public string RoutingKey { get; }
        public IDictionary<string, object> Arguments { get; }
    }
}
