using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public class Exchange : IExchange
    {
        protected readonly IList<IBinding> bindings = new List<IBinding>();
        public string Name { get; private set; }
        public ExchangeType ExchangeType { get; private set; }

        public static IExchange CreateDirect(string exchangeName)
        {
            return new Exchange(exchangeName, ExchangeType.Direct);
        }

        public static IExchange CreateTopic(string exchangeName)
        {
            return new Exchange(exchangeName, ExchangeType.Topic);
        }

        protected Exchange(string name, ExchangeType exchangeType)
        {
            Name = name;
            ExchangeType = exchangeType;
        }

        public void Visit(ITopologyVisitor visitor)
        {
            visitor.CreateExchange(Name, ExchangeType);
            foreach (var binding in bindings)
            {
                binding.Visit(visitor);
            }
        }

        public void BindTo(IExchange exchange, params string[] routingKeys)
        {
            var binding = new Binding(this, exchange, routingKeys);
            bindings.Add(binding);
        }
    }
}