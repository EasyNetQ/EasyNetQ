// ReSharper disable InconsistentNaming

using System;
using System.Runtime.Serialization;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    [TestFixture]
    public class When_an_action_is_invoked_that_throws
    {
        private IClientCommandDispatcher dispatcher;
        private IPersistentChannel channel;

        [SetUp]
        public void SetUp()
        {
            var connection = MockRepository.GenerateStub<IPersistentConnection>();
            var channelFactory = MockRepository.GenerateStub<IPersistentChannelFactory>();
            channel = MockRepository.GenerateStub<IPersistentChannel>();

            channelFactory.Stub(x => x.CreatePersistentChannel(connection)).Return(channel);
            channel.Stub(x => x.InvokeChannelAction(null)).IgnoreArguments().WhenCalled(
                x => ((Action<IModel>)x.Arguments[0])(null));

            dispatcher = new ClientCommandDispatcher(connection, channelFactory);

        }

        [TearDown]
        public void TearDown()
        {
            dispatcher.Dispose();
        }

        [Test]
        public void Should_raise_the_exception_on_the_calling_thread()
        {
            var exception = new CrazyTestOnlyException();
            
            var task = dispatcher.Invoke(x =>
            {
                throw exception;
            });

            try
            {
                task.Wait();
            }
            catch (AggregateException aggregateException)
            {
                aggregateException.InnerException.ShouldBeTheSameAs(exception);
            }
        }

        [Serializable]
        private class CrazyTestOnlyException : Exception
        {
            //
            // For guidelines regarding the creation of new exception types, see
            //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
            // and
            //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
            //

            public CrazyTestOnlyException()
            {
            }

            public CrazyTestOnlyException(string message) : base(message)
            {
            }

            public CrazyTestOnlyException(string message, Exception inner) : base(message, inner)
            {
            }

            protected CrazyTestOnlyException(
                SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
        }
    }
}

// ReSharper restore InconsistentNaming