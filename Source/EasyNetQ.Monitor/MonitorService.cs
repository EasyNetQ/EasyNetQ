using System;
using System.Threading;

namespace EasyNetQ.Monitor
{
    public interface IMonitorService
    {
        void Start();
        void Stop();
    }

    public class MonitorService : IMonitorService
    {
        private readonly MonitorConfigurationSection configurationSection;
        private readonly Timer timer;

        public MonitorService(
            IMonitorRun monitorRun, 
            MonitorConfigurationSection configurationSection)
        {
            this.configurationSection = configurationSection;
            timer = new Timer(state => monitorRun.Run());
        }

        public void Start()
        {
            timer.Change(
                TimeSpan.FromSeconds(configurationSection.IntervalMinutes),
                TimeSpan.FromSeconds(configurationSection.IntervalMinutes));
        }

        public void Stop()
        {
            timer.Dispose();
        }
    }
}