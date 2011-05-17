using System;
using System.Windows.Forms;
using EasyNetQ.Monitor.Model;
using EasyNetQ.Monitor.Ui;

namespace EasyNetQ.Monitor
{
    public partial class Main : Form
    {
        public Main(Rig rig)
        {
            InitializeComponent();

            var rigNode = new RigNode(rig);
            treeView.Nodes.Add(rigNode);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
