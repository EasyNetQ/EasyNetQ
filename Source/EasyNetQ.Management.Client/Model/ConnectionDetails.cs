namespace EasyNetQ.Management.Client.Model
{
    public class ConnectionDetails
    {
        public string Name { get; set; }
        public string PeerAddress { get; set; }
        public int PeerPort { get; set; }
    }
}