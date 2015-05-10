namespace EasyNetQ.Scheduler.Mongo.Core
{
    public interface ISchedulerService
    {
        void Start();
        void Stop();
    }
}