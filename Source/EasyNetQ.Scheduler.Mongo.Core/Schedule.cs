using System;
using MongoDB.Bson.Serialization.Attributes;

namespace EasyNetQ.Scheduler.Mongo.Core
{
    public class Schedule
    {
        public Guid Id { get; set; }

        public DateTime WakeTime { get; set; }

        [Obsolete]
        public string BindingKey { get; set; }

        [BsonIgnoreIfNull]
        public string CancellationKey { get; set; }

        public string Exchange { get; set; }

        public string ExchangeType { get; set; }

        public string RoutingKey { get; set; }

        public byte[] InnerMessage { get; set; }

        public MessageProperties BasicProperties { get; set; }

        public ScheduleState State { get; set; }

        [BsonIgnoreIfNull]
        public DateTime? PublishingTime { get; set; }

        [BsonIgnoreIfNull]
        public DateTime? PublishedTime { get; set; }
    }
}