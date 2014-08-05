using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class ConsumeSingleResult
    {
        public Task<ConsumeSingleMessageContext> MessageTask { get; private set; }
        public IQueue Queue { get; private set; }

        public ConsumeSingleResult(Task<ConsumeSingleMessageContext> messageTask, IQueue queue)
        {
            MessageTask = messageTask;
            Queue = queue;
        }
    }

    public class ConsumeSingleMessageContext
    {
        public byte[] Message { get; private set; }
        public MessageProperties Properties { get; private set; }
        public MessageReceivedInfo Info { get; private set; }

        public ConsumeSingleMessageContext(byte[] message, MessageProperties properties, MessageReceivedInfo info)
        {
            Message = message;
            Properties = properties;
            Info = info;
        }
    }
}