using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class Node
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Running { get; set; }
        public string OsPid { get; set; }
        public int MemEts { get; set; }
        public int MemBinary { get; set; }
        public int MemProc { get; set; }
        public int MemProcUsed { get; set; }
        public int MemAtom { get; set; }
        public int MemAtomUsed { get; set; }
        public int MemCode { get; set; }
        public string FdUsed { get; set; }
        public int FdTotal { get; set; }
        public int SocketsUsed { get; set; }
        public int SocketsTotal { get; set; }
        public int MemUsed { get; set; }
        public long MemLimit { get; set; }
        public bool MemAlarm { get; set; }
        public int DiskFreeLimit { get; set; }
        public long DiskFree { get; set; }
        public bool DiskFreeAlarm { get; set; }
        public int ProcUsed { get; set; }
        public int ProcTotal { get; set; }
        public string StatisticsLevel { get; set; }
        public string ErlangVersion { get; set; }
        public int Uptime { get; set; }
        public int RunQueue { get; set; }
        public int Processors { get; set; }
        public List<ExchangeType> ExchangeTypes { get; set; }
        public List<AuthMechanism> AuthMechanisms { get; set; }
        public List<Application> Applications { get; set; }
    }
}