using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Topology
{
    public class Exchange : IExchange
    {
        protected readonly IList<IBinding> bindings = new List<IBinding>();
        public string Name { get; private set; }
        public string ExchangeType { get; private set; }
        public bool Durable { get; private set; }
        public bool AutoDelete { get; private set; }
        public IDictionary Arguments { get; private set; }

        public static IExchange DeclareDirect(string exchangeName)
        {
            Preconditions.CheckNotBlank(exchangeName, "exchangeName");
            return new Exchange(exchangeName, Topology.ExchangeType.Direct);
        }

        public static IExchange DeclareDirect(string exchangeName, bool durable, bool autoDelete, IDictionary arguments)
        {
            Preconditions.CheckNotBlank(exchangeName, "exchangeName");
            return new Exchange(exchangeName, Topology.ExchangeType.Direct, durable, autoDelete, arguments);
        }

        public static IExchange DeclareTopic(string exchangeName)
        {
            Preconditions.CheckNotBlank(exchangeName, "exchangeName");
            return new Exchange(exchangeName, Topology.ExchangeType.Topic);
        }

        public static IExchange DeclareTopic(string exchangeName, bool durable, bool autoDelete, IDictionary arguments)
        {
            Preconditions.CheckNotBlank(exchangeName, "exchangeName");
            return new Exchange(exchangeName, Topology.ExchangeType.Topic, durable, autoDelete, arguments);
        }

        public static IExchange DeclareFanout(string exchangeName)
        {
            Preconditions.CheckNotBlank(exchangeName, "exchangeName");
            return new Exchange(exchangeName, Topology.ExchangeType.Fanout);
        }

        public static IExchange DeclareFanout(string exchangeName, bool durable, bool autoDelete, IDictionary arguments)
        {
            Preconditions.CheckNotBlank(exchangeName, "exchangeName");
            return new Exchange(exchangeName, Topology.ExchangeType.Fanout, durable, autoDelete, arguments);
        }

        public static IExchange GetDefault()
        {
            return new DefaultExchange();
        }

        protected Exchange(string name, string exchangeType)
        {
            Preconditions.CheckNotNull(name, "name");

            Name = name;
            ExchangeType = exchangeType;
            Durable = true;
        }

        protected Exchange(string name, string exchangeType, bool durable, bool autoDelete, IDictionary arguments)
        {
            Preconditions.CheckNotNull(name, "name");

            Name = name;
            ExchangeType = exchangeType;
            Durable = durable;
            AutoDelete = autoDelete;
            Arguments = arguments;
        }

        public virtual void Visit(ITopologyVisitor visitor)
        {
            Preconditions.CheckNotNull(visitor, "visitor");

            if (Name != string.Empty)
            {
                visitor.CreateExchange(Name, ExchangeType, Durable, AutoDelete, Arguments);
                foreach (var binding in bindings)
                {
                    binding.Visit(visitor);
                }
            }
        }

        public virtual void BindTo(IExchange exchange, params string[] routingKeys)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckAny(routingKeys, "routingKeys", "There must be at least one routingKey");
            Preconditions.CheckFalse(routingKeys.Any(string.IsNullOrEmpty), "routingKeys", "RoutingKey is null or empty");

            var binding = new Binding(this, exchange, routingKeys);
            bindings.Add(binding);
        }
    }
}