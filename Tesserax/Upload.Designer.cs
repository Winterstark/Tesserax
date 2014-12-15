namespace Tesserax
{
    partial class Upload
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtTitle = new System.Windows.Forms.TextBox();
            this.lblDesc = new System.Windows.Forms.Label();
            this.txtDesc = new System.Windows.Forms.TextBox();
            this.bttUpload = new System.Windows.Forms.Button();
            this.picSpinner = new System.Windows.Forms.PictureBox();
            this.cmbImgs = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbOpenLink = new System.Windows.Forms.ComboBox();
            this.cmbCopyLink = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picSpinner)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(9, 42);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(58, 13);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Image title:";
            // 
            // txtTitle
            // 
            this.txtTitle.Location = new System.Drawing.Point(73, 39);
            this.txtTitle.Name = "txtTitle";
            this.txtTitle.Size = new System.Drawing.Size(157, 20);
            this.txtTitle.TabIndex = 2;
            this.txtTitle.TextChanged += new System.EventHandler(this.txtTitle_TextChanged);
            // 
            // lblDesc
            // 
            this.lblDesc.AutoSize = true;
            this.lblDesc.Location = new System.Drawing.Point(9, 68);
            this.lblDesc.Name = "lblDesc";
            this.lblDesc.Size = new System.Drawing.Size(93, 13);
            this.lblDesc.TabIndex = 0;
            this.lblDesc.Text = "Image description:";
            // 
            // txtDesc
            // 
            this.txtDesc.Location = new System.Drawing.Point(12, 84);
            this.txtDesc.Multiline = true;
            this.txtDesc.Name = "txtDesc";
            this.txtDesc.Size = new System.Drawing.Size(218, 94);
            this.txtDesc.TabIndex = 3;
            this.txtDesc.TextChanged += new System.EventHandler(this.txtDesc_TextChanged);
            // 
            // bttUpload
            // 
            this.bttUpload.Location = new System.Drawing.Point(236, 12);
            this.bttUpload.Name = "bttUpload";
            this.bttUpload.Size = new System.Drawing.Size(116, 47);
            this.bttUpload.TabIndex = 4;
            this.bttUpload.Text = "Upload";
            this.bttUpload.UseVisualStyleBackColor = true;
            this.bttUpload.Click += new System.EventHandler(this.bttUpload_Click);
            // 
            // picSpinner
            // 
            this.picSpinner.Image = global::Tesserax.Properties.Resources.loadinfo_net;
            this.picSpinner.Location = new System.Drawing.Point(236, 84);
            this.picSpinner.Name = "picSpinner";
            this.picSpinner.Size = new System.Drawing.Size(116, 94);
            this.picSpinner.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picSpinner.TabIndex = 3;
            this.picSpinner.TabStop = false;
            this.picSpinner.Visible = false;
            // 
            // cmbImgs
            // 
            this.cmbImgs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbImgs.FormattingEnabled = true;
            this.cmbImgs.Location = new System.Drawing.Point(12, 12);
            this.cmbImgs.Name = "cmbImgs";
            this.cmbImgs.Size = new System.Drawing.Size(218, 21);
            this.cmbImgs.TabIndex = 1;
            this.cmbImgs.SelectedIndexChanged += new System.EventHandler(this.cmbImgs_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 210);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "After uploading open:";
            // 
            // cmbOpenLink
            // 
            this.cmbOpenLink.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOpenLink.FormattingEnabled = true;
            this.cmbOpenLink.Items.AddRange(new object[] {
            "Nothing",
            "Image",
            "Image with embed codes"});
            this.cmbOpenLink.Location = new System.Drawing.Point(126, 207);
            this.cmbOpenLink.Name = "cmbOpenLink";
            this.cmbOpenLink.Size = new System.Drawing.Size(226, 21);
            this.cmbOpenLink.TabIndex = 6;
            this.cmbOpenLink.SelectedIndexChanged += new System.EventHandler(this.cmbOpenLink_SelectedIndexChanged);
            // 
            // cmbCopyLink
            // 
            this.cmbCopyLink.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCopyLink.FormattingEnabled = true;
            this.cmbCopyLink.Items.AddRange(new object[] {
            "Nothing",
            "Imgur link",
            "Direct link",
            "Markdown",
            "HTML",
            "BBCode",
            "Linked BBCode"});
            this.cmbCopyLink.Location = new System.Drawing.Point(126, 234);
            this.cmbCopyLink.Name = "cmbCopyLink";
            this.cmbCopyLink.Size = new System.Drawing.Size(226, 21);
            this.cmbCopyLink.TabIndex = 8;
            this.cmbCopyLink.SelectedIndexChanged += new System.EventHandler(this.cmbCopyLink_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 237);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Copy into clipboard:";
            // 
            // Upload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 270);
            this.Controls.Add(this.cmbCopyLink);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbOpenLink);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbImgs);
            this.Controls.Add(this.picSpinner);
            this.Controls.Add(this.bttUpload);
            this.Controls.Add(this.txtDesc);
            this.Controls.Add(this.lblDesc);
            this.Controls.Add(this.txtTitle);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Upload";
            this.Text = "Upload";
            this.Load += new System.EventHandler(this.Upload_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picSpinner)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TextBox txtTitle;
        private System.Windows.Forms.Label lblDesc;
        private System.Windows.Forms.TextBox txtDesc;
        private System.Windows.Forms.Button bttUpload;
        private System.Windows.Forms.PictureBox picSpinner;
        private System.Windows.Forms.ComboBox cmbImgs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbOpenLink;
        private System.Windows.Forms.ComboBox cmbCopyLink;
        private System.Windows.Forms.Label label2;
    }
}