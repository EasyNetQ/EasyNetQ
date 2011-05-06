using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using log4net;

namespace EasyNetQ.SagaHost
{
    public class DefaultSagaHost : ISagaHost
    {
        private readonly string sagaDirectory;
        private readonly IBus bus;
        private readonly ILog log;
        private CompositionContainer container;

        public DefaultSagaHost(IBus bus, ILog log, string sagaDirectory)
        {
            this.bus = bus;
            this.log = log;
            this.sagaDirectory = sagaDirectory;
        }

        public void Start()
        {
            var assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var directoryCatalog = new DirectoryCatalog(sagaDirectory);
            var catalog = new AggregateCatalog(assemblyCatalog, directoryCatalog); 

            container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            log.Debug("MEF container initialized");
        }

        public void Stop()
        {
            if (container != null)
            {
                container.Dispose();
            }

            log.Debug("Stopped DefaultSagaHost");
        }

        private IEnumerable<ISaga> sagas;

        [ImportMany]
        public IEnumerable<ISaga> Sagas
        {
            get { return sagas; }
            set
            {
                log.Debug("Loading Sagas ...");
                sagas = value;
                sagas.LogWith(log).InitializeWith(bus);
            }
        }
    }
}