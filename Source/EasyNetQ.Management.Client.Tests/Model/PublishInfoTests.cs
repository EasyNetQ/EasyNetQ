// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests.Model
{
    [TestFixture]
    public class PublishInfoTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "payloadEncoding must be one of: 'string, base64'")]
        public void Should_throw_when_payload_encoding_is_incorrect()
        {
            new PublishInfo(new Dictionary<string, string>(), "routing_key", "payload", "unknown_payload_encoding");
        }
    }
}

// ReSharper restore InconsistentNaming