using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EasyNetQ.Monitor.Dialogue
{
    public partial class TypeChooserForm : Form
    {
        public static IEnumerable<string> GetChosenTypes(IEnumerable<string> typesToChooseFrom)
        {
            var typeChooserForm = new TypeChooserForm();
            foreach (var typeName in typesToChooseFrom)
            {
                typeChooserForm.assemblyTypeList.Items.Add(typeName);
            }

            if (typeChooserForm.ShowDialog() == DialogResult.OK)
            {
                foreach (var checkedItem in typeChooserForm.assemblyTypeList.CheckedItems)
                {
                    yield return (string) checkedItem;
                }
            }
        }

        public TypeChooserForm()
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
