using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Overview
    {
        public string management_version { get; set; }
        public string statistics_level { get; set; }
        public List<ExchangeType> exchange_types { get; set; }
        public List<object> message_stats { get; set; }
        public QueueTotals queue_totals { get; set; }
        public string node { get; set; }
        public string statistics_db_node { get; set; }
        public List<Listener> listeners { get; set; }
        public List<Context> contexts { get; set; }
    }
}