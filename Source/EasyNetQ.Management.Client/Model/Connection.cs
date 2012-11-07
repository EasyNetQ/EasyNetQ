namespace EasyNetQ.Management.Client.Model
{
    public class Connection
    {
        public int recv_oct { get; set; }
        public int recv_cnt { get; set; }
        public int send_oct { get; set; }
        public int send_cnt { get; set; }
        public int send_pend { get; set; }
        public string state { get; set; }
        public string last_blocked_by { get; set; }
        public string last_blocked_age { get; set; }
        public int channels { get; set; }
        public string type { get; set; }
        public string node { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public int port { get; set; }
        public string peer_address { get; set; }
        public int peer_port { get; set; }
        public bool ssl { get; set; }
        public string peer_cert_subject { get; set; }
        public string peer_cert_issuer { get; set; }
        public string peer_cert_validity { get; set; }
        public string auth_mechanism { get; set; }
        public string ssl_protocol { get; set; }
        public string ssl_key_exchange { get; set; }
        public string ssl_cipher { get; set; }
        public string ssl_hash { get; set; }
        public string protocol { get; set; }
        public string user { get; set; }
        public string vhost { get; set; }
        public int timeout { get; set; }
        public int frame_max { get; set; }
        public ClientProperties client_properties { get; set; }
    }
}