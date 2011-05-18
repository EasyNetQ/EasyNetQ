using System;
using System.Windows.Forms;
using EasyNetQ.Monitor.Controllers;

namespace EasyNetQ.Monitor.Ui
{
    public interface IMessageTypeNodeFactory
    {
        MessageTypeNode Create(IMessageTypeController messageTypeController);
    }

    public class MessageTypeNode : TreeNode
    {
        private readonly IMessageTypeController messageTypeController;

        public MessageTypeNode(IMessageTypeController messageTypeController)
        {
            this.messageTypeController = messageTypeController;
            this.Text = messageTypeController.GetMessageTypeName();
        }
    }
}