using System;
using System.Configuration;

namespace EasyNetQ.Scheduler
{
    public class ConfigurationBase
    {
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