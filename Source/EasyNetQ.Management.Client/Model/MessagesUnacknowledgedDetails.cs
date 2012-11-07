namespace EasyNetQ.Management.Client.Model
{
    public class MessagesUnacknowledgedDetails
    {
        public long rate { get; set; }
        public long longerval { get; set; }
        public long last_event { get; set; }
    }
}