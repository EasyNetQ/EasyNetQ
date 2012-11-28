// ReSharper disable InconsistentNaming

using EasyNetQ.Monitor.AlertSinks;
using NUnit.Framework;

namespace EasyNetQ.Monitor.Tests.AlertSinks
{
    [TestFixture]
    public class EventLogAlertSinkTests
    {
        private EventLogAlertSink eventLogAlertSink;

        [SetUp]
        public void SetUp()
        {
            eventLogAlertSink = new EventLogAlertSink();
        }

        [Test, Explicit("Writes to the event log")]
        public void Should_write_to_the_event_log()
        {
            eventLogAlertSink.Alert("some alert");
        }
    }
}

// ReSharper restore InconsistentNaming