using EasyNetQ.Producer;
using EasyNetQ.Scheduling;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        public RabbitBus(
            IAdvancedBus advanced,
            IPubSub pubSub,
            IRpc rpc,
            ISendReceive sendReceive,
            IScheduler scheduler
        )
        {
            Preconditions.CheckNotNull(advanced, "advanced");
            Preconditions.CheckNotNull(pubSub, "pubSub");
            Preconditions.CheckNotNull(rpc, "rpc");
            Preconditions.CheckNotNull(sendReceive, "sendReceive");
            Preconditions.CheckNotNull(scheduler, "scheduler");

            Advanced = advanced;
            PubSub = pubSub;
            Rpc = rpc;
            SendReceive = sendReceive;
            Scheduler = scheduler;
        }

        public IPubSub PubSub { get; }
        public IRpc Rpc { get; }
        public ISendReceive SendReceive { get; }
        public IScheduler Scheduler { get; }
        public IAdvancedBus Advanced { get; }

        public virtual void Dispose()
        {
            Advanced.Dispose();
        }
    }
}