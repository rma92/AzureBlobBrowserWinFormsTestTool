using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Win32;

namespace FOTATestManagementDesktopApplication
{
    public partial class Form1 : Form
    {
        CloudStorageAccount myStorageAccount = null;
        CloudBlobClient myClient = null;
        CloudBlobContainer myContainer = null;

        RegistryKey settingsKey = null;
        public Form1()
        {
            InitializeComponent();
            settingsKey = Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey("RM.VG").CreateSubKey("AzureBlobBrowser");
            var LastUsedConnectionString = settingsKey.GetValue("LastUsedConnectionString");
            if (LastUsedConnectionString != null)
            {
                AzureStorageConnectionString.Text = LastUsedConnectionString.ToString();
            }
        }

        private void disconnect()
        {
            //stop any tasks.
            myClient = null;
            myStorageAccount = null;
            myContainer = null;
            containerNameLabel.Text = "{disconnected}";
        }
        private void connect(string connectionString)
        {
            myStorageAccount = CloudStorageAccount.Parse(connectionString);
            myClient = myStorageAccount.CreateCloudBlobClient();
            var containerList = myClient.ListContainers();
            ListContainersDialog dlg = new ListContainersDialog();
            dlg.prep("Select the container to connect to.", "Blob Container");
            foreach (var a in containerList)
            {
                dlg.listbox.Items.Add(a.Name);
            }
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                myContainer = myClient.GetContainerReference(dlg.listbox.SelectedItem.ToString());
                containerNameLabel.Text = dlg.listbox.SelectedItem.ToString();
                prefixTextbox.Text = "/";
                cdTo_ui();
            }
            else
            {
                MessageBox.Show(this, "No container selected.  Disconnecting.");
                disconnect();
            }

        }
        private void connect_ui()
        {
            if (myStorageAccount != null || myClient != null)
            {
                DialogResult mbres = MessageBox.Show(this, "There is already a storage account opened. Close it and continue?", "Already connected", MessageBoxButtons.YesNo);
                if (mbres == DialogResult.Yes)
                {
                    disconnect();
                }
                else
                {
                    return;
                }
            }
            //save the connection string in the registry.
            settingsKey.SetValue("LastUsedConnectionString", this.AzureStorageConnectionString.Text);
            connect(this.AzureStorageConnectionString.Text);
        }
        private void cdTo(string prefix = "", bool flatListing = false)
        {
            if ( prefix.Length >0 && prefix[0] == '/' )
            {
                prefix = prefix.Substring(1);
            }
            if (myContainer == null)
            {
                MessageBox.Show(this, "You need to connect to a blob container first!");
                return;
            }
            this.listView1.Clear();
            foreach (IListBlobItem a in myContainer.ListBlobs(useFlatBlobListing: flatListing, prefix: prefix))
            {
                if (a is CloudBlockBlob)
                {
                    String blobName = ((CloudBlockBlob)a).Name;
                    int liS = blobName.LastIndexOf('/');
                    if( liS >= 0 && liS < blobName.Length -2)
                    {
                        blobName = blobName.Substring(liS+1);
                    }
                    ListViewItem i = new ListViewItem(blobName, 0);
                    i.ToolTipText = ((CloudBlockBlob)a).Name;
                    this.listView1.Items.Add(i);
                    //this.listView1.Items.Add(((CloudBlockBlob)a).Name, 0, );
                }
                else if (a is CloudBlobDirectory)
                {
                    var s2 = ((CloudBlobDirectory)a).Uri.ToString().Substring(myContainer.Uri.ToString().Length + 1);
                    var blobDirName = s2;
                    int liS = blobDirName.LastIndexOf('/');
                    if (liS >= 0 && liS < blobDirName.Length - 2)
                    {
                        blobDirName = blobDirName.Substring(liS + 1);
                    }
                    ListViewItem i = new ListViewItem(blobDirName, 1);
                    i.ToolTipText = blobDirName;
                    this.listView1.Items.Add(i);
                }
                else
                {
                    this.listView1.Items.Add("$$");
                }
            }
        }
        private void cdTo_ui()
        {
            cdTo(prefixTextbox.Text, useFlatListingToolStripMenuItem.Checked);
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            connect_ui();
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            disconnect();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void toolStripLabel2_Click(object sender, EventArgs e)
        {

        }

        private void cdButton_Click(object sender, EventArgs e)
        {
            cdTo_ui();
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            connect_ui();
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var sel = listView1.SelectedIndices;
            if (sel.Count > 0)
            {
                if (listView1.Items[sel[0]].ImageIndex == 1)
                { //dir
                    //MessageBox.Show(listView1.Items[sel[0]].Text);
                    prefixTextbox.Text = '/' + listView1.Items[sel[0]].Text;
                    cdTo_ui();
                }
                else if (listView1.Items[sel[0]].ImageIndex == 0)
                {//file
                    MessageBox.Show(listView1.Items[sel[0]].Text);
                }
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if(prefixTextbox.Text.LastIndexOf('/') != prefixTextbox.Text.IndexOf('/'))
            {
                prefixTextbox.Text = prefixTextbox.Text.Substring(0, prefixTextbox.Text.LastIndexOf('/', prefixTextbox.Text.LastIndexOf('/')-1)) + '/';
                cdTo_ui();
            }
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void getFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                MessageBox.Show(listView1.SelectedItems[0].ToolTipText);
                CloudBlockBlob blob = myContainer.GetBlockBlobReference(listView1.SelectedItems[0].ToolTipText);

                SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
                sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
                sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(12);
                sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List;

                string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);
                MessageBox.Show(this, blob.Uri + sasBlobToken, "url");
            }
        }

