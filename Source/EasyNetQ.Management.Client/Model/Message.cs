using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Property
    {
    }

    public class Message
    {
        public int payload_bytes { get; set; }
        public bool redelivered { get; set; }
        public string exchange { get; set; }
        public string routing_key { get; set; }
        public int message_count { get; set; }
        public List<Property> properties { get; set; }
        public string payload { get; set; }
        public string payload_encoding { get; set; }
    }
}