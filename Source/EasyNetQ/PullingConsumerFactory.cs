using EasyNetQ.Interception;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     A factory of pulling consumer
    /// </summary>
    public interface IPullingConsumerFactory
    {
        /// <summary>
        ///     Creates a pulling consumer
        /// </summary>
        /// <param name="queue">The queue</param>
        /// <param name="options">The options</param>
        /// <returns></returns>
        IPullingConsumer CreateConsumer(IQueue queue, PullingConsumerOptions options);
    }

    /// <inheritdoc />
    public class PullingConsumerFactory : IPullingConsumerFactory
    {
        private readonly IPersistentChannelFactory channelFactory;
        private readonly IProduceConsumeInterceptor produceConsumeInterceptor;

        /// <summary>
        ///     Creates PullingConsumerFactory
        /// </summary>
        /// <param name="channelFactory">The channel factory</param>
        /// <param name="produceConsumeInterceptor">The produce-consume interceptor</param>
        public PullingConsumerFactory(
            IPersistentChannelFactory channelFactory, IProduceConsumeInterceptor produceConsumeInterceptor
        )
        {
            this.channelFactory = channelFactory;
            this.produceConsumeInterceptor = produceConsumeInterceptor;
        }

        /// <inheritdoc />
        public IPullingConsumer CreateConsumer(IQueue queue, PullingConsumerOptions options)
        {
            var channel = channelFactory.CreatePersistentChannel(new PersistentChannelOptions());
            return new PullingConsumer(queue, options, channel, produceConsumeInterceptor);
        }
    }
}
