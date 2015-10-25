using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Interception
{
    public class CompositeInterceptor : IProduceConsumeInterceptor
    {
        private readonly List<IProduceConsumeInterceptor> interceptors = new List<IProduceConsumeInterceptor>();

        public RawMessage OnProduce(RawMessage rawMessage)
        {
            return interceptors.AsEnumerable()
                               .Aggregate(rawMessage, (x, y) => y.OnProduce(x));
        }

        public RawMessage OnConsume(RawMessage rawMessage)
        {
            return interceptors.AsEnumerable()
                               .Reverse()
                               .Aggregate(rawMessage, (x, y) => y.OnConsume(x));
        }

        public void Add(IProduceConsumeInterceptor interceptor)
        {
            interceptors.Add(interceptor);
        }
    }
}