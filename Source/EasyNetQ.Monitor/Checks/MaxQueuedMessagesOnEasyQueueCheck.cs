using System.Linq;
using EasyNetQ.Management.Client;

namespace EasyNetQ.Monitor.Checks
{
    public class MaxQueuedMessagesOnEasyQueueCheck : ICheck
    {
        private readonly int alertIndividualQueueMessagesCount;
        private const string alertMessage = "Broker '{0}', Queues '{1}' " +
                "have exceeded the maximum number of allowed messages {2}.";

        public MaxQueuedMessagesOnEasyQueueCheck(int alertIndividualQueueMessagesCount)
        {
            this.alertIndividualQueueMessagesCount = alertIndividualQueueMessagesCount;
        }

        public CheckResult RunCheck(IManagementClient managementClient)
        {
            var queues = managementClient.GetQueues();
            var alertQueues =
                from queue in queues
                where queue.messages >= alertIndividualQueueMessagesCount
                select string.Format("{0} on {1} with {2} messages", queue.name, queue.vhost, queue.messages);

            var message = string.Format(alertMessage, 
                managementClient.HostUrl,
                string.Join(", ", alertQueues),
                alertIndividualQueueMessagesCount
                );

            return new CheckResult(alertQueues.Any(), message);
        }
    }
}