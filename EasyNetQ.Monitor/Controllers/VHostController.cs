using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.Monitor.Model;

namespace EasyNetQ.Monitor.Controllers
{
    public interface IVHostControllerFactory
    {
        IVHostController Create(VHost vHost);
    }

    public interface IVHostController
    {
        IEnumerable<IMessageTypeController> CreateMessageTypes(IEnumerable<string> messageTypeNames);

        string GetVHostName();
    }

    public class VHostController : IVHostController
    {
        private readonly IMessageTypeControllerFactory messageTypeControllerFactory;
        private readonly VHost vHost;
        public VHostController(IMessageTypeControllerFactory messageTypeControllerFactory, VHost vHost)
        {
            this.vHost = vHost;
            this.messageTypeControllerFactory = messageTypeControllerFactory;
        }

        public IEnumerable<IMessageTypeController> CreateMessageTypes(IEnumerable<string> messageTypeNames)
        {
            return messageTypeNames
                .Select(messageTypeName => vHost.CreateMessageType(messageTypeName))
                .Select(messageType => messageTypeControllerFactory.Create(messageType));
        }

        public string GetVHostName()
        {
            return vHost.Name;
        }
    }
}