// ReSharper disable InconsistentNaming

using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConnectionErrorConditionsTests
    {
        [SetUp]
        public void SetUp() {}

        [Test, Explicit("Tries to make a connection to a RabbitMQ Broker")]
        public void Should_write_a_useful_error_message_when_connetion_fails()
        {
            RabbitHutch.CreateBus("localhost_not", "guest", "guest");
            Thread.Sleep(2000);
        } 

        [Test, Explicit("Tries to make a connection to a RabbitMQ Broker")]
        public void Should_write_a_useful_error_message_when_VHost_does_not_exist()
        {
            RabbitHutch.CreateBus("localhost/not", "guest", "guest");
            Thread.Sleep(2000);
        } 

        [Test, Explicit("Tries to make a connection to a RabbitMQ Broker")]
        public void Should_write_a_useful_error_message_when_credentials_are_incorrect()
        {
            RabbitHutch.CreateBus("localhost", "guest", "wrong_password");
            Thread.Sleep(2000);
        } 
    }
}

// ReSharper restore InconsistentNaming