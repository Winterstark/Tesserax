using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace Tesserax
{
    public partial class Upload : Form
    {
        public Action<string[]> returnLinks;

        BackgroundWorker uploader;
        string[] imgPaths, titles, descs;
        public bool uploadToReddit;

        
        public Upload()
        {
            InitializeComponent();
        }

        private void Upload_Load(object sender, EventArgs e)
        {
            imgPaths = Misc.GetFilesInNaturalOrder(Application.StartupPath + "\\temp");
            int n = imgPaths.Length;
            if (n > 1)
                n++; //+1 because the last title/desc pair referes to the album as a whole
            titles = new string[n];
            descs = new string[n];

            for (int i = 0; i < imgPaths.Length; i++)
            {
                titles[i] = Path.GetFileNameWithoutExtension(imgPaths[i]);
                cmbImgs.Items.Add(titles[i]);

                descs[i] = "";
            }

            if (n > 1)
            {
                titles[n - 1] = "";
                descs[n - 1] = "";

                cmbImgs.Items.Add("[ALBUM]");
            }

            cmbImgs.SelectedIndex = cmbImgs.Items.Count - 1;

            //prepare background worker
            uploader = new BackgroundWorker();
            uploader.DoWork += new DoWorkEventHandler(uploader_DoWork);
            uploader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(uploader_RunWorkerCompleted);

        }

        private void cmbImgs_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtTitle.Text = titles[cmbImgs.SelectedIndex];
            txtDesc.Text = descs[cmbImgs.SelectedIndex];

            if (cmbImgs.SelectedIndex > 1 && cmbImgs.SelectedIndex == cmbImgs.Items.Count - 1)
            {
                //selected album as a whole
                lblTitle.Text = "ALBUM title:";
                lblDesc.Text = "ALBUM description:";
            }
            else
            {
                //selected an image
                lblTitle.Text = "Image title:";
                lblDesc.Text = "Image description:";
            }
        }

        private void txtTitle_TextChanged(object sender, EventArgs e)
        {
            titles[cmbImgs.SelectedIndex] = txtTitle.Text;
        }

        private void txtDesc_TextChanged(object sender, EventArgs e)
        {
            descs[cmbImgs.SelectedIndex] = txtDesc.Text;
        }

        private void bttUpload_Click(object sender, EventArgs e)
        {
            txtTitle.Enabled = false;
            txtDesc.Enabled = false;
            bttUpload.Enabled = false;
            picSpinner.Visible = true;

            uploader.RunWorkerAsync();
        }

        private void uploader_DoWork(object sender, DoWorkEventArgs e)
        {
            string album;
            string[] links = Imgur.UploadImgs(imgPaths, titles, descs, out album);
            
            e.Result = new Tuple<string[], string>(links, album);
        }

        private void uploader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Tuple<string[], string> res = (Tuple<string[], string>)e.Result;
            string[] links = res.Item1;
            string album = res.Item2;

            picSpinner.Visible = false;

            if (links[0].Contains("Upload error"))
            {
                MessageBox.Show(links[0]);

                txtTitle.Enabled = true;
                txtDesc.Enabled = true;
                bttUpload.Enabled = true;
            }
            else
            {
                if (album != "")
                {
                    if (!uploadToReddit)
                        Process.Start(album);
                    else
                        Services.Reddit(album, Path.GetFileName(imgPaths[0]));
                }
                else
                {
                    if (!uploadToReddit)
                        Process.Start(links[0]);
                    else
                        Services.Reddit(links[0], Path.GetFileName(imgPaths[0]));
                }

                returnLinks(links);

                this.Close();
            }
        }
    }
}
