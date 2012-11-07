using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Definitions
    {
        public string rabbit_version { get; set; }
        public List<User> users { get; set; }
        public List<Vhost> vhosts { get; set; }
        public List<Permission> permissions { get; set; }
        public List<Queue> queues { get; set; }
        public List<Exchange> exchanges { get; set; }
        public List<Binding> bindings { get; set; }
    }
}