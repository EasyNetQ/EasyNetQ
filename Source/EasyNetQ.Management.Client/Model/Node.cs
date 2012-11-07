using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Node
    {
        public string name { get; set; }
        public string type { get; set; }
        public bool running { get; set; }
        public string os_pid { get; set; }
        public int mem_ets { get; set; }
        public int mem_binary { get; set; }
        public int mem_proc { get; set; }
        public int mem_proc_used { get; set; }
        public int mem_atom { get; set; }
        public int mem_atom_used { get; set; }
        public int mem_code { get; set; }
        public string fd_used { get; set; }
        public int fd_total { get; set; }
        public int sockets_used { get; set; }
        public int sockets_total { get; set; }
        public int mem_used { get; set; }
        public long mem_limit { get; set; }
        public bool mem_alarm { get; set; }
        public int disk_free_limit { get; set; }
        public long disk_free { get; set; }
        public bool disk_free_alarm { get; set; }
        public int proc_used { get; set; }
        public int proc_total { get; set; }
        public string statistics_level { get; set; }
        public string erlang_version { get; set; }
        public int uptime { get; set; }
        public int run_queue { get; set; }
        public int processors { get; set; }
        public List<ExchangeType> exchange_types { get; set; }
        public List<AuthMechanism> auth_mechanisms { get; set; }
        public List<Application> applications { get; set; }
    }
}