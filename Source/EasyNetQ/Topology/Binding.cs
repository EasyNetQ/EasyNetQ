namespace EasyNetQ.Topology
{
    public class Binding : IBinding
    {
        public Binding(IBindable bindable, IExchange exchange, params string[] routingKey)
        {
            Bindable = bindable;
            Exchange = exchange;
            RoutingKeys = routingKey;
        }

        public void Visit(ITopologyVisitor visitor)
        {
            Exchange.Visit(visitor);
            visitor.CreateBinding(Bindable, Exchange, RoutingKeys);
        }

        public IBindable Bindable { get; private set; }
        public IExchange Exchange { get; private set; }
        public string[] RoutingKeys { get; private set; }
    }
}