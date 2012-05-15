using System;
using System.Linq;

namespace EasyNetQ.Topology
{
    public class Binding : IBinding
    {
        public Binding(IBindable bindable, IExchange exchange, params string[] routingKeys)
        {
            if(bindable == null)
            {
                throw new ArgumentNullException("bindable");
            }
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if (routingKeys.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("RoutingKey is null or empty");
            }
            if (routingKeys.Length == 0)
            {
                throw new ArgumentException("There must be at least one routingKey");
            }

            Bindable = bindable;
            Exchange = exchange;
            RoutingKeys = routingKeys;
        }

        public void Visit(ITopologyVisitor visitor)
        {
            if(visitor == null)
            {
                throw new ArgumentNullException("visitor");
            }

            Exchange.Visit(visitor);
            visitor.CreateBinding(Bindable, Exchange, RoutingKeys);
        }

        public IBindable Bindable { get; private set; }
        public IExchange Exchange { get; private set; }
        public string[] RoutingKeys { get; private set; }
    }
}