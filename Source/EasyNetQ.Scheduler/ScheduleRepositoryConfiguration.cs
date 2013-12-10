using System.Configuration;

namespace EasyNetQ.Scheduler
{
    public class ScheduleRepositoryConfiguration : ConfigurationBase
    {
        private const string connectionStringKey = "scheduleDb";
        private const string connectionStringNameKey = "scheduleDbConnectionStringName";
        private const string schemaNameKey = "SchemaName";

        public string SchemaName { get; set; }
        public string ConnectionString { get; set; }
        public string ConnectionStringName { get; set; }
        public int PurgeBatchSize { get; set; }
        public int MaximumScheduleMessagesToReturn { get; set; }

        /// <summary>
        /// The number of days after a schedule item triggers before it is purged.
        /// </summary>
        public int PurgeDelayDays { get; set; }

        public static ScheduleRepositoryConfiguration FromConfigFile()
        {
            var connectionStringName = ConfigurationManager.AppSettings[connectionStringNameKey] ?? connectionStringKey;
            return new ScheduleRepositoryConfiguration
            {
                ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString,
                PurgeBatchSize = GetIntAppSetting("PurgeBatchSize"),
                PurgeDelayDays = GetIntAppSetting("PurgeDelayDays"),
                MaximumScheduleMessagesToReturn = GetIntAppSetting("MaximumScheduleMessagesToReturn"),
                SchemaName = ConfigurationManager.AppSettings[schemaNameKey]
            };
        }
    }
}