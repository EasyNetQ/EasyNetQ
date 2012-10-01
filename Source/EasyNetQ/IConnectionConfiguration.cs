namespace EasyNetQ
{
    public interface IConnectionConfiguration
    {
        string Host { get; }
        ushort Port { get; }
        string VirtualHost { get; }
        string UserName { get; }
        string Password { get; }
        ushort RequestedHeartbeat { get; }
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        public string Host { get; set; }
        public ushort Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort RequestedHeartbeat { get; set; }

        public ConnectionConfiguration()
        {
            // set default values
            Port = 5672;
            VirtualHost = "/";
            UserName = "guest";
            Password = "guest";
            RequestedHeartbeat = 0;
        }

        public void Validate()
        {
            if (Host == null)
            {
                throw new EasyNetQException("Invalid connection string. 'host' value must be supplied. e.g: \"host=myserver\"");
            }
        }
    }
}