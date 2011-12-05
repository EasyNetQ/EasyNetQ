using System;
using System.Configuration;

namespace EasyNetQ.Scheduler
{
    public class ScheduleRepositoryConfiguration
    {
        private const string connectionStringKey = "scheduleDb";

        public string ConnectionString { get; set; }
        public int PurgeBatchSize { get; set; }

        public static ScheduleRepositoryConfiguration FromConfigFile()
        {
            return new ScheduleRepositoryConfiguration
            {
                ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringKey].ConnectionString,
                PurgeBatchSize = GetIntAppSetting("PurgeBatchSize")
            };
        }

        public static int GetIntAppSetting(string settingKey)
        {
            var intAppSetting = ConfigurationManager.AppSettings[settingKey];
            int value;
            if (!int.TryParse(intAppSetting, out value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid integer", 
                    settingKey, intAppSetting));
            }
            return value;
        }
    }
}