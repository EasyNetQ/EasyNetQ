using System.Windows.Forms;
using EasyNetQ.Monitor.Dialogue;
using EasyNetQ.Monitor.Model;
using EasyNetQ.Monitor.Services;

namespace EasyNetQ.Monitor.Ui
{
    public class VHostNode : TreeNode
    {
        private VHost vHost;

        public VHostNode(VHost vHost)
        {
            this.vHost = vHost;
            this.Text = vHost.Name;
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

                    foreach (var chosenType in chosenTypes)
                    {
                        var messageType = vHost.CreateMessageType(chosenType);
                        var messageTypeNode = new MessageTypeNode(messageType);
                        Nodes.Add(messageTypeNode);
                    }
                }
            });
        }
    }
}