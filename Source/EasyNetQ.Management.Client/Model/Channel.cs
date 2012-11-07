namespace EasyNetQ.Management.Client.Model
{
    public class Channel
    {
        public ConnectionDetails connection_details { get; set; }
        public string idle_since { get; set; }
        public bool transactional { get; set; }
        public bool confirm { get; set; }
        public int consumer_count { get; set; }
        public int messages_unacknowledged { get; set; }
        public int messages_unconfirmed { get; set; }
        public int messages_uncommitted { get; set; }
        public int acks_uncommitted { get; set; }
        public int prefetch_count { get; set; }
        public bool client_flow_blocked { get; set; }
        public string node { get; set; }
        public string name { get; set; }
        public int number { get; set; }
        public string user { get; set; }
        public string vhost { get; set; }
    }
}