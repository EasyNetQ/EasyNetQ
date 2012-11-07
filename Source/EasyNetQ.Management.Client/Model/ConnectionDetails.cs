namespace EasyNetQ.Management.Client.Model
{
    public class ConnectionDetails
    {
        public string name { get; set; }
        public string peer_address { get; set; }
        public int peer_port { get; set; }
    }
}