namespace EasyNetQ.Management.Client.Model
{
    public class Queue
    {
        public string name { get; set; }
        public string vhost { get; set; }
        public bool durable { get; set; }
        public bool auto_delete { get; set; }
        public Arguments arguments { get; set; }
    }
}