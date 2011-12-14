using System.Configuration;

namespace EasyNetQ.Scheduler
{
    public class ScheduleRepositoryConfiguration : ConfigurationBase
    {
        private const string connectionStringKey = "scheduleDb";

        public string ConnectionString { get; set; }
        public int PurgeBatchSize { get; set; }
        public int MaximumScheduleMessagesToReturn { get; set; }

        /// <summary>
        /// The number of days after a schedule item triggers before it is purged.
        /// </summary>
        public int PurgeDelayDays { get; set; }

        public static ScheduleRepositoryConfiguration FromConfigFile()
        {
            return new ScheduleRepositoryConfiguration
            {
                ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringKey].ConnectionString,
                PurgeBatchSize = GetIntAppSetting("PurgeBatchSize"),
                PurgeDelayDays = GetIntAppSetting("PurgeDelayDays"),
                MaximumScheduleMessagesToReturn = GetIntAppSetting("MaximumScheduleMessagesToReturn")
            };
        }
    }
}