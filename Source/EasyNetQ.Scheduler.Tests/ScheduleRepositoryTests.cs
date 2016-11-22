// ReSharper disable InconsistentNaming
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Text;

namespace EasyNetQ.Scheduler.Tests
{
    [TestFixture]
    [Explicit("Required a database")]
    public class ScheduleRepositoryTests
    {
        private ScheduleRepository scheduleRepository;

        [SetUp]
        public void SetUp()
        {
            var log = Substitute.For<IEasyNetQLogger>();
            var configuration = new ScheduleRepositoryConfiguration
            {
                ProviderName = "System.Data.SqlClient",
                ConnectionString = "Data Source=localhost;Initial Catalog=EasyNetQ.Scheduler;Integrated Security=SSPI;",
                PurgeBatchSize = 100
            };
            scheduleRepository = new ScheduleRepository(configuration, log, () => DateTime.UtcNow);
        }

        [Test]
        [Explicit("Required a database")]
        public void Should_be_able_to_store_a_schedule()
        {
            scheduleRepository.Store(new ScheduleMe
            {
                BindingKey = "abc",
                CancellationKey = "bcd",
                WakeTime = new DateTime(2011, 5, 18),
                InnerMessage = Encoding.UTF8.GetBytes("Hello World!"),
                MessageProperties = new MessageProperties { Type = string.Format("{0}:{1}", typeof(ScheduleMe).FullName, typeof(ScheduleMe).Assembly.GetName().Name) }
            });
        }

        [Test]
        [Explicit("Required a database")]
        public void Should_be_able_to_store_a_schedule_with_exchange()
        {
            var typeNameSerializer = new TypeNameSerializer();
            var conventions = new Conventions(typeNameSerializer);
            var jsonSerializer = new JsonSerializer(typeNameSerializer);
            var messageSerializationStrategy = new DefaultMessageSerializationStrategy(typeNameSerializer, jsonSerializer, new DefaultCorrelationIdGenerationStrategy());
            var testScheduleMessage = new TestScheduleMessage { Text = "Hello World" };

            var serializedMessage = messageSerializationStrategy.SerializeMessage(new Message<TestScheduleMessage>(testScheduleMessage));

            scheduleRepository.Store(new ScheduleMe
            {
                BindingKey = "",
                CancellationKey = "bcd",
                Exchange = conventions.ExchangeNamingConvention(typeof(TestScheduleMessage)),
                ExchangeType = ExchangeType.Topic,
                RoutingKey = "#",
                WakeTime = DateTime.UtcNow.AddMilliseconds(-1),
                InnerMessage = serializedMessage.Body,
                MessageProperties = serializedMessage.Properties
            });
        }

        [Test]
        [Explicit("Required a database")]
        public void Should_be_able_to_cancel_a_schedule()
        {
            scheduleRepository.Cancel(new UnscheduleMe
            {
                CancellationKey = "bcd"
            });
        }

        [Test]
        [Explicit("Required a database")]
        public void Should_be_able_to_get_messages()
        {
            var schedules = scheduleRepository.GetPending();
            foreach (var scheduleMe in schedules)
            {
                Console.WriteLine("key: {0}, waketime: {1}, exchange {2}, type: {3}, properties: {4}, routing: {5}, body:{6}", 
                    scheduleMe.BindingKey, 
                    scheduleMe.WakeTime,
                    scheduleMe.Exchange,
                    scheduleMe.ExchangeType,
                    scheduleMe.MessageProperties,
                    scheduleMe.RoutingKey,
                    Encoding.UTF8.GetString(scheduleMe.InnerMessage));
            }
        }

        [Test]
        [Explicit("Required a database")]
        public void Should_be_able_to_purge_messages()
        {
            scheduleRepository.Purge();
        }

        public DateTime GetCurrentUtcTime()
        {
            return DateTime.UtcNow;
        }
    }

    public class TestScheduleMessage
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming