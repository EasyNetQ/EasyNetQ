namespace EasyNetQ.Management.Client.Model
{
    public class ConnectionDetails
    {
        public string Name { get; set; }
        //PeerAddress is the connected Peer IP Address for RabbitMQ Version <3.0
        public string PeerAddress { get; set; }
        //PeerAddress is the connected Peer IP Address for RabbitMQ Version >3.0
        public string PeerHost { get; set; }
        public int PeerPort { get; set; }
    }
}