using System;
using MongoDB.Bson.Serialization.Attributes;

namespace EasyNetQ.Scheduler.Mongo.Core
{
	public class ScheduleV1 : Schedule
    {
        public string BindingKey { get; set; }
    }

}