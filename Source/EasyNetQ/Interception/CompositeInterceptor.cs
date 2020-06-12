using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Interception
{
    /// <inheritdoc />
    public class CompositeInterceptor : IProduceConsumeInterceptor
    {
        private readonly List<IProduceConsumeInterceptor> interceptors = new List<IProduceConsumeInterceptor>();

        /// <inheritdoc />
        public ProducedMessage OnProduce(ProducedMessage message)
        {
            return interceptors.AsEnumerable()
                               .Aggregate(message, (x, y) => y.OnProduce(x));
        }

        /// <inheritdoc />
        public ConsumedMessage OnConsume(ConsumedMessage message)
        {
            return interceptors.AsEnumerable()
                               .Reverse()
                               .Aggregate(message, (x, y) => y.OnConsume(x));
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
