using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace EasyNetQ.Topology
{
    public class TopologyBuilder : ITopologyVisitor
    {
        private readonly IModel model;

        public TopologyBuilder(IModel model)
        {
            if(model == null)
            {
                throw new ArgumentNullException("model");
            }

            this.model = model;
        }

        public void CreateExchange(string exchangeName, string exchangeType, bool autoDelete, IDictionary arguments)
        {
            if(exchangeName == null)
            {
                throw new ArgumentNullException("exchangeName");
            }

            model.ExchangeDeclare(exchangeName, exchangeType, true, autoDelete, arguments);
        }

        public void CreateQueue(string queueName, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException("queueName is null or empty");
            }

            model.QueueDeclare(queueName, durable, exclusive, autoDelete, (IDictionary)arguments);
        }

        public string CreateQueue()
        {
            var queueDeclareOk = model.QueueDeclare();
            return queueDeclareOk.QueueName;
        }

        public void CreateBinding(IBindable bindable, IExchange exchange, string[] routingKeys)
        {
            if(bindable == null)
            {
                throw new ArgumentNullException("bindable");
            }
            if(exchange == null)
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

            var queue = bindable as IQueue;
            if(queue != null)
            {
                foreach (var routingKey in routingKeys)
                {
                    model.QueueBind(queue.Name, exchange.Name, routingKey);
                }
            }
            var targetExchange = bindable as IExchange;
            if (targetExchange != null)
            {
                foreach (var routingKey in routingKeys)
                {
                    model.ExchangeBind(targetExchange.Name, exchange.Name, routingKey);
                }
            }
        }
    }
}