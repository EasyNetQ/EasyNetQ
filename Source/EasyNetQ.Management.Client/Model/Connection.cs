using System;
using System.Dynamic;

namespace EasyNetQ.Management.Client.Model
{
    public class Connection
    {
        public Int64 RecvOct { get; set; }
        public Int64 RecvCnt { get; set; }
        public Int64 SendOct { get; set; }
        public Int64 SendCnt { get; set; }
        public Int64 SendPend { get; set; }
        public string State { get; set; }
        public string LastBlockedBy { get; set; }
        public string LastBlockedAge { get; set; }
        public Int64 Channels { get; set; }
        public string Type { get; set; }
        public string Node { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Int64 Port { get; set; }
        public string PeerAddress { get; set; }
        public Int64 PeerPort { get; set; }
        public bool Ssl { get; set; }
        public string PeerCertSubject { get; set; }
        public string PeerCertIssuer { get; set; }
        public string PeerCertValidity { get; set; }
        public string AuthMechanism { get; set; }
        public string SslProtocol { get; set; }
        public string SslKeyExchange { get; set; }
        public string SslCipher { get; set; }
        public string SslHash { get; set; }
        public string Protocol { get; set; }
        public string User { get; set; }
        public string Vhost { get; set; }
        public Int64 Timeout { get; set; }
        public Int64 FrameMax { get; set; }
        public ClientProperties ClientProperties { get; set; }
    }
}