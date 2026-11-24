using EasyNetQ.Hosting;

namespace EasyNetQ.Core.Tests;

public class ConsumerHostStatusTests
{
    [Fact]
    public void Should_report_started_once_every_joined_host_started()
    {
        var status = new ConsumerHostStatus();
        object fluent = new(), autoSubscriber = new();
        status.Join(fluent);
        status.Join(autoSubscriber);
        status.Pending(fluent, 2);
        status.Pending(autoSubscriber, 1);
        status.PendingConsumers.Should().Be(3);

        status.Failed(new InvalidOperationException("broker down"));
        status.Started(fluent);
        status.IsStarted.Should().BeFalse();
        status.PendingConsumers.Should().Be(1);

        status.Started(autoSubscriber);
        status.IsStarted.Should().BeTrue();
        status.PendingConsumers.Should().Be(0);
        status.LastError.Should().BeNull();
    }
}
