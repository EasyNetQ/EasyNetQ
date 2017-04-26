using System;

namespace EasyNetQ.SystemMessages
{
    /// <summary>
    /// A wrapper for errored messages
    /// </summary>
    public class Error
    {
        public string RoutingKey { get; set; }
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public string Exception { get; set; }
        public string Message { get; set; }
        public DateTime DateTime { get; set; }
        public MessageProperties BasicProperties { get; set; }
    }
}