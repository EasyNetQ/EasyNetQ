namespace EasyNetQ
{
    /// <inheritdoc />
    public class RabbitBus : IBus
    {
        /// <summary>
        ///     Creates RabbitBus
        /// </summary>
        /// <param name="advanced">The advanced bus</param>
        /// <param name="pubSub">The pub-sub</param>
        /// <param name="rpc">The rpc</param>
        /// <param name="sendReceive">The send-receive</param>
        /// <param name="scheduler">The scheduler</param>
        public RabbitBus(
            IAdvancedBus advanced,
            IPubSub pubSub,
            IRpc rpc,
            ISendReceive sendReceive,
            IScheduler scheduler
        )
        {
            Preconditions.CheckNotNull(advanced, nameof(advanced));
            Preconditions.CheckNotNull(pubSub, nameof(pubSub));
            Preconditions.CheckNotNull(rpc, nameof(rpc));
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));
            Preconditions.CheckNotNull(scheduler, nameof(scheduler));

            Advanced = advanced;
            PubSub = pubSub;
            Rpc = rpc;
            SendReceive = sendReceive;
            Scheduler = scheduler;
        }

        /// <inheritdoc />
        public IPubSub PubSub { get; }

        /// <inheritdoc />
        public IRpc Rpc { get; }

        /// <inheritdoc />
        public ISendReceive SendReceive { get; }

        /// <inheritdoc />
        public IScheduler Scheduler { get; }

        /// <inheritdoc />
        public IAdvancedBus Advanced { get; }

        /// <inheritdoc />
        public virtual void Dispose()
        {
        }
    }
}
