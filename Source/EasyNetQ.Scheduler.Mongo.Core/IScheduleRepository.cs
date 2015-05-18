using System;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace EasyNetQ.Scheduler.Mongo.Core
{
	public interface IScheduleRepository<T> where T : Schedule
    {
        void Store(T schedule);
        void Cancel(string cancelation);
        T GetPending();
        void MarkAsPublished(Guid id);
        void HandleTimeout();
    }
	
}