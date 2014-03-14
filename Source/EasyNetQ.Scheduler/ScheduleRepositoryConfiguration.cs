using System.Configuration;

namespace EasyNetQ.Scheduler
{
    public class ScheduleRepositoryConfiguration : ConfigurationBase
    {
        private const string connectionStringKey = "scheduleDb";
        private const string connectionStringNameKey = "scheduleDbConnectionStringName";
        private const string schemaNameKey = "SchemaName";

        public string ProviderName { get; set; }
        public string ConnectionString { get; set; }
        public string ConnectionStringName { get; set; }
        public string SchemaName { get; set; }

        public short PurgeBatchSize { get; set; }
        public int MaximumScheduleMessagesToReturn { get; set; }

        /// <summary>
        /// The number of days after a schedule item triggers before it is purged.
        /// </summary>
        public int PurgeDelayDays { get; set; }

        public static ScheduleRepositoryConfiguration FromConfigFile()
        {
            var connectionStringName = ConfigurationManager.AppSettings[connectionStringNameKey] ?? connectionStringKey;
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];
            var providerName = ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName;
            return new ScheduleRepositoryConfiguration
            {
                ProviderName = string.IsNullOrEmpty(providerName) ? "System.Data.SqlClient" : providerName,
                ConnectionString = connectionString.ConnectionString,
                PurgeBatchSize = GetShortAppSetting("PurgeBatchSize"),
                PurgeDelayDays = GetIntAppSetting("PurgeDelayDays"),
                MaximumScheduleMessagesToReturn = GetIntAppSetting("MaximumScheduleMessagesToReturn"),
                SchemaName = ConfigurationManager.AppSettings[schemaNameKey]
            };
        }
    }
}