using System;
using System.Collections.Generic;

namespace EasyNetQ.Monitor.Model
{
    [Serializable]
    public class Rig
    {
        public IList<VHost> VHosts { get; protected set; }

        public Rig()
        {
            VHosts = new List<VHost>();
        }

        public VHost CreateVHost(string name)
        {
            var vHost = new VHost(name);
            VHosts.Add(vHost);
            return vHost;
        }
    }
}