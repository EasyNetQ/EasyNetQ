namespace EasyNetQ.Management.Client.Model
{
    public class Listener
    {
        public string node { get; set; }
        public string protocol { get; set; }
        public string ip_address { get; set; }
        public int port { get; set; }
    }
}