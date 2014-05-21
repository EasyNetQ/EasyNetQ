using System;
using System.Configuration;
using EasyNetQ.Scheduler.Mongo.Core;

namespace EasyNetQ.Scheduler.Mongo
{
    public class ScheduleRepositoryConfiguration : ConfigurationBase, IScheduleRepositoryConfiguration
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public TimeSpan DeleteTimeout { get; set; }
        public TimeSpan PublishTimeout { get; set; }

        public static ScheduleRepositoryConfiguration FromConfigFile()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["mongodb"];
            return new ScheduleRepositoryConfiguration
                {
                    ConnectionString = connectionString.ConnectionString,
                    CollectionName = ConfigurationManager.AppSettings["collectionName"],
                    DatabaseName = ConfigurationManager.AppSettings["databaseName"],
                    DeleteTimeout = GetTimeSpanAppSettings("deleteTimeout"),
                    PublishTimeout = GetTimeSpanAppSettings("publishTimeout")
                };
        }
    }
}