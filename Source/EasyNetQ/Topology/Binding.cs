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

        public IBindable Bindable { get; private set; }
        public IExchange Exchange { get; private set; }
        public string RoutingKey { get; private set; }
        public IDictionary<string, object> Headers { get; private set; }
    }
}