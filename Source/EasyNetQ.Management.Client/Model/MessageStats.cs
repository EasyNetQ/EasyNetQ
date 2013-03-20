using System;

namespace EasyNetQ.Management.Client.Model
{
    public class MessageStats
    {
        public Int64 DeliverGet { get; set; }
        public DeliverGetDetails DeliverGetDetails { get; set; }
        public Int64 DeliverNoAck { get; set; }
        public DeliverNoAckDetails DeliverNoAckDetails { get; set; }
        public Int64 Publish { get; set; }
        public PublishDetails PublishDetails { get; set; }
    }
}
