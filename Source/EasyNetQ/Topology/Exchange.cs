using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Topology
{
    public class Exchange : IExchange
    {
        protected readonly IList<IBinding> bindings = new List<IBinding>();
        public string Name { get; private set; }
        public ExchangeType ExchangeType { get; private set; }

        public static IExchange DeclareDirect(string exchangeName)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentException("name is null or empty");
            }
            return new Exchange(exchangeName, ExchangeType.Direct);
        }

        public static IExchange DeclareTopic(string exchangeName)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentException("name is null or empty");
            }
            return new Exchange(exchangeName, ExchangeType.Topic);
        }

        public static IExchange DeclareFanout(string exchangeName)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentException("name is null or empty");
            }
            return new Exchange(exchangeName, ExchangeType.Fanout);
        }

        public static IExchange GetDefault()
        {
            return new Exchange("", ExchangeType.Direct);
        }

        protected Exchange(string name, ExchangeType exchangeType)
        {
            if(name == null)
            {
                throw new ArgumentNullException("name");
            }

            Name = name;
            ExchangeType = exchangeType;
        }

        public void Visit(ITopologyVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException("visitor");
            }

            visitor.CreateExchange(Name, ExchangeType);
            foreach (var binding in bindings)
            {
                binding.Visit(visitor);
            }
        }

        public void BindTo(IExchange exchange, params string[] routingKeys)
        {
            if (exchange == null)
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

            var binding = new Binding(this, exchange, routingKeys);
            bindings.Add(binding);
        }
    }
}