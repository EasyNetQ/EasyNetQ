using System;
using EasyNetQ.Scheduler.Mongo.Core;

namespace EasyNetQ.Scheduler.Mongo.Tests
{
    public class MockScheduleRepository : IScheduleRepository
    {
        public Func<ScheduleV1> GetPendingDelegate { get; set; } 

        public void Store(ScheduleV1 scheduleMe)
        {
        }

        public void Cancel(string cancelation)
        {
        }

        public ScheduleV1 GetPending()
        {
            return (GetPendingDelegate != null)
                       ? GetPendingDelegate()
                       : null;
        }

        public void MarkAsPublished(Guid id)
        {
        }

        public void HandleTimeout()
        {
        }
    }
}