using System;
using EasyNetQ.Monitor.Model;

namespace EasyNetQ.Monitor.Controllers
{
    public interface IMessageTypeControllerFactory
    {
        IMessageTypeController Create(MessageType messageType);
    }

    public interface IMessageTypeController
    {
        string GetMessageTypeName();
    }

    public class MessageTypeController : IMessageTypeController
    {
        private readonly MessageType messageType;

        public MessageTypeController(MessageType messageType)
        {
            this.messageType = messageType;
        }

        public string GetMessageTypeName()
        {
            return messageType.TypeName;
        }
    }
}