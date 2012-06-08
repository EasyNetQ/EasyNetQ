using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ.InMemoryClient
{
    public class ExchangeInfo
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public bool Durable { get; private set; }

        private readonly IList<BindingInfo> bindings = new List<BindingInfo>();

        public ExchangeInfo(string name, string type, bool durable)
        {
            Name = name;
            Type = type;
            Durable = durable;
        }

        public void Publish(string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            foreach (var bindingInfo in bindings)
            {
                if (bindingInfo.RoutingKeyMatches(routingKey))
                {
                    bindingInfo.Queue.AcceptMessage(Name, routingKey, basicProperties, body);
                }
            }
        }

        public void BindTo(QueueInfo queueInfo, string routingKey)
        {
            bindings.Add(new BindingInfo(queueInfo, routingKey));
        }
    }
}