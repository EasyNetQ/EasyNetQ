using System;
using System.Collections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#pragma warning disable 67

namespace EasyNetQ.Tests
{
    public class MockConnection : IConnection
    {
        public Func<IModel> CreateModelAction { get; set; } 
        public Action DisposeAction { get; set;  }

        public MockConnection(IModel model)
        {
            CreateModelAction = () => model;
            DisposeAction = () => { };
        }

        public void Dispose()
        {
            DisposeAction();
        }

        public IModel CreateModel()
        {
            return CreateModelAction();
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public void Close(ushort reasonCode, string reasonText)
        {
            throw new System.NotImplementedException();
        }

        public void Close(int timeout)
        {
            throw new System.NotImplementedException();
        }

        public void Close(ushort reasonCode, string reasonText, int timeout)
        {
            throw new System.NotImplementedException();
        }

        public void Abort()
        {
            throw new System.NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText)
        {
            throw new System.NotImplementedException();
        }

        public void Abort(int timeout)
        {
            throw new System.NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText, int timeout)
        {
            throw new System.NotImplementedException();
        }

        public AmqpTcpEndpoint Endpoint
        {
            get { throw new System.NotImplementedException(); }
        }

        public IProtocol Protocol
        {
            get { throw new System.NotImplementedException(); }
        }

        public ushort ChannelMax
        {
            get { throw new System.NotImplementedException(); }
        }

        public uint FrameMax
        {
            get { throw new System.NotImplementedException(); }
        }

        public ushort Heartbeat
        {
            get { throw new System.NotImplementedException(); }
        }

        public IDictionary ClientProperties
        {
            get { throw new System.NotImplementedException(); }
        }

        public IDictionary ServerProperties
        {
            get { throw new System.NotImplementedException(); }
        }

        public AmqpTcpEndpoint[] KnownHosts
        {
            get { throw new System.NotImplementedException(); }
        }

        public ShutdownEventArgs CloseReason
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsOpen
        {
            get { return true; }
        }

        public bool AutoClose
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public IList ShutdownReport
        {
            get { throw new System.NotImplementedException(); }
        }

        public event ConnectionShutdownEventHandler ConnectionShutdown;
        public event CallbackExceptionEventHandler CallbackException;
    }
}

#pragma warning restore 67