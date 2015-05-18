using System;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace EasyNetQ.Scheduler.Mongo.Core
{
    public interface IScheduleV2Repository
    {
        void Store(ScheduleV2 schedule);
        void Cancel(string cancelation);
        ScheduleV2 GetPending();
        void MarkAsPublished(Guid id);
        void HandleTimeout();
    }

    public class ScheduleV2Repository : IScheduleV2Repository
    {
        private readonly IScheduleRepositoryConfiguration configuration;
        private readonly Func<DateTime> getNow;
        private readonly Lazy<MongoCollection<ScheduleV2>> lazyCollection;
        private readonly Lazy<MongoServer> lazyServer;

        public ScheduleV2Repository(IScheduleRepositoryConfiguration configuration, Func<DateTime> getNow)
        {
            this.configuration = configuration;
            this.getNow = getNow;
            lazyServer = new Lazy<MongoServer>(Connect);
            lazyCollection = new Lazy<MongoCollection<ScheduleV2>>(CreateAndIndex);
        }

        private MongoServer Server
        {
            get { return lazyServer.Value; }
        }

        private MongoCollection<ScheduleV2> Collection
        {
            get { return lazyCollection.Value; }
        }

        public void Store(ScheduleV2 schedule)
        {
            Collection.Insert(schedule);
        }

        public void Cancel(string cancelation)
        {
            Collection.Remove(Query<ScheduleV2>.EQ(x => x.CancellationKey, cancelation));
        }

        public ScheduleV2 GetPending()
        {
            var now = getNow();
            var query = Query.And(
                Query<ScheduleV2>.EQ(x => x.State, ScheduleState.Pending),
                Query<ScheduleV2>.LTE(x => x.WakeTime, now));
            var update = Update.Combine(Update<ScheduleV2>.Set(x => x.State, ScheduleState.Publishing),
                                        Update<ScheduleV2>.Set(x => x.PublishingTime, now));
            var findAndModifyResult = Collection.FindAndModify(new FindAndModifyArgs
                {
                    Query = query,
                    SortBy = SortBy<ScheduleV2>.Ascending(x => x.WakeTime),
                    Update = update,
                    VersionReturned = FindAndModifyDocumentVersion.Modified,
                });
            return findAndModifyResult.GetModifiedDocumentAs<ScheduleV2>();
        }

        public void MarkAsPublished(Guid id)
        {
            var now = getNow();
            var query = Query.And(Query<ScheduleV2>.EQ(x => x.Id, id));
            var update = Update.Combine(Update<ScheduleV2>.Set(x => x.State, ScheduleState.Published),
                                        Update<ScheduleV2>.Set(x => x.PublishedTime, now),
                                        Update<ScheduleV2>.Unset(x => x.PublishingTime)
                );
            Collection.Update(query, update);
        }

        public void HandleTimeout()
        {
            var publishingTimeTimeout = getNow() - configuration.PublishTimeout;
            var query = Query.And(Query<ScheduleV2>.EQ(x => x.State, ScheduleState.Publishing), Query<ScheduleV2>.LTE(x => x.PublishingTime, publishingTimeTimeout));
            var update = Update.Combine(Update<ScheduleV2>.Set(x => x.State, ScheduleState.Pending), Update<ScheduleV2>.Unset(x => x.PublishingTime));
            Collection.Update(query, update, UpdateFlags.Multi);
        }

        private MongoCollection<ScheduleV2> CreateAndIndex()
        {
            var collection = Server.GetDatabase(configuration.DatabaseName).GetCollection<ScheduleV2>(configuration.CollectionName + "V2");
            collection.CreateIndex(IndexKeys<ScheduleV1>.Ascending(x => x.CancellationKey), IndexOptions.SetSparse(true));
            collection.CreateIndex(IndexKeys<ScheduleV1>.Ascending(x => x.State, x => x.WakeTime));
            collection.CreateIndex(IndexKeys<ScheduleV1>.Ascending(x => x.PublishedTime), IndexOptions.SetTimeToLive(configuration.DeleteTimeout).SetSparse(true));
            return collection;
        }

        private MongoServer Connect()
        {
            return new MongoClient(configuration.ConnectionString).GetServer();
        }
    }
}