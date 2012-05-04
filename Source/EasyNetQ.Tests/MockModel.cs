using System;
using System.Collections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests
{
    public class MockModel : IModel
    {
        public Action AbortAction { get; set; }
        public Action<uint, ushort, bool> BasicQosAction { get; set; }
        public Action<string, string, IBasicProperties, byte[]> BasicPublishAction { get; set; }
        public Func<string, bool, bool, bool, IDictionary, QueueDeclareOk> QueueDeclareAction { get; set; }
        public Action<string, string, string> QueueBindAction { get; set; }
        public Func<string, bool, string, IBasicConsumer, string> BasicConsumeAction { get; set; }
        public Action<string, string, bool, bool, IDictionary> ExchangeDeclareAction { get; set; }

        public MockModel()
        {
            // create some innocent defaults

            AbortAction = () => { };
            BasicQosAction = (a, b, c) => { };
            BasicPublishAction = (a, b, c, d) => { };
            QueueDeclareAction = (a, b, c, d, e) => new QueueDeclareOk("some queue", 0, 0);
            QueueBindAction = (a, b, c) => { };
            BasicConsumeAction = (a, b, c, d) => "";
            ExchangeDeclareAction = (a, b, c, d, e) => { };
        }

        public void Dispose()
        {
            
        }

        public IBasicProperties CreateBasicProperties()
        {
            return new MockBasicProperties();
        }

        public IFileProperties CreateFileProperties()
        {
            throw new NotImplementedException();
        }

        public IStreamProperties CreateStreamProperties()
        {
            throw new NotImplementedException();
        }

        public void ChannelFlow(bool active)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments)
        {
            ExchangeDeclareAction(exchange, type, durable, autoDelete, arguments);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
        	ExchangeDeclareAction(exchange, type, durable, true, null);
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDelete(string exchange)
        {
            throw new NotImplementedException();
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            throw new NotImplementedException();
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclare()
        {
            throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            return QueueDeclareAction(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void QueueBind(string queue, string exchange, string routingKey)
        {
            QueueBindAction(queue, exchange, routingKey);
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public uint QueuePurge(string queue)
        {
            throw new NotImplementedException();
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            throw new NotImplementedException();
        }

        public uint QueueDelete(string queue)
        {
            throw new NotImplementedException();
        }

        public void ConfirmSelect()
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms()
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie()
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IBasicConsumer consumer)
        {
            return BasicConsumeAction(queue, noAck, consumerTag, consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public void BasicCancel(string consumerTag)
        {
            throw new NotImplementedException();
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            BasicQosAction(prefetchSize, prefetchCount, global);
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            if(BasicPublishAction == null)
            {
                throw new NullReferenceException("BasicPublishAction is null");
            }

            BasicPublishAction(exchange, routingKey, basicProperties, body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            throw new NotImplementedException();
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicRecover(bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicRecoverAsync(bool requeue)
        {
            throw new NotImplementedException();
        }

        public BasicGetResult BasicGet(string queue, bool noAck)
        {
            throw new NotImplementedException();
        }

        public void TxSelect()
        {
            throw new NotImplementedException();
        }

        public void TxCommit()
        {
            throw new NotImplementedException();
        }

        public void TxRollback()
        {
            throw new NotImplementedException();
        }

        public void DtxSelect()
        {
            throw new NotImplementedException();
        }

        public void DtxStart(string dtxIdentifier)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Close(ushort replyCode, string replyText)
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            AbortAction();
        }

        public void Abort(ushort replyCode, string replyText)
        {
            throw new NotImplementedException();
        }

        public IBasicConsumer DefaultConsumer
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ShutdownEventArgs CloseReason
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsOpen
        {
            get { throw new NotImplementedException(); }
        }

        public ulong NextPublishSeqNo
        {
            get { throw new NotImplementedException(); }
        }

        public event ModelShutdownEventHandler ModelShutdown;
        public event BasicReturnEventHandler BasicReturn;
        public event BasicAckEventHandler BasicAcks;
        public event BasicNackEventHandler BasicNacks;
        public event CallbackExceptionEventHandler CallbackException;
        public event FlowControlEventHandler FlowControl;
        public event BasicRecoverOkEventHandler BasicRecoverOk;


        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}