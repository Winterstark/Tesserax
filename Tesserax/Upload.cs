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
        public bool uploadToReddit, openingConfig;

        BackgroundWorker uploader;
        string[] imgPaths, titles, descs;
        string openLinkPreference, copyLinkPreference, albumOpenLinkPreference, albumCopyLinkPreference;


        void saveUploadConfig()
        {
            if (openLinkPreference == "")
                openLinkPreference = "Image";
            if (copyLinkPreference == "")
                copyLinkPreference = "Nothing";
            if (albumOpenLinkPreference == "")
                albumOpenLinkPreference = "Album";
            if (albumCopyLinkPreference == "")
                albumCopyLinkPreference = "Nothing";

            StreamWriter file = new StreamWriter(Application.StartupPath + "\\uploadConfig.txt");
            file.WriteLine(openLinkPreference);
            file.WriteLine(copyLinkPreference);
            file.WriteLine(albumOpenLinkPreference);
            file.WriteLine(albumCopyLinkPreference);
            file.Close();
        }

        void openUploadConfig()
        {
            string filePath = Application.StartupPath + "\\uploadConfig.txt";

            if (File.Exists(filePath))
            {
                StreamReader file = new StreamReader(filePath);
                openLinkPreference = file.ReadLine();
                copyLinkPreference = file.ReadLine();
                albumOpenLinkPreference = file.ReadLine();
                albumCopyLinkPreference = file.ReadLine();
                file.Close();
            }
            else
            {
                //set default values
                openLinkPreference = "Image";
                copyLinkPreference = "Nothing";
                albumOpenLinkPreference = "Album";
                albumCopyLinkPreference = "Nothing";
            }

            openingConfig = true;
            if (imgPaths.Length == 1)
            {
                cmbOpenLink.Text = openLinkPreference;
                cmbCopyLink.Text = copyLinkPreference;
            }
            else
            {
                cmbOpenLink.Text = albumOpenLinkPreference;
                cmbCopyLink.Text = albumCopyLinkPreference;
            }
            openingConfig = false;
        }

        string copyLink(string link, string directImgLink)
        {
            switch (cmbCopyLink.Text)
            {
                case "Imgur link":
                    return link;
                case "Direct link":
                    return directImgLink;
                case "Markdown":
                    return String.Format("[{0}]({1})", txtTitle.Text, directImgLink);
                case "HTML":
                    return String.Format("<a href=\"{0}\"><img src=\"{1}\" title=\"source: imgur.com\" /></a>", link, directImgLink);
                case "BBCode":
                    return String.Format("[img]{0}[/img]", directImgLink);
                case "Linked BBCode":
                    return String.Format("[url={0}][img]{1}[/img][/url]", link, directImgLink);
                case "Nothing":
                default:
                    return "";
            }
        }

        string getImgurLink(string directLink)
        {
            string link = directLink.Replace("//i.", "//");
            
            if (link.Contains('.'))
                link = link.Substring(0, link.LastIndexOf('.'));

            return link;
        }

        
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

            //set post-upload actions
            openUploadConfig();

            if (uploadToReddit)
            {
                openingConfig = true;
                cmbOpenLink.Items.Clear();
                cmbOpenLink.Items.Add("Reddit submission form");
                cmbOpenLink.SelectedIndex = 0;
                cmbOpenLink.Enabled = false;
                openingConfig = false;
            }
            else if (n > 1)
            {
                openingConfig = true;
                cmbOpenLink.Items.Clear();
                cmbOpenLink.Items.Add("Nothing");
                cmbOpenLink.Items.Add("Album");
                cmbOpenLink.SelectedIndex = 1;

                openingConfig = false;
            }
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
                    {
                        if (cmbOpenLink.Text == "Album")
                            Process.Start(album);
                    }
                    else
                        Services.Reddit(album, Path.GetFileName(imgPaths[0]));

                    //copy all links
                    string clipboard = "";
                    foreach (string directLink in links)
                        clipboard += copyLink(getImgurLink(directLink), directLink) + Environment.NewLine;

                    Clipboard.SetText(clipboard);
                }
                else
                {
                    string link = getImgurLink(links[0]);

                    if (!uploadToReddit)
                    {
                        //open link
                        switch (cmbOpenLink.Text)
                        {
                            case "Nothing":
                                break;
                            case "Image":
                                Process.Start(links[0]);
                                break;
                            case "Image with embed codes":
                                Process.Start(link + "?tags");
                                break;
                        }    
                    }
                    else
                        Services.Reddit(links[0], Path.GetFileName(imgPaths[0]));

                    Clipboard.SetText(copyLink(link, links[0]));
                }

                returnLinks(links);
                this.Close();
            }
        }

        private void cmbOpenLink_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!openingConfig)
            {
                if (imgPaths.Length == 1)
                    openLinkPreference = cmbOpenLink.Text;
                else
                    albumOpenLinkPreference = cmbOpenLink.Text;

                saveUploadConfig();
            }
        }

        private void cmbCopyLink_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!openingConfig && !uploadToReddit)
            {
                if (imgPaths.Length == 1)
                    copyLinkPreference = cmbCopyLink.Text;
                else
                    albumCopyLinkPreference = cmbCopyLink.Text;

                saveUploadConfig();
            }
        }
    }
}
