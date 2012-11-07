namespace EasyNetQ.Management.Client.Model
{
    public class MessagesReadyDetails
    {
        public long rate { get; set; }
        public long longerval { get; set; }
        public long last_event { get; set; }
    }
}