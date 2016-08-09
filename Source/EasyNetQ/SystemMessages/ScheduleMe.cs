using System;

namespace EasyNetQ.SystemMessages
{
#if !DOTNET5_4
#endif
    public class ScheduleMe
    {
        public DateTime WakeTime { get; set; }
        public string CancellationKey { get; set; }
        public string Exchange { get; set; }
        public string ExchangeType { get; set; }
        public string RoutingKey { get; set; }
        public byte[] InnerMessage { get; set; }
        public MessageProperties MessageProperties { get; set; }

        [Obsolete]
        public string BindingKey { get; set; }
    }
}