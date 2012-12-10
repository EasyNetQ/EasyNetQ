namespace EasyNetQ.Management.Client.Model
{
    public class Permission
    {
        public string User { get; set; }
        public string Vhost { get; set; }
        public string Configure { get; set; }
        public string Write { get; set; }
        public string Read { get; set; }
    }
}