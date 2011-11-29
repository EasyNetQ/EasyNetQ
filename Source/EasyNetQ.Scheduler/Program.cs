using System;
using System.IO;
using log4net.Config;
using Topshelf;

namespace EasyNetQ.Scheduler
{
    public class Program
    {
        static void Main()
        {
            XmlConfigurator.Configure();

            HostFactory.Run(hostConfiguration =>
            {
                hostConfiguration.AfterStartingServices(() => Console.WriteLine("Started EasyNetQ.Scheduler"));
                hostConfiguration.AfterStoppingServices(() => Console.WriteLine("Stopped EasyNetQ.Scheduler"));
                // hostConfiguration.EnableDashboard();
                hostConfiguration.RunAsLocalSystem();
                hostConfiguration.SetDescription("EasyNetQ.Scheduler");
                hostConfiguration.SetDisplayName("EasyNetQ.Scheduler");
                hostConfiguration.SetServiceName("EasyNetQ.Scheduler");

                hostConfiguration.Service<ISchedulerService>(serviceConfiguration =>
                {
                    serviceConfiguration.SetServiceName("SchedulerService");
                    serviceConfiguration.ConstructUsing(_ => SchedulerServiceFactory.CreateScheduler());

                    serviceConfiguration.WhenStarted(service => service.Start());
                    serviceConfiguration.WhenStopped(service => service.Stop());
                });
            });
        }
    }
}
