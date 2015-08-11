// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.ConnectionString;
using NUnit.Framework;
using RabbitMQ.Client;
using Sprache;

namespace EasyNetQ.Tests.ConnectionString
{
    [TestFixture]
    public class SslOptionsGrammarTests
    {
        [Test]
        public void Should_parse_servername()
        {
            SslOption sslOption = SslOptionsGrammar.SslOptionParser.Parse("servername=myhost");

            sslOption.ServerName.ShouldEqual("myhost");
        }

        [Test]
        public void Should_parse_certpath()
        {
            SslOption sslOption = SslOptionsGrammar.SslOptionParser.Parse(@"certpath=c:\\mycert.p12");

            sslOption.CertPath.ShouldEqual(@"c:\\mycert.p12");
        }

        [Test]
        public void Should_parse_certpassphrase()
        {
            SslOption sslOption = SslOptionsGrammar.SslOptionParser.Parse("certpassphrase=secret");

            sslOption.CertPassphrase.ShouldEqual("secret");
        }

        [Test]
        public void Should_parse_enabled()
        {
            SslOption sslOption = SslOptionsGrammar.SslOptionParser.Parse("enabled=false");

            sslOption.Enabled.ShouldEqual(false);
        }

        [Test]
        public void Should_default_enabled_to_true()
        {
            SslOption sslOption = SslOptionsGrammar.SslOptionParser.Parse("servername=myhost");

            sslOption.Enabled.ShouldEqual(true);
        }

        [Test]
        public void Should_throw_on_enabled_not_bool()
        {
            Assert.That(() => SslOptionsGrammar.SslOptionParser.Parse("enabled=asdf"), Throws.InstanceOf<ParseException>());
        }

        [Test]
        public void Should_throw_on_unknown_key()
        {
            Assert.That(() => SslOptionsGrammar.SslOptionParser.Parse("unknownkey=value"), Throws.InstanceOf<ParseException>());
        }

        [Test]
        public void Should_throw_on_unknown_key_at_end()
        {
            Assert.That(() => SslOptionsGrammar.SslOptionParser.Parse("server=myhost;unknownkey=value"), Throws.InstanceOf<ParseException>());
        }

        [Test]
        public void Should_throw_when_unparseable()
        {
            Assert.That(() => SslOptionsGrammar.SslOptionParser.Parse("asdf"), Throws.InstanceOf<ParseException>());
        }

        [Test]
        public void Should_throw_when_expect_at_least_one_part()
        {
            Assert.That(() => SslOptionsGrammar.SslOptionStringBuilder.Parse(""), Throws.InstanceOf<ParseException>());
        }

        [Test]
        public void Should_parse_full_ssloption()
        {
            string server = "myhost";
            string certpath = @"c:\\mycerts.mycert.p12";
            string passphrase = "secret";
            string enabled = "false";

            string fullSslOptionString = String.Format("servername={0};certpath={1};certpassphrase={2};enabled={3}", server, certpath, passphrase, enabled);

            var sslOption = SslOptionsGrammar.SslOptionParser.Parse(fullSslOptionString);

            sslOption.ServerName.ShouldEqual(server);
            sslOption.CertPath.ShouldEqual(certpath);
            sslOption.CertPassphrase.ShouldEqual(passphrase);
            sslOption.Enabled.ShouldEqual(bool.Parse(enabled));
        }

        [Test]
        public void Should_parse_multiple_ssloptions()
        {
            string server1 = "myhost1";
            string certpath1 = @"c:\\mycerts.mycert1.p12";
            string passphrase1 = "secret1";
            string enabled1 = "false";
            string server2 = "myhost2";
            string certpath2 = @"c:\\mycerts.mycert2.p12";
            string passphrase2 = "secret2";
            string enabled2 = "true";

            string fullSslOptionsString = String.Format("servername={0};certpath={1};certpassphrase={2};enabled={3},servername={4};certpath={5};certpassphrase={6};enabled={7}", 
                server1, certpath1, passphrase1, enabled1,
                server2, certpath2, passphrase2, enabled2
                );

            var sslOptions = SslOptionsGrammar.SslOptionsParser.Parse(fullSslOptionsString);


            sslOptions.Count().ShouldEqual(2);
            var sslOption1 = sslOptions.First();
            var sslOption2 = sslOptions.Last();

            sslOption1.ServerName.ShouldEqual(server1);
            sslOption1.CertPath.ShouldEqual(certpath1);
            sslOption1.CertPassphrase.ShouldEqual(passphrase1);
            sslOption1.Enabled.ShouldEqual(Boolean.Parse(enabled1));

            sslOption2.ServerName.ShouldEqual(server2);
            sslOption2.CertPath.ShouldEqual(certpath2);
            sslOption2.CertPassphrase.ShouldEqual(passphrase2);
            sslOption2.Enabled.ShouldEqual(Boolean.Parse(enabled2));

        }

        [Test]
        public void Should_parse_empty_string_to_empty_ssloptions_enumerable()
        {
            IEnumerable<SslOption> sslOptions = null;

            Assert.DoesNotThrow(() =>
                sslOptions = SslOptionsGrammar.SslOptionsParser.Parse(String.Empty)
                );

            sslOptions.ShouldNotBeNull();
            sslOptions.ShouldBeEmpty();
        }

        [Test]
        public void Should_parse_whitespace_to_empty_ssloptions_enumerable()
        {
            IEnumerable<SslOption> sslOptions = null;

            Assert.DoesNotThrow(() =>
                sslOptions = SslOptionsGrammar.SslOptionsParser.Parse("  ")
                );

            sslOptions.ShouldNotBeNull();
            sslOptions.ShouldBeEmpty();
        }
    }
    
}

// ReSharper restore InconsistentNaming