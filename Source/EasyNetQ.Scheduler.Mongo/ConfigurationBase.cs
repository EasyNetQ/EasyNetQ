using System;
using System.Configuration;

namespace EasyNetQ.Scheduler.Mongo
{
    public class ConfigurationBase
    {
        protected static int GetIntAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            int value;
            if (!Int32.TryParse(appSetting, out value))
            {
                throw new ApplicationException(String.Format("AppSetting '{0}' value '{1}' is not a valid integer",
                                                             settingKey, appSetting));
            }
            return value;
        }

        protected static TimeSpan GetTimeSpanAppSettings(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            TimeSpan value;
            if (!TimeSpan.TryParse(appSetting, out value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid timespan",
                                                             settingKey, appSetting));
            }
            return value;
        }

        protected static bool GetBoolAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            bool value;
            if (!bool.TryParse(appSetting, out value))
            {
                throw new ApplicationException(String.Format("AppSetting '{0}' value '{1}' is not a valid boolean",
                                                             settingKey, appSetting));
            }
            return value;
        }
    }
}