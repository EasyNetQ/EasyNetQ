namespace EasyNetQ.Management.Client.Model
{
    public class Permission
    {
        public string user { get; set; }
        public string vhost { get; set; }
        public string configure { get; set; }
        public string write { get; set; }
        public string read { get; set; }
    }
}