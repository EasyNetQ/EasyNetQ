// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class PublisherConfirmsTests
    {
        private PublisherConfirms publisherConfirms;
        private IModel channel;

        [SetUp]
        public void SetUp()
        {
            channel = MockRepository.GenerateStub<IModel>();
            publisherConfirms = new PublisherConfirms();
        }

        [Test]
        public void Should_mark_success()
        {
            const int sequenceNumber = 34456;
            channel.Stub(x => x.NextPublishSeqNo).Return(sequenceNumber);

            var success = false;
            var failure = false;

            Action successCallback = () => success = true;
            Action failureCallback = () => failure = true;

            publisherConfirms.RegisterCallbacks(channel, successCallback, failureCallback);

            var args = new BasicAckEventArgs
            {
                DeliveryTag = sequenceNumber,
                Multiple = false
            };

            publisherConfirms.SuccessfulPublish(channel, args);

            success.ShouldBeTrue();
            failure.ShouldBeFalse();
        }

        [Test]
        public void Should_mark_failure()
        {
            const int sequenceNumber = 34456;
            channel.Stub(x => x.NextPublishSeqNo).Return(sequenceNumber);

            var success = false;
            var failure = false;

            Action successCallback = () => success = true;
            Action failureCallback = () => failure = true;

            publisherConfirms.RegisterCallbacks(channel, successCallback, failureCallback);

            var args = new BasicNackEventArgs
            {
                DeliveryTag = sequenceNumber,
                Multiple = false
            };

            publisherConfirms.FailedPublish(channel, args);

            success.ShouldBeFalse();
            failure.ShouldBeTrue();
        }

        [Test]
        public void Should_mark_success_up_to_sequence_number_when_multiple_is_true()
        {
            const string expectedOutput = "Success0 Success1 Success2 Success3 Success4 Success5 --multiple ack of 5 complete --Failure6 Failure7 Failure8 ";
            var writer = new StringWriter();
            //var writer = Console.Out;

            Action<ulong> registerCallback = sequenceNumber =>
            {
                var model = MockRepository.GenerateStub<IModel>();
                model.Stub(x => x.NextPublishSeqNo).Return(sequenceNumber);
                publisherConfirms.RegisterCallbacks(model,
                    () => writer.Write("Success{0} ", sequenceNumber),
                    () => writer.Write("Failure{0} ", sequenceNumber));
            };

            for (ulong i = 0; i < 10; i++)
            {
                registerCallback(i);
            }

            var args = new BasicAckEventArgs
            {
                DeliveryTag = 5, // should callback success up to and including seq no 5
                Multiple = true
            };

            publisherConfirms.SuccessfulPublish(channel, args);

            writer.Write("--multiple ack of 5 complete --");

            var args2 = new BasicNackEventArgs
            {
                DeliveryTag = 8, // should callback failure for 6, 7, 8
                Multiple = true
            };

            publisherConfirms.FailedPublish(channel, args2);

            writer.ToString().ShouldEqual(expectedOutput);
        }

        [Test]
        public void Should_have_thread_safe_publish()
        {
            ulong sequenceNumber = 0;
            channel.Stub(x => x.NextPublishSeqNo).Do(new Func<ulong>(() => sequenceNumber++));

            const int repeat = 100000;
            var register = new Task(() =>
            {
                for (int i = 0; i < repeat; ++i)
                {
                    var tag = i;

                    publisherConfirms.RegisterCallbacks(channel, () => { }, () => { });

                    Task.Factory.StartNew(() => publisherConfirms.SuccessfulPublish(channel, new BasicAckEventArgs
                    {
                        DeliveryTag = (ulong)tag
                    }), TaskCreationOptions.AttachedToParent);
                }
            });

            register.Start();
            register.Wait();

            Assert.That(publisherConfirms.NumCallbacks, Is.EqualTo(0));
        }
    }
}

// ReSharper restore InconsistentNaming