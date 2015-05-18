using System;
using MongoDB.Bson.Serialization.Attributes;

namespace EasyNetQ.Scheduler.Mongo.Core
{
	public abstract class Schedule
	{
		public Guid Id { get; set; }

		public DateTime WakeTime { get; set; }

		[BsonIgnoreIfNull]
		public string CancellationKey { get; set; }

		public ScheduleState State { get; set; }

		[BsonIgnoreIfNull]
		public DateTime? PublishingTime { get; set; }

		[BsonIgnoreIfNull]
		public DateTime? PublishedTime { get; set; }

		public byte[] InnerMessage { get; set; }
	}
}