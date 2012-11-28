using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class BackingQueueStatus
    {
        public int q1 { get; set; }
        public int q2 { get; set; }
        public List<object> delta { get; set; }
        public int q3 { get; set; }
        public int q4 { get; set; }
        public int len { get; set; }
        public int pending_acks { get; set; }
        public string target_ram_count { get; set; }
        public int ram_msg_count { get; set; }
        public int ram_ack_count { get; set; }
        public int next_seq_id { get; set; }
        public int persistent_count { get; set; }
        public double avg_ingress_rate { get; set; }
        public double avg_egress_rate { get; set; }
        public double avg_ack_ingress_rate { get; set; }
        public double avg_ack_egress_rate { get; set; }
    }
}