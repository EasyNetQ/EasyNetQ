using System;
using System.Configuration;
using EasyNetQ.Scheduler.Mongo.Core;

namespace EasyNetQ.Scheduler.Mongo
{
    public class SchedulerServiceConfiguration : ConfigurationBase, ISchedulerServiceConfiguration
    {
        public string SubscriptionId { get; set; }
        public TimeSpan PublishInterval { get; set; }
        public TimeSpan HandleTimeoutInterval { get; set; }
        public int PublishMaxSchedules { get; set; }
        public bool EnableLegacyConventions { get; set; }

        public static SchedulerServiceConfiguration FromConfigFile()
        {
            return new SchedulerServiceConfiguration
                {
                    SubscriptionId = ConfigurationManager.AppSettings["subscriptionId"],
                    PublishInterval = GetTimeSpanAppSettings("publishInterval"),
                    PublishMaxSchedules = GetIntAppSetting("publishMaxSchedules"),
                    HandleTimeoutInterval = GetTimeSpanAppSettings("handleTimeoutInterval"),
                    EnableLegacyConventions = GetBoolAppSetting("enableLegacyConventions")
                };
        }
    }
}