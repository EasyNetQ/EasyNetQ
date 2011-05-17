using System.Windows.Forms;
using EasyNetQ.Monitor.Model;

namespace EasyNetQ.Monitor.Ui
{
    public class MessageTypeNode : TreeNode
    {
        private MessageType messageType;

        public MessageTypeNode(MessageType messageType)
        {
            this.messageType = messageType;
            this.Text = messageType.TypeName;
        }
    }
}