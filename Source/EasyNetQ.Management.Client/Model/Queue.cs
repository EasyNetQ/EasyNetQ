using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Queue
    {
        public int memory { get; set; }
        public string idle_since { get; set; }
        public string policy { get; set; }
        public string exclusive_consumer_tag { get; set; }
        public int messages_ready { get; set; }
        public int messages_unacknowledged { get; set; }
        public int messages { get; set; }
        public int consumers { get; set; }
        public int active_consumers { get; set; }
        public BackingQueueStatus backing_queue_status { get; set; }
        public List<object> consumer_details { get; set; }
        public string name { get; set; }
        public string vhost { get; set; }
        public bool durable { get; set; }
        public bool auto_delete { get; set; }
        public Arguments arguments { get; set; }
        public string node { get; set; }
    }
}