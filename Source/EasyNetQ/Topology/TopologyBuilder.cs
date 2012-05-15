using System;
using RabbitMQ.Client;

namespace EasyNetQ.Topology
{
    public class TopologyBuilder : ITopologyVisitor
    {
        private readonly IModel model;

        public TopologyBuilder(IModel model)
        {
            this.model = model;
        }

        public void CreateExchange(string exchangeName, ExchangeType exchangeType)
        {
            model.ExchangeDeclare(exchangeName, Enum.GetName(typeof(ExchangeType), exchangeType), true);
        }

        public void CreateQueue(string queueName, bool durable, bool exclusive, bool autoDelete)
        {
            model.QueueDeclare(queueName, durable, exclusive, autoDelete, null);
        }

        public void CreateBinding(IBindable bindable, IExchange exchange, string[] routingKeys)
        {
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