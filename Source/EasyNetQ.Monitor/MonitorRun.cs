using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Monitor
{
    public interface IMonitorRun
    {
        void Run();
    }

    public class MonitorRun : IMonitorRun
    {
        private readonly IEnumerable<Broker> brokers;
        private readonly IEnumerable<ICheck> checks;
        private readonly IManagementClientFactory managementClientFactory;
        private readonly MonitorConfigurationSection configuration;
        private readonly IAlertSink alertSink;

        public MonitorRun(
            IEnumerable<Broker> brokers, 
            IEnumerable<ICheck> checks, 
            IManagementClientFactory managementClientFactory, 
            MonitorConfigurationSection configuration, 
            IAlertSink alertSink)
        {
            this.brokers = brokers;
            this.checks = checks;
            this.managementClientFactory = managementClientFactory;
            this.configuration = configuration;
            this.alertSink = alertSink;
        }

        public void Run()
        {
            var alerts =
                from broker in brokers
                let managementClient = managementClientFactory.CreateManagementClient(broker)
                from check in checks
                let runResult = check.RunCheck(managementClient, configuration, broker)
                where runResult.Alert
                select runResult.Message;

            foreach (var alert in alerts)
            {
                alertSink.Alert(alert);
            }
        }
    }


}