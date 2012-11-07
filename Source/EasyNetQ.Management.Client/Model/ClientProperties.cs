namespace EasyNetQ.Management.Client.Model
{
    public class ClientProperties
    {
        public string copyright { get; set; }
        public string information { get; set; }
        public string version { get; set; }
        public string platform { get; set; }
        public string product { get; set; }
        public Capabilities capabilities { get; set; }
    }
}