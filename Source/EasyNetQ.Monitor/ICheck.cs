using EasyNetQ.Management.Client;

namespace EasyNetQ.Monitor
{
    public interface ICheck
    {
        CheckResult RunCheck(IManagementClient managementClient);
    }

    public class CheckResult
    {
        public bool Alert { get; private set; }
        public string Message { get; private set; }

        public CheckResult(bool alert, string message)
        {
            Alert = alert;
            Message = message;
        }
    }
}