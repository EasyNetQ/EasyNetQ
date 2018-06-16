// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class ClusterTests : IDisposable
    {
        private const string clusterHost1 = "ubuntu";
        private const string clusterHost2 = "ubuntu";
        private const string clusterPort1 = "5672"; // rabbit@ubuntu
        private const string clusterPort2 = "5674"; // rabbit_2@ubuntu
        private string connectionString;

        private IBus bus;

        public ClusterTests()
        {
            const string hostFormat = "{0}:{1}";
            var host1 = string.Format(hostFormat, clusterHost1, clusterPort1);
            var host2 = string.Format(hostFormat, clusterHost2, clusterPort2);
            var hosts = string.Format("{0},{1}", host1, host2);
            connectionString = string.Format("host={0};requestedHeartbeat=1", hosts);

            bus = RabbitHutch.CreateBus(connectionString);
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact][Explicit("Requires a running rabbitMQ cluster on server 'ubuntu'")]
        public void Should_create_the_correct_connection_string()
        {
            connectionString.Should().Be("host=ubuntu:5672,ubuntu:5674;requestedHeartbeat=1");
        }

        [Fact][Explicit("Requires a running rabbitMQ cluster on server 'ubuntu'")]
        public void Should_connect_to_the_first_available_node_in_cluster()
        {
            // just watch what happens
            Thread.Sleep(5 * 60 * 1000); // let's give it 5 minutes
        }

        [Fact][Explicit("Requires a running rabbitMQ cluster on server 'ubuntu'")]
        public void Should_be_able_to_resubscribe_on_reconnection()
        {
            bus.Subscribe<MyMessage>("cluster_test", message => Console.WriteLine(message.Text));

//            var count = 0;
//            while (true)
//            {
//                Thread.Sleep(5 * 1000); // five seconds between each publish    
//                using (var channel = bus.OpenPublishChannel())
//                {
//                    channel.Publish(new MyMessage { Text = "Hello " + count.ToString()});
//                }
//                count++;
//            }

            Thread.Sleep(TimeSpan.FromMinutes(5)); // let's give it 5 minutes    
        }
    }
}

// ReSharper restore InconsistentNaming