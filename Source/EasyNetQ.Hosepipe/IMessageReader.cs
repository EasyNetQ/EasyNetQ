using System.Collections.Generic;

namespace EasyNetQ.Hosepipe
{
    public interface IMessageReader
    {
        IEnumerable<HosepipeMessage> ReadMessages(QueueParameters parameters);
        IEnumerable<HosepipeMessage> ReadMessages(QueueParameters parameters, string messageName);
    }
}
