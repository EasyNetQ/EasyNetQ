using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    public interface IAdvancedServerRpc
    {
        IDisposable Respond(IExchange requestExchange,
                            string queueName, 
                            string topic,
                            Func<SerializedMessage, Task<SerializedMessage>> handleRequest);
    }
}