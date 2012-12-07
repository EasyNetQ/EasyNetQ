namespace EasyNetQ.Management.Client.Model
{
    public class MessagesUnacknowledgedDetails
    {
        public long Rate { get; set; }
        public long Longerval { get; set; }
        public long LastEvent { get; set; }
    }
}