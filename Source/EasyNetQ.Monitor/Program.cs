using Castle.Windsor;
using Castle.Windsor.Installer;
using Topshelf;

namespace EasyNetQ.Monitor
{
    public class Program
    {
        static void Main()
        {
            HostFactory.Run(x =>
            {
                x.Service<IMonitorService>(s =>
                {
                    IWindsorContainer container = null;
              
                    s.ConstructUsing(name =>
                    {
                        container = new WindsorContainer().Install(FromAssembly.This());
                        return container.Resolve<IMonitorService>();
                    });
                    
                    s.WhenStarted(tc => tc.Start());
                    
                    s.WhenStopped(tc =>
                    {
                        tc.Stop();
                        if (container != null)
                        {
                            container.Release(tc);
                            container.Dispose();
                        }
                    });
                });

                x.RunAsLocalSystem();

                x.SetDescription("EasyNetQ.Monitor - monitors RabbitMQ brokers");
                x.SetDisplayName("EasyNetQ.Monitor");
                x.SetServiceName("EasyNetQ.Monitor");
            });
        }
    }
}
