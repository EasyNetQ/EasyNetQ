using System.Collections.Generic;
using log4net;

namespace EasyNetQ.SagaHost
{
    public static class SagaExtensions
    {
        public static void InitializeWith(this IEnumerable<ISaga> sagas, IBus bus)
        {
            foreach (var saga in sagas)
            {
                saga.Initialize(bus);
            }
        }

        public static IEnumerable<ISaga> LogWith(this IEnumerable<ISaga> sagas, ILog log)
        {
            var sagaCount = 0;

            foreach (var saga in sagas)
            {
                sagaCount++;
                log.Debug(string.Format("Loading Saga: {0}", saga.GetType().FullName));
                yield return saga;
            }
            log.Debug(string.Format("Loaded {0} Sagas", sagaCount));
        }
    }
}