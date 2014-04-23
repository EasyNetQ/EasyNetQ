using System;
using System.Collections.Generic;
using EasyNetQ.SystemMessages;

namespace EasyNetQ.Scheduler.Tests
{
    public class MockScheduleRepository : IScheduleRepository
    {
        public Func<IList<ScheduleMe>> GetPendingDelegate { get; set; } 

        public void Store(ScheduleMe scheduleMe)
        {
            throw new NotImplementedException();
        }

        public void Cancel(UnscheduleMe unscheduleMe)
        {
            throw new NotImplementedException();
        }

        public IList<ScheduleMe> GetPending()
        {
            return (GetPendingDelegate != null)
                       ? GetPendingDelegate()
                       : null;
        }

        public void Purge()
        {
            throw new NotImplementedException();
        }
    }
}