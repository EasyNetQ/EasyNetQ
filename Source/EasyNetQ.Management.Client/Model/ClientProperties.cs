using System;

namespace EasyNetQ.Management.Client.Model
{
    public class ClientProperties
    {
        public Capabilities capabilities { get; set; }
        public string user { get; set; }
        public string application { get; set; }
        public string client_api { get; set; }
        public string application_location { get; set; }
        public DateTime connected { get; set; }
        public string easynetq_version { get; set; }
        public string machine_name { get; set; }
    }
}