namespace EasyNetQ.Management.Client.Model
{
    public class MessagesDetails
    {
        public long rate { get; set; }
        public long interval { get; set; }
        public long last_event { get; set; }
    }
}