using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    public interface IAdvancedClientRpc
    {
        Task<SerializedMessage> RequestAsync(string requestExchange,
                                             string requestRoutingKey,
                                             bool mandatory,
                                             bool immediate,
                                             TimeSpan timeout,
                                             SerializedMessage request);
    }
}