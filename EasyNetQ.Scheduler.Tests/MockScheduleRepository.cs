using System;
using System.Collections.Generic;
using EasyNetQ.SystemMessages;

namespace EasyNetQ.Scheduler.Tests
{
    public class MockScheduleRepository : IScheduleRepository
    {
        public Func<DateTime, IList<ScheduleMe>> GetPendingDelegate { get; set; } 

        public void Store(ScheduleMe scheduleMe)
        {
            throw new NotImplementedException();
        }

        public IList<ScheduleMe> GetPending(DateTime timeNow)
        {
            return (GetPendingDelegate != null)
                       ? GetPendingDelegate(timeNow)
                       : null;
        }
    }
}