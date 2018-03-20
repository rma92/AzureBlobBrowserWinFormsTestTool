using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FOTATestManagementDesktopApplication
{
    public partial class UploadDataForm : Form
    {
        public byte[] contents = new byte[4096];
        public UploadDataForm()
        {
            InitializeComponent();
        }

        private void UploadDataForm_Load(object sender, EventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (textBox1.Enabled)
            {
                contents = Encoding.ASCII.GetBytes(textBox1.Text);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void UploadDataForm_Shown(object sender, EventArgs e)
        {
            //If the contents is not enormous (<1MB), convert it to text
            //for user convenience and readability. 
            //The textbox will be disabled, to prevent the user from accidentally
            //corrupting it.
            //Textbox1.Enabled being false also means that the correct data is
            //currently stored in the contents byte array.
            if (contents.Length < 1048600)
            {
                textBox1.Text = System.Text.Encoding.Default.GetString(contents);
                textBox1.Enabled = false;
            }
        }

        //Handle close to save the data.
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            //If textBox1 is enabled, text content has been edited.  
            //Save it in the contentsbyte array.
            textBox1.Enabled = true;
        }

        private void LoadFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*";
            of.FilterIndex = 2;

            if( of.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var myStream = of.OpenFile();
                    using (var streamReader = new MemoryStream())
                    {
                        myStream.CopyTo(streamReader);
                        contents = streamReader.ToArray();
                        myStream.Dispose();
                        streamReader.Dispose();

                        if (contents.Length < 1048600)
                        {
                            textBox1.Text = System.Text.Encoding.Default.GetString(contents);
                            textBox1.Enabled = false;
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(this,"Failed to open file");
                }
            }
        }

        private void SaveFileButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            if( sf.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var myStream = sf.OpenFile();
                    //convert text to binary if needed.
                    if (textBox1.Enabled)
                    {
                        contents = Encoding.ASCII.GetBytes(textBox1.Text);
                    }

                    using (var memStream = new MemoryStream(contents))
                    {
                        memStream.WriteTo(myStream);
                        myStream.Flush();
                        memStream.Dispose();
                        myStream.Dispose();
                        MessageBox.Show(this, "File saved.");
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(this, "failed to save file:" + ex.Message);
                }
            }
        }
    }
}
