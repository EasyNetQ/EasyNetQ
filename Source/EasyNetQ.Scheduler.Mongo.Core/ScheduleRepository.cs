using System;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace EasyNetQ.Scheduler.Mongo.Core
{
    public interface IScheduleRepository
    {
        void Store(Schedule schedule);
        void Cancel(string cancelation);
        Schedule GetPending();
        void MarkAsPublished(Guid id);
        void HandleTimeout();
    }

    public class ScheduleRepository : IScheduleRepository
    {
        private readonly IScheduleRepositoryConfiguration configuration;
        private readonly Func<DateTime> getNow;
        private readonly Lazy<MongoCollection<Schedule>> lazyCollection;
        private readonly Lazy<MongoServer> lazyServer;

        public ScheduleRepository(IScheduleRepositoryConfiguration configuration, Func<DateTime> getNow)
        {
            this.configuration = configuration;
            this.getNow = getNow;
            lazyServer = new Lazy<MongoServer>(Connect);
            lazyCollection = new Lazy<MongoCollection<Schedule>>(CreateAndIndex);
        }

        private MongoServer Server
        {
            get { return lazyServer.Value; }
        }

        private MongoCollection<Schedule> Collection
        {
            get { return lazyCollection.Value; }
        }

        public void Store(Schedule schedule)
        {
            Collection.Insert(schedule);
        }

        public void Cancel(string cancelation)
        {
            Collection.Remove(Query<Schedule>.EQ(x => x.CancellationKey, cancelation));
        }

        public Schedule GetPending()
        {
            var now = getNow();
            var query = Query.And(
                Query<Schedule>.EQ(x => x.State, ScheduleState.Pending),
                Query<Schedule>.LTE(x => x.WakeTime, now));
            var update = Update.Combine(Update<Schedule>.Set(x => x.State, ScheduleState.Publishing),
                                        Update<Schedule>.Set(x => x.PublishingTime, now));
            var findAndModifyResult = Collection.FindAndModify(new FindAndModifyArgs
                {
                    Query = query,
                    SortBy = SortBy<Schedule>.Ascending(x => x.WakeTime),
                    Update = update,
                    VersionReturned = FindAndModifyDocumentVersion.Modified,
                });
            return findAndModifyResult.GetModifiedDocumentAs<Schedule>();
        }

        public void MarkAsPublished(Guid id)
        {
            var now = getNow();
            var query = Query.And(Query<Schedule>.EQ(x => x.Id, id));
            var update = Update.Combine(Update<Schedule>.Set(x => x.State, ScheduleState.Published),
                                        Update<Schedule>.Set(x => x.PublishedTime, now),
                                        Update<Schedule>.Unset(x => x.PublishingTime)
                );
            Collection.Update(query, update);
        }

        public void HandleTimeout()
        {
            var publishingTimeTimeout = getNow() - configuration.PublishTimeout;
            var query = Query.And(Query<Schedule>.EQ(x => x.State, ScheduleState.Publishing), Query<Schedule>.LTE(x => x.PublishingTime, publishingTimeTimeout));
            var update = Update.Combine(Update<Schedule>.Set(x => x.State, ScheduleState.Pending), Update<Schedule>.Unset(x => x.PublishingTime));
            Collection.Update(query, update, UpdateFlags.Multi);
        }

        private MongoCollection<Schedule> CreateAndIndex()
        {
            var collection = Server.GetDatabase(configuration.DatabaseName).GetCollection<Schedule>(configuration.CollectionName);
            collection.CreateIndex(IndexKeys<Schedule>.Ascending(x => x.CancellationKey), IndexOptions.SetSparse(true));
            collection.CreateIndex(IndexKeys<Schedule>.Ascending(x => x.State, x => x.WakeTime));
            collection.CreateIndex(IndexKeys<Schedule>.Ascending(x => x.PublishedTime), IndexOptions.SetTimeToLive(configuration.DeleteTimeout).SetSparse(true));
            return collection;
        }

        private MongoServer Connect()
        {
            return new MongoClient(configuration.ConnectionString).GetServer();
        }
    }
}