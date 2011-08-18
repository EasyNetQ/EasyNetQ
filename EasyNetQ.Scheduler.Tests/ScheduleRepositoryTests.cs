// ReSharper disable InconsistentNaming
using System;
using System.Text;
using EasyNetQ.SystemMessages;
using NUnit.Framework;

namespace EasyNetQ.Scheduler.Tests
{
    [TestFixture]
    public class ScheduleRepositoryTests
    {
        private ScheduleRepository scheduleRepository;

        [SetUp]
        public void SetUp()
        {
            scheduleRepository = new ScheduleRepository(
                "Data Source=localhost;Initial Catalog=EasyNetQ.Scheduler;Integrated Security=SSPI;");
        }

        [Test]
        [Explicit("Required a database")]
        public void Should_be_able_to_store_a_schedule()
        {
            scheduleRepository.Store(new ScheduleMe
            {
                BindingKey = "abc",
                WakeTime = new DateTime(2011, 5, 18),
                InnerMessage = Encoding.UTF8.GetBytes("Hello World!")
            });
        }

        [Test]
        [Explicit("Required a database")]
        public void Should_be_able_to_get_messages()
        {
            var schedules = scheduleRepository.GetPending(new DateTime(2011, 5, 19));
            foreach (var scheduleMe in schedules)
            {
                Console.WriteLine("{0}, {1}, {2}", 
                    scheduleMe.BindingKey, 
                    scheduleMe.WakeTime,
                    Encoding.UTF8.GetString(scheduleMe.InnerMessage));
            }
        }

        public DateTime GetCurrentUtcTime()
        {
            return DateTime.UtcNow;
        }
    }
}

// ReSharper restore InconsistentNaming