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
            this.txtTitle.TabIndex = 1;
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
            this.txtDesc.TabIndex = 1;
            this.txtDesc.TextChanged += new System.EventHandler(this.txtDesc_TextChanged);
            // 
            // bttUpload
            // 
            this.bttUpload.Location = new System.Drawing.Point(236, 12);
            this.bttUpload.Name = "bttUpload";
            this.bttUpload.Size = new System.Drawing.Size(116, 47);
            this.bttUpload.TabIndex = 2;
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
            this.cmbImgs.TabIndex = 4;
            this.cmbImgs.SelectedIndexChanged += new System.EventHandler(this.cmbImgs_SelectedIndexChanged);
            // 
            // Upload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 191);
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
    }
}