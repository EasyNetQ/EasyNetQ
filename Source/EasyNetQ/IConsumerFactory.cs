using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public interface IConsumerFactory : IDisposable
    {
        IConsumer CreateConsumer(
            IQueue queue, 
            Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection);
    }
}