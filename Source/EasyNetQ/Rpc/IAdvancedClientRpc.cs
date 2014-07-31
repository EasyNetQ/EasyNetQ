using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    public interface IAdvancedClientRpc
    {
        Task<SerializedMessage> RequestAsync(IExchange requestExchange,
                                             string requestRoutingKey,
                                             bool mandatory,
                                             bool immediate,
                                             Func<string> responseQueueName,
                                             SerializedMessage request);
    }
}