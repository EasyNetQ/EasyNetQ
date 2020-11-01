using System;

namespace EasyNetQ.Hosepipe
{
    public class HosepipeMessage
    {
        public string Body { get; }
        public MessageProperties Properties { get; }
        public MessageReceivedInfo Info { get; }

        public HosepipeMessage(string body, MessageProperties properties, MessageReceivedInfo info)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }
    }
}
