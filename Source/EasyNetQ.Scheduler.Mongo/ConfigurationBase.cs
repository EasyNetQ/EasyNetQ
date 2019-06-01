using System;
using System.Configuration;

namespace EasyNetQ.Scheduler.Mongo
{
    public class ConfigurationBase
    {
        protected static int GetIntAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            if (!int.TryParse(appSetting, out int value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid integer",
                    settingKey, appSetting));
            }

            return value;
        }

        protected static TimeSpan GetTimeSpanAppSettings(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            if (!TimeSpan.TryParse(appSetting, out TimeSpan value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid timespan",
                    settingKey, appSetting));
            }

            return value;
        }

        protected static bool GetBoolAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            if (!bool.TryParse(appSetting, out bool value))
            {
                throw new ApplicationException(String.Format("AppSetting '{0}' value '{1}' is not a valid boolean",
                    settingKey, appSetting));
            }

            return value;
        }
    }
}
