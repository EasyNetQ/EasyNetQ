using System.Configuration;

namespace EasyNetQ.Monitor
{
    public class MonitorConfigurationSection : ConfigurationSection
    {
        private static readonly MonitorConfigurationSection settings =
            ConfigurationManager.GetSection("monitor") as MonitorConfigurationSection;

        [ConfigurationProperty("intervalMinutes", IsRequired = true)]
        public double IntervalMinutes
        {
            get { return (double) this["intervalMinutes"]; }
            set { this["intervalMinutes"] = value; }
        }

        public static MonitorConfigurationSection Settings
        {
            get { return settings; }        
        }

        [ConfigurationProperty("brokers", IsDefaultCollection = false)]
        public Brokers Brokers
        {
            get { return (Brokers) this["brokers"]; }
        }

        [ConfigurationProperty("checkSettings")]
        public CheckSettings CheckSettings
        {
            get { return (CheckSettings) this["checkSettings"]; }
            set { this["checkSettings"] = value; }
        }
    }

    public class Brokers : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Broker();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Broker) element).ManagementUrl;
        }
    }

    public class Broker : ConfigurationElement
    {
        [ConfigurationProperty("managementUrl", IsRequired = true, IsKey = true)]
        public string ManagementUrl
        {
            get { return (string) this["managementUrl"]; }
            set { this["managementUrl"] = value; }
        }

        [ConfigurationProperty("username", IsRequired = true)]
        public string Username
        {
            get { return (string) this["username"]; }
            set { this["username"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password
        {
            get { return (string) this["password"]; }
            set { this["password"] = value; }
        }
    }

    public class CheckSettings : ConfigurationElement
    {
        [ConfigurationProperty("alertQueueCount")]
        public ulong AlertQueueCount
        {
            get { return (ulong)this["alertQueueCount"]; }
            set { this["alertQueueCount"] = value; }
        }

        [ConfigurationProperty("alertConnectionCount")]
        public ulong AlertConnectionCount
        {
            get { return (ulong) this["alertConnectionCount"]; }
            set { this["alertConnectionCount"] = value; }
        }

        [ConfigurationProperty("alertChannelCount")]
        public ulong AlertChannelCount
        {
            get { return (ulong)this["alertChannelCount"]; }
            set { this["alertChannelCount"] = value; }
        }
    }
}