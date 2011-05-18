using System;
using System.Windows.Forms;
using EasyNetQ.Monitor.Ui;

namespace EasyNetQ.Monitor
{
    public partial class Main : Form
    {
        private readonly RigNode rigNode;

        public Main(RigNode rigNode)
        {
            this.rigNode = rigNode;
            InitializeComponent();

            treeView.Nodes.Add(rigNode);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rigNode.SaveRig();
        }
    }
}
