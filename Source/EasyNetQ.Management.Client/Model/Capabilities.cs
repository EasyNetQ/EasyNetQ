namespace EasyNetQ.Management.Client.Model
{
    public class Capabilities
    {
        public bool BasicNack { get; set; }
        public bool PublisherConfirms { get; set; }
        public bool ConsumerCancelNotify { get; set; }
        public bool ExchangeExchangeBindings { get; set; }
    }
}