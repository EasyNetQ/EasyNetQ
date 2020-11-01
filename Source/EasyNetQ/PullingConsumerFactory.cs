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
        IPullingConsumer<PullResult> CreateConsumer(IQueue queue, PullingConsumerOptions options);

        /// <summary>
        ///     Creates a pulling consumer
        /// </summary>
        /// <param name="queue">The queue</param>
        /// <param name="options">The options</param>
        /// <returns></returns>
        IPullingConsumer<PullResult<T>> CreateConsumer<T>(IQueue queue, PullingConsumerOptions options);
    }

    /// <inheritdoc />
    public class PullingConsumerFactory : IPullingConsumerFactory
    {
        private readonly IPersistentChannelFactory channelFactory;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;
        private readonly IProduceConsumeInterceptor produceConsumeInterceptor;

        /// <summary>
        ///     Creates PullingConsumerFactory
        /// </summary>
        /// <param name="channelFactory">The channel factory</param>
        /// <param name="produceConsumeInterceptor">The produce-consume interceptor</param>
        /// <param name="messageSerializationStrategy">The message serialization strategy</param>
        public PullingConsumerFactory(
            IPersistentChannelFactory channelFactory,
            IProduceConsumeInterceptor produceConsumeInterceptor,
            IMessageSerializationStrategy messageSerializationStrategy
        )
        {
            this.channelFactory = channelFactory;
            this.produceConsumeInterceptor = produceConsumeInterceptor;
            this.messageSerializationStrategy = messageSerializationStrategy;
        }

        /// <inheritdoc />
        public IPullingConsumer<PullResult> CreateConsumer(IQueue queue, PullingConsumerOptions options)
        {
            var channel = channelFactory.CreatePersistentChannel(new PersistentChannelOptions());
            return new PullingConsumer(options, queue, channel, produceConsumeInterceptor);
        }

        /// <inheritdoc />
        public IPullingConsumer<PullResult<T>> CreateConsumer<T>(IQueue queue, PullingConsumerOptions options)
        {
            var consumer = CreateConsumer(queue, options);
            return new PullingConsumer<T>(consumer, messageSerializationStrategy);
        }
    }
}
