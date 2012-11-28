using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Overview
    {
        public string management_version { get; set; }
        public string statistics_level { get; set; }
        public List<ExchangeType> exchange_types { get; set; }
        public string rabbitmq_version { get; set; }
        public string erlang_version { get; set; }
        public List<object> message_stats { get; set; }
        public QueueTotals queue_totals { get; set; }
        public ObjectTotals object_totals { get; set; }
        public string node { get; set; }
        public string statistics_db_node { get; set; }
        public List<Listener> listeners { get; set; }
        public List<Context> contexts { get; set; }
    }

    public class ObjectTotals
    {
        public int consumers { get; set; }
        public int queues { get; set; }
        public int exchanges { get; set; }
        public int connections { get; set; }
        public int channels { get; set; }
    }
}