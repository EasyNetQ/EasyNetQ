namespace EasyNetQ
{
    public class MessageReceivedInfo
    {
        public string ConsumerTag { get; set; }
        public ulong DeliverTag { get; set; }
        public bool Redelivered { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }         
    }
}