        private void addFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string curUri = "";
            //attempt to get the current directory.
            if(listView1.Items.Count > 0)
            {
                CloudBlockBlob blob = myContainer.GetBlockBlobReference(listView1.Items[0].ToolTipText);
                //curUri = blob.Uri.ToString();
                curUri = blob.Name;
                if( curUri.LastIndexOf('/') != -1)
                {
                    curUri = curUri.Substring(0, curUri.LastIndexOf('/'));
                }
            }

            UploadDataForm f = new UploadDataForm();
            f.pathTextBox.Text = curUri;
show_upload_dialog_location:
            if( f.ShowDialog(this) == DialogResult.OK)
            {
                //upload the file.
                try
                {
                    CloudBlockBlob blob = myContainer.GetBlockBlobReference(f.pathTextBox.Text);
                    if (blob.Exists())
                    {
                        MessageBox.Show(this, "There is already a blob at that location.  Use the replace blob function to replace it, or change the name of the blob.");
                        goto show_upload_dialog_location;
                    }
                    else
                    {
                        CloudBlobStream blobWriteStream = blob.OpenWrite();
                        var mS = new MemoryStream(f.contents);
                        mS.CopyTo(blobWriteStream);
                        blobWriteStream.Flush();
                        blobWriteStream.Dispose();
                        MessageBox.Show("Wrote file");
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show( this, ex.Message, "Error uploading file");
                }
                cdTo_ui();
            }
        }

        private void replaceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloudBlockBlob blob = myContainer.GetBlockBlobReference(listView1.SelectedItems[0].ToolTipText);
            UploadDataForm f = new UploadDataForm();
            
            f.pathTextBox.Text = listView1.SelectedItems[0].ToolTipText;
            f.pathTextBox.Enabled = false;
            blob.FetchAttributes();
            f.contents = new Byte[blob.Properties.Length];
            try
            {
                blob.DownloadToByteArray(f.contents, 0, null, null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error opening blob contents to edit");
                return;
            }
           
            show_upload_dialog_location:
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                //upload the file.
                try
                {
                    if (!blob.Exists())
                    {
                        MessageBox.Show(this, "There is not a blob at that location.  Use the add blob function to add one, or change the name of the blob.");
                        goto show_upload_dialog_location;
                    }
                    else
                    {
                        CloudBlobStream blobWriteStream = blob.OpenWrite();
                        var mS = new MemoryStream(f.contents);
                        mS.CopyTo(blobWriteStream);
                        blobWriteStream.Flush();
                        blobWriteStream.Dispose();
                        MessageBox.Show("Wrote file");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error uploading file");
                }
                cdTo_ui();
            }
        }

        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                MessageBox.Show(listView1.SelectedItems[0].ToolTipText);
                CloudBlockBlob blob = myContainer.GetBlockBlobReference(listView1.SelectedItems[0].ToolTipText);
                if (blob.Exists())
                {
                    if( MessageBox.Show(this, "Really delete the blob '" + listView1.SelectedItems[0].ToolTipText + "'?", "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        blob.Delete();
                    }
                }
            }
            cdTo_ui();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                deleteFileToolStripMenuItem.Enabled = true;
                replaceFileToolStripMenuItem.Enabled = true;
                getFileDirectlyToolStripMenuItem.Enabled = true;
                getFileToolStripMenuItem.Enabled = true;
            }
            else
            {
                deleteFileToolStripMenuItem.Enabled = false;
                replaceFileToolStripMenuItem.Enabled = false;
                getFileDirectlyToolStripMenuItem.Enabled = false;
                getFileToolStripMenuItem.Enabled = false;
            }
        }

        private void getFileDirectlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                MessageBox.Show(listView1.SelectedItems[0].ToolTipText);
                CloudBlockBlob blob = myContainer.GetBlockBlobReference(listView1.SelectedItems[0].ToolTipText);
                
                UploadDataForm f = new UploadDataForm();
                blob.FetchAttributes();
                f.contents = new Byte[blob.Properties.Length];
                blob.DownloadToByteArray(f.contents, 0, null, null, null);
                f.pathTextBox.Text = listView1.SelectedItems[0].ToolTipText;
                f.saveButton.Enabled = false;
                f.ShowDialog(this);
            }
        }

        //UI Get an SAS URL to add file.
        private void addFileSASToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string curUri = "";
            //attempt to get the current directory.
            if (listView1.Items.Count > 0)
            {
                CloudBlockBlob blobDir = myContainer.GetBlockBlobReference(listView1.Items[0].ToolTipText);
                //curUri = blob.Uri.ToString();
                curUri = blobDir.Name;
                if (curUri.LastIndexOf('/') != -1)
                {
                    curUri = curUri.Substring(0, curUri.LastIndexOf('/'));
                }
            }
            InputBox ib1 = new InputBox();
            ib1.label1.Text = "Please enter the blob name for the new blob.";
            ib1.textBox1.Text = curUri;

            if (ib1.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    curUri = ib1.textBox1.Text;
                    //check that the blob does not exist.

                    CloudBlockBlob blob = myContainer.GetBlockBlobReference(curUri);
                    if (blob.Exists())
                    {
                        MessageBox.Show(this, "There's already a blob at '" + curUri + "'.");
                        return;
                    }
                    SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
                    sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
                    sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
                    sasConstraints.Permissions = SharedAccessBlobPermissions.Write;

                    string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);
                    MessageBox.Show(this, "To upload the file, perform an HTTP PUT request to the URL provided with the file contents in the contents." +
                        "You must include the following header:\n" +
                        "x-ms-blob-type: BlockBlob" +
                        "\n\n" + blob.Uri + sasBlobToken, "url");
                }
                catch(Exception ex)
                {
                    MessageBox.Show(this, "There was an error created the SAS token: " + ex.Message);
                }
            }
        }
    }
}
