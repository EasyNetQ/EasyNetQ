using System;
using System.Threading;

namespace EasyNetQ.Monitor
{
    public class MonitorService
    {
        private readonly IMonitorRun monitorRun;
        private readonly MonitorConfigurationSection configurationSection;
        private readonly Timer timer;

        public MonitorService(IMonitorRun monitorRun, MonitorConfigurationSection configurationSection)
        {
            this.monitorRun = monitorRun;
            this.configurationSection = configurationSection;
            timer = new Timer(state => monitorRun.Run());
        }

        public void Start()
        {
            timer.Change(
                TimeSpan.FromMinutes(configurationSection.IntervalMinutes),
                TimeSpan.FromMinutes(configurationSection.IntervalMinutes));
        }

        public void Stop()
        {
            timer.Dispose();
        }
    }
}