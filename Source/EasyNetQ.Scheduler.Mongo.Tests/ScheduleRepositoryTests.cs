using System;
using System.Text;
using EasyNetQ.Scheduler.Mongo.Core;
using EasyNetQ.Tests;
using Xunit;

namespace EasyNetQ.Scheduler.Mongo.Tests
{
    [Explicit("Required a database")]
    public class ScheduleRepositoryTests
    {
        private ScheduleRepository scheduleRepository;

        public ScheduleRepositoryTests()
        {
            var configuration = new ScheduleRepositoryConfiguration
                {
                    ConnectionString = "mongodb://localhost:27017/?w=1&readPreference=primary&uuidRepresentation=csharpLegacy",
                    CollectionName = "Schedules",
                    DatabaseName = "EasyNetQ",
                    DeleteTimeout = TimeSpan.FromMinutes(5),
                    PublishTimeout = TimeSpan.FromSeconds(60),
                };
            scheduleRepository = new ScheduleRepository(configuration, () => DateTime.UtcNow);
        }

        [Fact]
        [Explicit("Required a database")]
        public void Should_be_able_to_store_a_schedule()
        {
            scheduleRepository.Store(new Schedule
                {
                    BindingKey = "abc",
                    CancellationKey = "bcd",
                    WakeTime = new DateTime(2011, 5, 18),
                    InnerMessage = Encoding.UTF8.GetBytes("Hello World!"),
                    Id = Guid.Empty,
                    State = ScheduleState.Pending,
                });
        }

        [Fact]
        [Explicit("Required a database")]
        public void Should_be_able_to_cancel_a_schedule()
        {
            scheduleRepository.Cancel("bcd");
        }

        [Fact]
        [Explicit("Required a database")]
        public void Should_be_able_to_get_messages()
        {
            while (true)
            {
                var schedule = scheduleRepository.GetPending();
                if(schedule == null)
                    break;
                Console.WriteLine("{0}, {1}, {2}",
                                  schedule.BindingKey,
                                  schedule.WakeTime,
                                  Encoding.UTF8.GetString(schedule.InnerMessage));
            }
        }

        [Fact]
        [Explicit("Required a database")]
        public void Should_be_able_to_handle_timeout()
        {
            scheduleRepository.HandleTimeout();
        }


        [Fact]
        [Explicit("Required a database")]
        public void Should_be_able_to_mark_as_published()
        {
            scheduleRepository.MarkAsPublished(Guid.Empty);
        }
    }
}

// ReSharper restore InconsistentNaming