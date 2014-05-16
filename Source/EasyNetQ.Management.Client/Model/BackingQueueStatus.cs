using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class BackingQueueStatus
    {
        public int Q1 { get; set; }
        public int Q2 { get; set; }
        public List<object> Delta { get; set; }
        public int Q3 { get; set; }
        public int Q4 { get; set; }
        public int Len { get; set; }
        public int PendingAcks { get; set; }
        public string TargetRamCount { get; set; }
        public int RamMsgCount { get; set; }
        public int RamAckCount { get; set; }
        public long NextSeqId { get; set; }
        public int PersistentCount { get; set; }
        public double AvgIngressRate { get; set; }
        public double AvgEgressRate { get; set; }
        public double AvgAckIngressRate { get; set; }
        public double AvgAckEgressRate { get; set; }
    }
}