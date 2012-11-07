namespace EasyNetQ.Management.Client.Model
{
    public class Capabilities
    {
        public bool basic_nack { get; set; }
        public bool publisher_confirms { get; set; }
        public bool consumer_cancel_notify { get; set; }
        public bool exchange_exchange_bindings { get; set; }
    }
}