using System;

namespace EasyNetQ.Management.Client.Model
{
    public class ClientProperties
    {
        public Capabilities Capabilities { get; set; }
        public string User { get; set; }
        public string Application { get; set; }
        public string ClientApi { get; set; }
        public string ApplicationLocation { get; set; }
        public DateTime Connected { get; set; }
        public string EasynetqVersion { get; set; }
        public string MachineName { get; set; }
    }
}