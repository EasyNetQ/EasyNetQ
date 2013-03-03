using System;
using System.Linq;

namespace EasyNetQ.Topology
{
    public class Binding : IBinding
    {
        public Binding(IBindable bindable, IExchange exchange, params string[] routingKeys)
        {
            Preconditions.CheckNotNull(bindable, "bindable");
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckAny(routingKeys, "routingKeys", "There must be at least one routingKey");
            Preconditions.CheckFalse(routingKeys.Any(string.IsNullOrEmpty), "routingKeys", "RoutingKey is null or empty");

            Bindable = bindable;
            Exchange = exchange;
            RoutingKeys = routingKeys;
        }

        public void Visit(ITopologyVisitor visitor)
        {
            Preconditions.CheckNotNull(visitor, "visitor");

            Exchange.Visit(visitor);
            visitor.CreateBinding(Bindable, Exchange, RoutingKeys);
        }

        public IBindable Bindable { get; private set; }
        public IExchange Exchange { get; private set; }
        public string[] RoutingKeys { get; private set; }
    }
}