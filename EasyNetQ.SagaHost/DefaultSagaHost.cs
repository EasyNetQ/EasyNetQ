using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
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
            if(bus == null)
            {
                throw new ArgumentNullException("bus");
            }
            if(log == null)
            {
                throw new ArgumentNullException("log");
            }
            if(sagaDirectory == null)
            {
                throw new ArgumentNullException("sagaDirectory");
            }

            this.bus = bus;
            this.log = log;
            this.sagaDirectory = sagaDirectory;
        }

        public void Start()
        {
            UrlZoneService.ClearUrlZonesInDirectory(sagaDirectory);

            var assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var directoryCatalog = new DirectoryCatalog(sagaDirectory);

            LogDirectoryCatalogueInfo(directoryCatalog);

            var catalog = new AggregateCatalog(assemblyCatalog, directoryCatalog); 

            container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            log.Debug("MEF container initialized");
        }

        private void LogDirectoryCatalogueInfo(DirectoryCatalog directoryCatalog)
        {
            log.Debug(string.Format("MEF DirectoryCatalog is probing '{0}'", directoryCatalog.FullPath));
            log.Debug("Listing *.dll files in Saga directory ....");
            foreach (var file in Directory.EnumerateFiles(directoryCatalog.FullPath, "*.dll"))
            {
                log.Debug(file);

                var fileInfo = new FileInfo(file);
                
            }
            log.Debug("");

            
        }

        public void Stop()
        {
            bus.Dispose();

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