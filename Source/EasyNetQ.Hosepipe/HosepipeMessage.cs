using System;

namespace EasyNetQ.Hosepipe
{
    public class HosepipeMessage
    {
        public string Body { get; private set; }
        public MessageProperties Properties { get; private set; }
        public MessageReceivedInfo Info { get; private set; }

        public HosepipeMessage(string body, MessageProperties properties, MessageReceivedInfo info)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }
    }
}
