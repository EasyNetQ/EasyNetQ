using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public abstract class Exchange : IExchange
    {
        protected readonly IList<IBinding> bindings = new List<IBinding>();

        public static IExchange CreateDirect(string exchangeName)
        {
            return new DirectExchange(exchangeName);
        }

        protected Exchange(string name)
        {
            Name = name;
        }

        public abstract void Visit(ITopologyVisitor visitor);

        public string Name { get; private set; }

        public static IExchange CreateTopic(string exchangeName)
        {
            return new TopicExchange(exchangeName);
        }

        public void BindTo(IExchange exchange, params string[] routingKeys)
        {
            var binding = new Binding(this, exchange, routingKeys);
            bindings.Add(binding);
        }
    }
}