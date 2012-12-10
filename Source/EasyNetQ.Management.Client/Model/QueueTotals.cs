namespace EasyNetQ.Management.Client.Model
{
    public class QueueTotals
    {
        public int Messages { get; set; }
        public int MessagesReady { get; set; }
        public int MessagesUnacknowledged { get; set; }
        public MessagesDetails MessagesDetails { get; set; }
        public MessagesReadyDetails MessagesReadyDetails { get; set; }
        public MessagesUnacknowledgedDetails MessagesUnacknowledgedDetails { get; set; }
    }
}