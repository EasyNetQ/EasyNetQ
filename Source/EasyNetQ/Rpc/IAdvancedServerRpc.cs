using System;
using System.Threading.Tasks;

namespace EasyNetQ.Rpc
{
    public interface IAdvancedServerRpc
    {
        IDisposable Respond(string requestExchange, string queueName, string topic, Func<SerializedMessage, MessageReceivedInfo, Task<SerializedMessage>> handleRequest);
        IDisposable Respond(string requestExchange, string queueName, string topic, Func<SerializedMessage, Task<SerializedMessage>> handleRequest);
    }
}