using System.Windows.Forms;

namespace EasyNetQ.Monitor.Dialogue
{
    public partial class NewVHostForm : Form
    {
        public static FromDialogue<string> GetVHostName()
        {
            var newVHostForm = new NewVHostForm();
            return newVHostForm.ShowDialog() == DialogResult.OK ?
                FromDialogue<string>.OK(newVHostForm.nameTextBox.Text) : 
                FromDialogue<string>.Cancelled();
        }

        public NewVHostForm()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
