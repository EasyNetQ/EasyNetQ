using System;
using EasyNetQ.Management.Client;
using log4net;

namespace EasyNetQ.Monitor.Checks
{
    public class MaxConnectionsCheck : ICheck
    {
        private readonly int alertConnectionCount;
        private readonly ILog log;

        public MaxConnectionsCheck(int alertConnectionCount, ILog log)
        {
            this.alertConnectionCount = alertConnectionCount;
            this.log = log;
        }

        public CheckResult RunCheck(IManagementClient managementClient)
        {
            var overview = managementClient.GetOverview();
            var alert = overview.object_totals.connections >= alertConnectionCount;
            var message = alert
                ? string.Format(
                    "broker {0} connections have exceeded alert level {1}. Now {2}", 
                    managementClient.HostUrl,
                    alertConnectionCount,
                    overview.object_totals.connections)
                : "";

            return new CheckResult(alert, message);
        }
    }
}