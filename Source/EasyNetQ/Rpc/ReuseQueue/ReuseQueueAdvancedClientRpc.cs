using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc.ReuseQueue
{
    class ReuseQueueAdvancedClientRpc : IAdvancedClientRpc
    {
        public Task<SerializedMessage> RequestAsync(IExchange requestExchange, string requestRoutingKey, bool mandatory, bool immediate, TimeSpan timeout,
                                                    SerializedMessage request)
        {
            throw new NotImplementedException();
        }
    }
}