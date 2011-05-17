using System.Windows.Forms;
using EasyNetQ.Monitor.Dialogue;
using EasyNetQ.Monitor.Model;

namespace EasyNetQ.Monitor.Ui
{
    public class RigNode : TreeNode
    {
        private Rig rig;

        public RigNode(Rig rig)
        {
            this.rig = rig;
            this.Text = "Root";
            CreateContextMenu();
        }

        private void CreateContextMenu()
        {
            ContextMenuStrip = new ContextMenuStrip();
            ContextMenuStrip.Items.Add("Add New vHost", null, (source, args) =>
            {
                var vHostName = NewVHostForm.GetVHostName();
                if (vHostName.WasCancelled) return;

                var vHost = rig.AddVHost(vHostName.Value);
                var vHostNode = new VHostNode(vHost);
                this.Nodes.Add(vHostNode);
            });
        }
    }
}