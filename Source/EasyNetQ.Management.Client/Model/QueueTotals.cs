namespace EasyNetQ.Management.Client.Model
{
    public class QueueTotals
    {
        public int messages { get; set; }
        public int messages_ready { get; set; }
        public int messages_unacknowledged { get; set; }
        public MessagesDetails messages_details { get; set; }
        public MessagesReadyDetails messages_ready_details { get; set; }
        public MessagesUnacknowledgedDetails messages_unacknowledged_details { get; set; }
    }
}