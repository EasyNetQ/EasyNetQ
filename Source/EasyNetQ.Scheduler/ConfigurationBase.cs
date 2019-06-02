using System;
using System.Configuration;

namespace EasyNetQ.Scheduler
{
    public class ConfigurationBase
    {
        public static short GetShortAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            if (!short.TryParse(appSetting, out short value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid short",
                    settingKey, appSetting));
            }

            return value;
        }

        public static int GetIntAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            if (!int.TryParse(appSetting, out int value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid integer",
                    settingKey, appSetting));
            }

            return value;
        }

        public static bool GetBoolAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            if (!bool.TryParse(appSetting, out bool value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid boolean",
                    settingKey, appSetting));
            }

            return value;
        }
    }
}
