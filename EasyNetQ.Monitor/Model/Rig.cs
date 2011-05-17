using System.Collections.Generic;

namespace EasyNetQ.Monitor.Model
{
    public class Rig
    {
        public IList<VHost> VHosts { get; protected set; }

        public Rig()
        {
            VHosts = new List<VHost>();
        }

        public VHost AddVHost(string name)
        {
            var vHost = new VHost(name);
            VHosts.Add(vHost);
            return vHost;
        }
    }
}