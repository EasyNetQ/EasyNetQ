using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Properties
    {
        public int delivery_mode { get; set; }
        public IDictionary<string, string> headers { get; set; }
    }

    public class Message
    {
        public int payload_bytes { get; set; }
        public bool redelivered { get; set; }
        public string exchange { get; set; }
        public string routing_key { get; set; }
        public int message_count { get; set; }
        public Properties properties { get; set; }
        public string payload { get; set; }
        public string payload_encoding { get; set; }
    }
}