using System.Linq;
using System.Windows.Forms;
using EasyNetQ.Monitor.Controllers;
using EasyNetQ.Monitor.Dialogue;
using EasyNetQ.Monitor.Model;
using EasyNetQ.Monitor.Services;

namespace EasyNetQ.Monitor.Ui
{
    public interface IVHostNodeFactory
    {
        VHostNode Create(IVHostController vHostController);
    }

    public class VHostNode : TreeNode
    {
        private readonly IVHostController vHostController;
        private readonly IMessageTypeNodeFactory messageTypeNodeFactory;

        public VHostNode(
            IVHostController vHostController, 
            IMessageTypeNodeFactory messageTypeNodeFactory)
        {
            this.vHostController = vHostController;
            this.messageTypeNodeFactory = messageTypeNodeFactory;
            this.Text = vHostController.GetVHostName();
            CreateContextMenu();
        }

        private void CreateContextMenu()
        {
            ContextMenuStrip = new ContextMenuStrip();
            ContextMenuStrip.Items.Add("Add Message Types From Assembly", null, (source, args) =>
            {
                var openFileDialogue = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Filter = "Assemblies (*.dll)|*.dll"
                };
                if (openFileDialogue.ShowDialog() == DialogResult.OK)
                {
                    var assemblyLoader = new AssemblyLoader();
                    var types = assemblyLoader.GetTypes(openFileDialogue.FileName);
                    var chosenTypes = TypeChooserForm.GetChosenTypes(types);

                    var messageTypeNodes = vHostController.CreateMessageTypes(chosenTypes)
                        .Select(x => messageTypeNodeFactory.Create(x)).ToArray();

                    Nodes.AddRange(messageTypeNodes);
                }
            });
        }
    }
}