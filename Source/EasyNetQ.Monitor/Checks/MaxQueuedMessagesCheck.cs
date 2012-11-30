using EasyNetQ.Management.Client;

namespace EasyNetQ.Monitor.Checks
{
    public class MaxQueuedMessagesCheck : ICheck
    {
        private readonly int alertQueueCount;

        public MaxQueuedMessagesCheck(int alertQueueCount)
        {
            this.alertQueueCount = alertQueueCount;
        }

        public CheckResult RunCheck(IManagementClient managementClient)
        {
            var overview = managementClient.GetOverview();
            var alert = overview.queue_totals.messages >= alertQueueCount;
            var message = string.Format("broker '{0}' queued messages exceed alert level {1}, now {2}",
                managementClient.HostUrl, alertQueueCount, overview.queue_totals.messages);

            return new CheckResult(alert, message);
        }
    }
}