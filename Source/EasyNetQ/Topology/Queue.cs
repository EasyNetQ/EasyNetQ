using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public class Queue : IQueue
    {
        readonly bool durable;
        readonly bool exclusive;
        readonly bool autoDelete;

        private readonly IList<IBinding> bindings = new List<IBinding>();

        public static IQueue CreateDurable(string queueName)
        {
            return new Queue(true, false, false, queueName);
        }

        protected Queue(bool durable, bool exclusive, bool autoDelete, string name)
        {
            this.durable = durable;
            this.exclusive = exclusive;
            this.autoDelete = autoDelete;
            Name = name;
        }

        public string Name { get; private set; }

        public void BindTo(IExchange exchange, params string[] routingKeys)
        {
            var binding = new Binding(this, exchange, routingKeys);
            bindings.Add(binding);
        }

        public void Visit(ITopologyVisitor visitor)
        {
            visitor.CreateQueue(Name, durable, exclusive, autoDelete);
            foreach (var binding in bindings)
            {
                binding.Visit(visitor);
            }
        }

        public static IQueue CreateTransient(string queueName)
        {
            return new Queue(false, true, true, queueName);
        }
    }
}