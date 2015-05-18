using System;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace EasyNetQ.Scheduler.Mongo.Core
{
	public class ScheduleRepository<T> : IScheduleRepository<T> where T : Schedule
    {
        private readonly IScheduleRepositoryConfiguration configuration;
        private readonly Func<DateTime> getNow;
        private readonly Lazy<MongoCollection<T>> lazyCollection;
        private readonly Lazy<MongoServer> lazyServer;

        public ScheduleRepository(IScheduleRepositoryConfiguration configuration, Func<DateTime> getNow)
        {
            this.configuration = configuration;
            this.getNow = getNow;
            lazyServer = new Lazy<MongoServer>(Connect);
            lazyCollection = new Lazy<MongoCollection<T>>(CreateAndIndex);
        }

        private MongoServer Server
        {
            get { return lazyServer.Value; }
        }

        private MongoCollection<T> Collection
        {
            get { return lazyCollection.Value; }
        }

        public void Store(T schedule)
        {
            Collection.Insert(schedule);
        }

        public void Cancel(string cancelation)
        {
            Collection.Remove(Query<T>.EQ(x => x.CancellationKey, cancelation));
        }

        public T GetPending()
        {
            var now = getNow();
            var query = Query.And(
                Query<T>.EQ(x => x.State, ScheduleState.Pending),
                Query<T>.LTE(x => x.WakeTime, now));
            var update = Update.Combine(Update<T>.Set(x => x.State, ScheduleState.Publishing),
                                        Update<T>.Set(x => x.PublishingTime, now));
            var findAndModifyResult = Collection.FindAndModify(new FindAndModifyArgs
                {
                    Query = query,
                    SortBy = SortBy<T>.Ascending(x => x.WakeTime),
                    Update = update,
                    VersionReturned = FindAndModifyDocumentVersion.Modified,
                });
            return findAndModifyResult.GetModifiedDocumentAs<T>();
        }

        public void MarkAsPublished(Guid id)
        {
            var now = getNow();
            var query = Query.And(Query<T>.EQ(x => x.Id, id));
            var update = Update.Combine(Update<T>.Set(x => x.State, ScheduleState.Published),
                                        Update<T>.Set(x => x.PublishedTime, now),
                                        Update<T>.Unset(x => x.PublishingTime)
                );
            Collection.Update(query, update);
        }

        public void HandleTimeout()
        {
            var publishingTimeTimeout = getNow() - configuration.PublishTimeout;
            var query = Query.And(Query<T>.EQ(x => x.State, ScheduleState.Publishing), Query<T>.LTE(x => x.PublishingTime, publishingTimeTimeout));
            var update = Update.Combine(Update<T>.Set(x => x.State, ScheduleState.Pending), Update<T>.Unset(x => x.PublishingTime));
            Collection.Update(query, update, UpdateFlags.Multi);
        }

        private MongoCollection<T> CreateAndIndex()
        {
            var collection = Server.GetDatabase(configuration.DatabaseName).GetCollection<T>(configuration.CollectionName);
            collection.CreateIndex(IndexKeys<T>.Ascending(x => x.CancellationKey), IndexOptions.SetSparse(true));
            collection.CreateIndex(IndexKeys<T>.Ascending(x => x.State, x => x.WakeTime));
            collection.CreateIndex(IndexKeys<T>.Ascending(x => x.PublishedTime), IndexOptions.SetTimeToLive(configuration.DeleteTimeout).SetSparse(true));
            return collection;
        }

        private MongoServer Connect()
        {
            return new MongoClient(configuration.ConnectionString).GetServer();
        }
    }
}