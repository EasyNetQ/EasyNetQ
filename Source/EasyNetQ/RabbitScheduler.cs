using System;
using System.Threading.Tasks;
using EasyNetQ.SystemMessages;

namespace EasyNetQ
{
    public class RabbitScheduler : IScheduler
    {
        private readonly ISerializer serializer;
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly IBus bus;

        public RabbitScheduler(
            IBus bus,
            ISerializer serializer,
            ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(bus, "bus");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.bus = bus;
            this.serializer = serializer;
            this.typeNameSerializer = typeNameSerializer;
        }

        public void FuturePublish<T>(DateTime futurePublishDate, T message) where T : class
        {
            FuturePublish(futurePublishDate, null, message);
        }

        public void FuturePublish<T>(DateTime futurePublishDate, string cancellationKey, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            
            var typeName = typeNameSerializer.Serialize(typeof(T));
            var messageBody = serializer.MessageToBytes(message);

            bus.Publish(new ScheduleMe
            {
                WakeTime = futurePublishDate,
                BindingKey = typeName,
                CancellationKey = cancellationKey,
                InnerMessage = messageBody
            });
        }

        public void CancelFuturePublish(string cancellationKey)
        {
            bus.Publish(new UnscheduleMe
            {
                CancellationKey = cancellationKey
            });
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message) where T : class
        {
            return FuturePublishAsync(futurePublishDate, null, message);
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, string cancellationKey, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            var typeName = typeNameSerializer.Serialize(typeof(T));
            var messageBody = serializer.MessageToBytes(message);

            return bus.PublishAsync(new ScheduleMe
            {
                WakeTime = futurePublishDate,
                BindingKey = typeName,
                CancellationKey = cancellationKey,
                InnerMessage = messageBody
            });
        }

        public Task CancelFuturePublishAsync(string cancellationKey)
        {
            return bus.PublishAsync(new UnscheduleMe
            {
                CancellationKey = cancellationKey
            });
        }
    }
}
