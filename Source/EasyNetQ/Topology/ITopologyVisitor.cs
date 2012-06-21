using System.Collections.Generic;

namespace EasyNetQ.Topology
{
    public interface ITopologyVisitor
    {
        void CreateExchange(string exchangeName, string exchangeType);
        void CreateQueue(string queueName, bool durable, bool exclusive, bool autoDelete, Dictionary<string, string> arguments);
        string CreateQueue();
        void CreateBinding(IBindable bindable, IExchange exchange, string[] routingKeys);
    }
}