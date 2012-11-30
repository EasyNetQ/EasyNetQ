using System;
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
        private readonly IEnumerable<IAlertSink> alertSinks;

        public MonitorRun(
            IEnumerable<Broker> brokers, 
            IEnumerable<ICheck> checks, 
            IManagementClientFactory managementClientFactory, 
            IEnumerable<IAlertSink> alertSinks)
        {
            this.brokers = brokers;
            this.checks = checks;
            this.managementClientFactory = managementClientFactory;
            this.alertSinks = alertSinks;
        }

        public void Run()
        {
            var actions =
                from broker in brokers
                from check in checks
                from sink in alertSinks
                let managementClient = managementClientFactory.CreateManagementClient(broker)
                let runResult = check.RunCheck(managementClient)
                where runResult.Alert
                let alert = runResult.Message
                select (Action)(() => sink.Alert(alert));

            foreach (var action in actions)
            {
                action();
            }
        }
    }


}