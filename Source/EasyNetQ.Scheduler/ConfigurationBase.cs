using System;
using System.Configuration;

namespace EasyNetQ.Scheduler
{
    public class ConfigurationBase
    {
        public static short GetShortAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            short value;
            if (!short.TryParse(appSetting, out value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid short",
                                                             settingKey, appSetting));
            }
            return value;
        }

        public static int GetIntAppSetting(string settingKey)
        {
            var appSetting = ConfigurationManager.AppSettings[settingKey];
            int value;
            if (!int.TryParse(appSetting, out value))
            {
                throw new ApplicationException(string.Format("AppSetting '{0}' value '{1}' is not a valid integer",
                                                             settingKey, appSetting));
            }
            return value;
        }
    }
}