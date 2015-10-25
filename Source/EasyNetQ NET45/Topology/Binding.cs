namespace EasyNetQ.Topology
{
    public class Binding : IBinding
    {
        public Binding(IBindable bindable, IExchange exchange, string routingKey)
        {
            Preconditions.CheckNotNull(bindable, "bindable");
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(routingKey, "routingKey");

            Bindable = bindable;
            Exchange = exchange;
            RoutingKey = routingKey;
        }

        public IBindable Bindable { get; private set; }
        public IExchange Exchange { get; private set; }
        public string RoutingKey { get; private set; }
    }
}