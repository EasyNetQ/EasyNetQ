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
            Preconditions.CheckNotNull(model, "model");

            this.model = model;
        }

        public void CreateExchange(string exchangeName, string exchangeType, bool autoDelete, IDictionary arguments)
        {
            Preconditions.CheckNotNull(exchangeName, "exchangeName");

            model.ExchangeDeclare(exchangeName, exchangeType, true, autoDelete, arguments);
        }

        public void CreateQueue(string queueName, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            Preconditions.CheckNotBlank(queueName, "queueName");

            model.QueueDeclare(queueName, durable, exclusive, autoDelete, (IDictionary)arguments);
        }

        public string CreateQueue()
        {
            var queueDeclareOk = model.QueueDeclare();
            return queueDeclareOk.QueueName;
        }

        public void CreateBinding(IBindable bindable, IExchange exchange, string[] routingKeys)
        {
            Preconditions.CheckNotNull(bindable, "bindable");
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckAny(routingKeys, "routingKeys", "There must be at least one routingKey");
            Preconditions.CheckFalse(routingKeys.Any(string.IsNullOrEmpty), "routingKeys", "RoutingKey is null or empty");

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