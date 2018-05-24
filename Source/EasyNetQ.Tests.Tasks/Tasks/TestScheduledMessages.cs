using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Scheduling;
using Net.CommandLine;
using Serilog;

namespace EasyNetQ.Tests.Tasks
{
    public class TestScheduledMessages : ICommandLineTask<int>, IDisposable
    {
        private readonly ILogger logger;
        private IBus bus;

        public TestScheduledMessages(ILogger logger)
        {
            this.logger = logger;
        }

        public Task Run(int delay, CancellationToken cancellationToken)
        {
            bus = RabbitHutch.CreateBus("host=localhost");

            bus.Subscribe<ScheduleTestMessage>("scheduled-message", OnMessage, configuration => configuration.WithAutoDelete());
            bus.FuturePublish(TimeSpan.FromSeconds(delay), new ScheduleTestMessage());

            Console.WriteLine("Waiting for message to return, press enter to stop");
            Console.ReadLine();

            return Task.FromResult(0);
        }

        private void OnMessage(ScheduleTestMessage message)
        {
            logger.Information("Received message {message} after {delay}", message, DateTime.Now - message.Timestamp);
        }

        public void Dispose()
        {
            bus.Dispose();
        }
    }

    public class ScheduleTestMessage
    {
        public ScheduleTestMessage()
        {
            Timestamp = DateTime.Now;
        }
        public DateTime Timestamp { get; set; }
    }
}