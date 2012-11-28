using EasyNetQ.Management.Client;

namespace EasyNetQ.Monitor.Checks
{
    public class MaxConnectionsCheck : ICheck
    {
        public CheckResult RunCheck(
            IManagementClient managementClient, 
            MonitorConfigurationSection configuration, 
            Broker broker)
        {
            var overview = managementClient.GetOverview();
            var alert = (ulong)overview.object_totals.connections >= configuration.CheckSettings.AlertConnectionCount;
            var message = alert
                ? string.Format(
                    "broker {0} connections have exceeded alert level {1}. Now {2}", 
                    broker.ManagementUrl,
                    configuration.CheckSettings.AlertConnectionCount,
                    overview.object_totals.connections)
                : "";

            return new CheckResult(alert, message);
        }
    }
}