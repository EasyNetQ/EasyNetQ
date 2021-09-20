using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Interception
{
    /// <inheritdoc />
    public class CompositeInterceptor : IProduceConsumeInterceptor
    {
        private readonly List<IProduceConsumeInterceptor> interceptors = new();

        /// <inheritdoc />
        public ProducedMessage OnProduce(in ProducedMessage message)
        {
            return interceptors.AsEnumerable()
                               .Aggregate(message, (x, y) => y.OnProduce(x));
        }

        /// <inheritdoc />
        public void OnProduced(in ProducedMessage message)
        {
            foreach (var i in interceptors)
                i.OnProduced(in message);
        }

        /// <inheritdoc />
        public ConsumedMessage OnConsume(in ConsumedMessage message)
        {
            return interceptors.AsEnumerable()
                               .Reverse()
                               .Aggregate(message, (x, y) => y.OnConsume(x));
        }

        /// <inheritdoc />
        public void OnConsumed(in ConsumedMessage message)
        {
            foreach (var i in interceptors.AsEnumerable().Reverse())
                i.OnConsumed(in message);
        }

        /// <summary>
        ///     Add the interceptor to pipeline
        /// </summary>
        /// <param name="interceptor"></param>
        public void Add(IProduceConsumeInterceptor interceptor)
        {
            interceptors.Add(interceptor);
        }
    }
}
