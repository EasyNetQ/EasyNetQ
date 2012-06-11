using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using log4net;

namespace EasyNetQ.SagaHost
{
    public class WindsorSagaHost : ISagaHost
    {
        private readonly string sagaDirectory;
        private readonly IBus bus;
        private readonly ILog log;

        private IWindsorContainer container;

        public WindsorSagaHost(IBus bus, ILog log, string sagaDirectory)
        {
            if (bus == null)
            {
                throw new ArgumentNullException("bus");
            }
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }
            if (sagaDirectory == null)
            {
                throw new ArgumentNullException("sagaDirectory");
            }
            
            this.sagaDirectory = sagaDirectory;
            this.bus = bus;
            this.log = log;
        }

        public void Start()
        {
			try
			{
				container = new WindsorContainer()
					.Install(FromAssembly.InDirectory(new AssemblyFilter(sagaDirectory)));

				var sagas = container.ResolveAll<ISaga>();
				sagas.LogWith(log).InitializeWith(bus, log);
			}
			catch (Exception ex)
			{
				log.Error("WindsorSagaHost.Start: Exception occured", ex);
				throw;
			}
        }

        public void Stop()
        {
            if(container != null) container.Dispose();
        }
    }
}