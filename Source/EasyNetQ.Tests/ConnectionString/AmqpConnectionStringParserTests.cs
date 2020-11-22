using System;
using System.Collections.Generic;
using EasyNetQ.ConnectionString;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.ConnectionString
{
    public class AmqpConnectionStringParserTests
    {
        private readonly AmqpConnectionStringParser connectionStringParser = new AmqpConnectionStringParser();

        [Theory]
        [MemberData(nameof(AppendixAExamples))]
        public void Should_parse_amqp(AmqpSpecification spec)
        {
            var configuration = connectionStringParser.Parse(spec.Uri);

            configuration.Should().BeEquivalentTo(spec.Configuration);
        }

        public static IEnumerable<object[]> AppendixAExamples()
        {
            object[] Spec(string uri, ConnectionConfiguration configuration)
            {
                return new[] {new AmqpSpecification(uri, configuration)};
            }

            yield return Spec(
                "amqp://user:pass@host:10000/vhost",
                new ConnectionConfiguration
                {
                    Hosts = new[] {new HostConfiguration {Host = "host", Port = 10000}},
                    VirtualHost = "vhost",
                    UserName = "user",
                    Password = "pass"
                }
            );
            yield return Spec(
                "amqp://",
                new ConnectionConfiguration
                {
                    Hosts = new[] {new HostConfiguration {Host = "localhost", Port = 5672}},
                }
            );
            yield return Spec(
                "amqp://host",
                new ConnectionConfiguration
                {
                    Hosts = new[] {new HostConfiguration {Host = "host", Port = 5672}},
                }
            );
            yield return Spec(
                "amqps://host",
                new ConnectionConfiguration
                {
                    Hosts = new[]
                    {
                        new HostConfiguration {Host = "host", Port = 5671, Ssl = {Enabled = true, ServerName = "host"}}
                    },
                }
            );
            yield return Spec(
                "amqp://?" + string.Join(
                    "&",
                    "persistentMessages=false",
                    "prefetchCount=2",
                    "timeout=1",
                    "publisherConfirms=true",
                    "name=unit-test",
                    "mandatoryPublish=true",
                    "connectIntervalAttempt=2",
                    "product=product",
                    "platform=platform"
                ),
                new ConnectionConfiguration
                {
                    Hosts = new[] {new HostConfiguration {Host = "localhost", Port = 5672}},
                    PersistentMessages = false,
                    PrefetchCount = 2,
                    Timeout = TimeSpan.FromSeconds(1),
                    ConnectIntervalAttempt = TimeSpan.FromSeconds(2),
                    PublisherConfirms = true,
                    Name = "unit-test",
                    MandatoryPublish = true,
                    Product = "product",
                    Platform = "platform"
                }
            );
        }

        public class AmqpSpecification
        {
            public readonly string Uri;
            public readonly ConnectionConfiguration Configuration;

            public AmqpSpecification(string uri, ConnectionConfiguration configuration)
            {
                Uri = uri;
                Configuration = configuration;
            }

            public override string ToString() => $"Uri: {Uri}";
        }
    }
}
