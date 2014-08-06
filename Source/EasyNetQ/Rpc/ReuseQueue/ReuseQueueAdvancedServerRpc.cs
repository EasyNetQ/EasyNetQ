using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc.ReuseQueue
{
    class ReuseQueueAdvancedServerRpc : IAdvancedServerRpc
    {
        public IDisposable Respond(IExchange requestExchange, IQueue queue, string topic, Func<SerializedMessage, Task<SerializedMessage>> handleRequest)
        {
            throw new NotImplementedException();
        }
    }
}