// ReSharper disable InconsistentNaming

using System;
using System.Net;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using EasyNetQ.Monitor.Checks;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Monitor.Tests.Checks
{
    [TestFixture]
    public class EasyNetQErrorQueueCheckTests : CheckTestBase
    {
        private ICheck easyNetQErrorQueueCheck;
        readonly Vhost vhost = new Vhost { Name = "my_vhost" };


        protected override void DoSetUp()
        {
            easyNetQErrorQueueCheck = new EasyNetQErrorQueueCheck();
        }

        [Test]
        public void Should_alert_if_there_are_any_error_messages_in_any_error_queues()
        {
            ManagementClient.Stub(x => x.GetVHosts()).Return(new[]
            {
                vhost 
            });

            var queue = new Queue
            {
                Messages = 1
            };

            ManagementClient.Stub(x => x.GetQueue("EasyNetQ_Default_Error_Queue", vhost)).Return(queue);

            var result = easyNetQErrorQueueCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeTrue();
            result.Message.ShouldEqual("broker 'http://the.broker.com', VHost(s): 'my_vhost' has messages on the EasyNetQ error queue.");
        }

        [Test]
        public void Should_not_alert_if_there_are_no_error_messages()
        {
            ManagementClient.Stub(x => x.GetVHosts()).Return(new[]
            {
                vhost 
            });
            
            var queue = new Queue
            {
                Messages = 0
            };

            ManagementClient.Stub(x => x.GetQueue("EasyNetQ_Default_Error_Queue", vhost)).Return(queue);

            var result = easyNetQErrorQueueCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeFalse();
        }

        [Test]
        public void Should_alert_for_each_error_queue_in_each_vhost()
        {
            var vhost2 = new Vhost {Name = "my_other_vhost"};
            ManagementClient.Stub(x => x.GetVHosts()).Return(new[]
            {
                vhost,
                vhost2
            });

            var queue = new Queue
            {
                Messages = 1
            };

            ManagementClient.Stub(x => x.GetQueue("EasyNetQ_Default_Error_Queue", vhost)).Return(queue);
            ManagementClient.Stub(x => x.GetQueue("EasyNetQ_Default_Error_Queue", vhost2)).Return(queue);

            var result = easyNetQErrorQueueCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeTrue();

            result.Message.ShouldEqual("broker 'http://the.broker.com', VHost(s): 'my_vhost, my_other_vhost' has messages on the EasyNetQ error queue.");
        }

        [Test]
        public void Should_not_alert_if_there_is_no_error_queue()
        {
            ManagementClient.Stub(x => x.GetVHosts()).Return(new[]
            {
                vhost 
            });

            ManagementClient.Stub(x => x.GetQueue("EasyNetQ_Default_Error_Queue", vhost)).Throw(new UnexpectedHttpStatusCodeException(HttpStatusCode.NotFound));

            var result = easyNetQErrorQueueCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming