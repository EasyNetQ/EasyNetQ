using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Definitions
    {
        public string RabbitVersion { get; set; }
        public List<User> Users { get; set; }
        public List<Vhost> Vhosts { get; set; }
        public List<Permission> Permissions { get; set; }
        public List<Queue> Queues { get; set; }
        public List<Exchange> Exchanges { get; set; }
        public List<Binding> Bindings { get; set; }
    }
}