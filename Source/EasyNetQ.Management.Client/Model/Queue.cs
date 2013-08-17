using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Queue
    {
        public int Memory { get; set; }
        public string IdleSince { get; set; }
        public string Policy { get; set; }
        public string ExclusiveConsumerTag { get; set; }
        public int MessagesReady { get; set; }
        public int MessagesUnacknowledged { get; set; }
        public int Messages { get; set; }
        public int Consumers { get; set; }
        public int ActiveConsumers { get; set; }
        public BackingQueueStatus BackingQueueStatus { get; set; }
        public List<object> ConsumerDetails { get; set; }
        public string Name { get; set; }
        public string Vhost { get; set; }
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public Arguments Arguments { get; set; }
        public string Node { get; set; }
        public IEnumerable<string> SlaveNodes { get; set; }
        public IEnumerable<string> SynchronisedSlaveNodes { get; set; }
    }
}