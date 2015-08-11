// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using EasyNetQ.ConnectionString;
using NUnit.Framework;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConnectionString
{
    [TestFixture]
    public class SslOptionsStringParserTests
    {
        [Test]
        public void Should_parse_empty_string_to_empty_ssloptions_enumerable()
        {
            IEnumerable<SslOption> sslOptions = null;

            Assert.DoesNotThrow(() =>
                sslOptions = new SslOptionsStringParser().Parse(String.Empty)
                );

            sslOptions.ShouldNotBeNull();
            sslOptions.ShouldBeEmpty();
        }

        [Test]
        public void Should_parse_null_to_empty_ssloptions_enumerable()
        {
            IEnumerable<SslOption> sslOptions = null;

            Assert.DoesNotThrow(() =>
                sslOptions = new SslOptionsStringParser().Parse(null)
                );

            sslOptions.ShouldNotBeNull();
            sslOptions.ShouldBeEmpty();
        }

        [Test]
        public void Should_throw_on_parser_error()
        {
            Assert.That(() => new SslOptionsStringParser().Parse("asdf"), Throws.InstanceOf<EasyNetQException>());
        }
    }
}

// ReSharper restore InconsistentNaming