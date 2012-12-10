namespace EasyNetQ.Management.Client.Model
{
    public class Connection
    {
        public int RecvOct { get; set; }
        public int RecvCnt { get; set; }
        public int SendOct { get; set; }
        public int SendCnt { get; set; }
        public int SendPend { get; set; }
        public string State { get; set; }
        public string LastBlockedBy { get; set; }
        public string LastBlockedAge { get; set; }
        public int Channels { get; set; }
        public string Type { get; set; }
        public string Node { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string PeerAddress { get; set; }
        public int PeerPort { get; set; }
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
        public int Timeout { get; set; }
        public int FrameMax { get; set; }
        public ClientProperties ClientProperties { get; set; }
    }
}