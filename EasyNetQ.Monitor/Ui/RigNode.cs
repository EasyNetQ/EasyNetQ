using System;
using System.Windows.Forms;
using EasyNetQ.Monitor.Controllers;
using EasyNetQ.Monitor.Dialogue;

namespace EasyNetQ.Monitor.Ui
{
    public class RigNode : TreeNode
    {
        private readonly IRigController rigController;
        private readonly IVHostNodeFactory vHostNodeFactory;

        public RigNode(IRigController rigController, IVHostNodeFactory vHostNodeFactory)
        {
            this.Text = "Root";
            CreateContextMenu();
            this.rigController = rigController;
            this.vHostNodeFactory = vHostNodeFactory;
        }

        private void CreateContextMenu()
        {
            ContextMenuStrip = new ContextMenuStrip();
            ContextMenuStrip.Items.Add("Add New vHost", null, (source, args) =>
            {
                var vHostName = NewVHostForm.GetVHostName();
                if (vHostName.WasCancelled) return;

                var vHostController = rigController.CreateNewVHost(vHostName.Value);
                var vHostNode = vHostNodeFactory.Create(vHostController);
                this.Nodes.Add(vHostNode);
            });
        }

        public void SaveRig()
        {
            rigController.SaveRig();
        }
    }
}