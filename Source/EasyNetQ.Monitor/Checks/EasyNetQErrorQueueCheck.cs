using System.Linq;
using System.Net;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;

namespace EasyNetQ.Monitor.Checks
{
    public class EasyNetQErrorQueueCheck : ICheck
    {
        private const string errorMessage = "broker '{0}', VHost(s): '{1}' has messages on the EasyNetQ error queue.";
        private const string easyNetQErrorQueue = "EasyNetQ_Default_Error_Queue";

        public CheckResult RunCheck(IManagementClient managementClient)
        {
            var vhostsWithErrors =
                from vhost in managementClient.GetVHosts()
                let errorQueue = GetErrorQueue(managementClient, vhost)
                where errorQueue.Messages > 0
                select vhost.Name;

            var message = string.Format(errorMessage, managementClient.HostUrl, string.Join(", ", vhostsWithErrors));

            return new CheckResult(vhostsWithErrors.Any(), message);
        }

        private Queue GetErrorQueue(IManagementClient managementClient, Vhost vhost)
        {
            try
            {
                return managementClient.GetQueue(easyNetQErrorQueue, vhost);
            }
            catch (UnexpectedHttpStatusCodeException exception)
            {
                if (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    return new Queue { Messages = 0 };
                }
                throw;
            }
        }
    }
}