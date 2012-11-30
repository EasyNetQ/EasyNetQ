using EasyNetQ.Management.Client;

namespace EasyNetQ.Monitor.Checks
{
    public class MaxChannelsCheck : ICheck
    {
        private readonly int alertChannelCount;

        public MaxChannelsCheck(int alertChannelCount)
        {
            this.alertChannelCount = alertChannelCount;
        }

        public CheckResult RunCheck(IManagementClient managementClient)
        {
            var overview = managementClient.GetOverview();

            var alert = overview.object_totals.channels >= alertChannelCount;

            var message = string.Format("broker '{0}' channels have exceeded alert level {1}, now {2}",
                managementClient.HostUrl, alertChannelCount, overview.object_totals.channels);

            return new CheckResult(alert, message);
        }
    }
}