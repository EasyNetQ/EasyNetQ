using System.Collections.Generic;
using System.Linq;
using EasyNetQ.Monitor.AlertSinks;
using EasyNetQ.Monitor.Checks;
using Topshelf;

namespace EasyNetQ.Monitor
{
    public class Program
    {
        static void Main()
        {
            HostFactory.Run(x =>
            {
                x.Service<MonitorService>(s =>
                {
                    s.ConstructUsing(name => CreateMonitorService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();

                x.SetDescription("EasyNetQ.Monitor - monitors RabbitMQ brokers");
                x.SetDisplayName("EasyNetQ.Monitor");
                x.SetServiceName("EasyNetQ.Monitor");
            });
        }

        private static MonitorService CreateMonitorService()
        {
            var configuration = MonitorConfigurationSection.Settings;
            var monitorRun = new MonitorRun(
                GetBrokers(configuration), 
                GetChecks(), 
                new ManagementClientFactory(),
                configuration, 
                new NullAlertSink());

            return new MonitorService(monitorRun, configuration);
        }

        private static IEnumerable<Broker> GetBrokers(MonitorConfigurationSection configuration)
        {
            return configuration.Brokers.Cast<object>().Cast<Broker>();
        }

        private static IEnumerable<ICheck> GetChecks()
        {
            yield return new MaxConnectionsCheck();
        }
    }

    
}
