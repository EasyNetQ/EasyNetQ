using System;
using RabbitMQ.Client;

namespace EasyNetQ.Topology
{
    public class TopologyVisitor : ITopologyVisitor
    {
        private readonly IModel model;

        public TopologyVisitor(IModel model)
        {
            this.model = model;
        }

        public void CreateExchange(string exchangeName, ExchangeType exchangeType)
        {
            model.ExchangeDeclare(exchangeName, Enum.GetName(typeof(ExchangeType), exchangeType), true);
        }
    }
}