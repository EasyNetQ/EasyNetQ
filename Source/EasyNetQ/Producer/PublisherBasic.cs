using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Handles basic publish scenarios where publisher confirms and transactions are not required
    ///
    /// Note, this class is designed to be called sequentially from a single thread. It is NOT
    /// thread safe.
    /// </summary>
    public class PublisherBasic : PublisherBase
    {
        public PublisherBasic(IEventBus eventBus) : base(eventBus)
        {
        }

        public override Task PublishAsync(IModel model, Action<IModel> publishAction)
        {
            SetModel(model);
            publishAction(model);
            return TaskHelpers.Completed;
        }
    }
}