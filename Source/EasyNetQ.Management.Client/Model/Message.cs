using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Properties : Dictionary<string, string>
    {
        public Properties()
        {
            Headers = new Dictionary<string, string>();
        }

        public Dictionary<string, string> Headers { get; set; } 
    }

    public class Message
    {
        public int PayloadBytes { get; set; }
        public bool Redelivered { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public int MessageCount { get; set; }
        public Properties Properties { get; set; }
        public string Payload { get; set; }
        public string PayloadEncoding { get; set; }
    }
}