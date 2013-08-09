using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Overview
    {
        public string ManagementVersion { get; set; }
        public string StatisticsLevel { get; set; }
        public List<ExchangeType> ExchangeTypes { get; set; }
        public string RabbitmqVersion { get; set; }
        public string ErlangVersion { get; set; }
        public MessageStats MessageStats { get; set; }
        public QueueTotals QueueTotals { get; set; }
        public ObjectTotals ObjectTotals { get; set; }
        public string Node { get; set; }
        public string StatisticsDbNode { get; set; }
        public List<Listener> Listeners { get; set; }
        public List<Context> Contexts { get; set; }
    }

    public class ObjectTotals
    {
        public int Consumers { get; set; }
        public int Queues { get; set; }
        public int Exchanges { get; set; }
        public int Connections { get; set; }
        public int Channels { get; set; }
    }
}