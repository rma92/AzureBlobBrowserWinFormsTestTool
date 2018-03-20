using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FOTATestManagementDesktopApplication
{
    public partial class ListContainersDialog : Form
    {
        public ListContainersDialog()
        {
            InitializeComponent();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        private void ListContainersDialog_Show(object sender, EventArgs e)
        {
            if(listbox.Items.Count > 0)
            {
                listbox.SetSelected(0, true);
            }
        }
    }
}
