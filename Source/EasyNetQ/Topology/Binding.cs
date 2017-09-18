namespace EasyNetQ.Topology
{
    using System.Collections.Generic;

    public class Binding : IBinding
    {
        public Binding(IBindable bindable, IExchange exchange, string routingKey, IDictionary<string, object> headers)
        {
            Preconditions.CheckNotNull(bindable, "bindable");
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(routingKey, "routingKey");
            Preconditions.CheckNotNull(headers, "headers");

            Bindable = bindable;
            Exchange = exchange;
            RoutingKey = routingKey;
            Headers = headers;
        }

        public IBindable Bindable { get; }
        public IExchange Exchange { get; }
        public string RoutingKey { get; }
        public IDictionary<string, object> Headers { get; }
    }
}