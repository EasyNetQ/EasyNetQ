using System.Collections.Generic;

namespace EasyNetQ.Hosepipe
{
    public interface IMessageWriter
    {
        void Write(IEnumerable<string> messages, QueueParameters parameters);
    }
}