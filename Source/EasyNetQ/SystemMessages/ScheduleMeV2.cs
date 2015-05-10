using System;

namespace EasyNetQ.SystemMessages
{
    [Serializable]
    public class ScheduleMeV2
    {
        public DateTime WakeTime { get; set; }
        public string CancellationKey { get; set; }
        public string Exchange { get; set; }
        public string ExchangeType { get; set; }
        public string RoutingKey { get; set; }
        public byte[] Message { get; set; }
        public MessageProperties MessageProperties { get; set; }
    }
}