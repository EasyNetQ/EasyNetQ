namespace EasyNetQ.Scheduler
{
    public class SchedulerServiceConfiguration : ConfigurationBase
    {
        public int PublishIntervalSeconds { get; set; }
        public int PurgeIntervalSeconds { get; set; }
        public bool EnableLegacyConventions { get; set; }

        public static SchedulerServiceConfiguration FromConfigFile()
        {
            return new SchedulerServiceConfiguration
            {
                PublishIntervalSeconds = GetIntAppSetting("PublishIntervalSeconds"),
                PurgeIntervalSeconds = GetIntAppSetting("PurgeIntervalSeconds"),
                EnableLegacyConventions = GetBoolAppSetting("EnableLegacyConventions")
            };
        }
    }
}