// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Editor.CrashReport
{
    partial class CrashReportForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing"><c>true</c> if managed resources should be disposed; otherwise, <c>false</c>.</param>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CrashReportForm));
            this.textBoxEmail = new System.Windows.Forms.TextBox();
            this.labelEmail = new System.Windows.Forms.Label();
            this.buttonSend = new System.Windows.Forms.Button();
            this.buttonDontSend = new System.Windows.Forms.Button();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
            this.labelDescription = new System.Windows.Forms.Label();
            this.buttonViewLog = new System.Windows.Forms.Button();
            this.labelMainContent = new System.Windows.Forms.Label();
            this.labelPrivacy = new System.Windows.Forms.Label();
            this.linkPrivacyPolicy = new System.Windows.Forms.LinkLabel();
            this.button1 = new System.Windows.Forms.Button();
            this.emailCheckbox = new System.Windows.Forms.CheckBox();
            this.textBoxDescription = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // textBoxEmail
            // 
            this.textBoxEmail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxEmail.Location = new System.Drawing.Point(12, 237);
            this.textBoxEmail.Name = "textBoxEmail";
            this.textBoxEmail.Size = new System.Drawing.Size(535, 19);
            this.textBoxEmail.TabIndex = 1;
            this.textBoxEmail.TextChanged += new System.EventHandler(this.TextBoxText_Changed);
            // 
            // labelEmail
            // 
            this.labelEmail.AutoSize = true;
            this.labelEmail.Location = new System.Drawing.Point(12, 220);
            this.labelEmail.Name = "labelEmail";
            this.labelEmail.Size = new System.Drawing.Size(87, 12);
            this.labelEmail.TabIndex = 1;
            this.labelEmail.Text = "Email: (optional)";
            // 
            // buttonSend
            // 
            this.buttonSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSend.Location = new System.Drawing.Point(472, 323);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(75, 23);
            this.buttonSend.TabIndex = 3;
            this.buttonSend.Text = "Send";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.ButtonSend_Click);
            // 
            // buttonDontSend
            // 
            this.buttonDontSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDontSend.Location = new System.Drawing.Point(391, 323);
            this.buttonDontSend.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonDontSend.Name = "buttonDontSend";
            this.buttonDontSend.Size = new System.Drawing.Size(75, 23);
            this.buttonDontSend.TabIndex = 4;
            this.buttonDontSend.Text = "Don\'t Send";
            this.buttonDontSend.UseVisualStyleBackColor = true;
            this.buttonDontSend.Click += new System.EventHandler(this.ButtonDontSend_Click);
            // 
            // textBoxLog
            // 
            this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLog.Location = new System.Drawing.Point(12, 358);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLog.Size = new System.Drawing.Size(535, 162);
            this.textBoxLog.TabIndex = 5;
            // 
            // pictureBoxIcon
            // 
            this.pictureBoxIcon.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxIcon.Image")));
            this.pictureBoxIcon.Location = new System.Drawing.Point(12, 11);
            this.pictureBoxIcon.Name = "pictureBoxIcon";
            this.pictureBoxIcon.Size = new System.Drawing.Size(96, 89);
            this.pictureBoxIcon.TabIndex = 6;
            this.pictureBoxIcon.TabStop = false;
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = true;
            this.labelDescription.Location = new System.Drawing.Point(9, 109);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(372, 12);
            this.labelDescription.TabIndex = 7;
            this.labelDescription.Text = "If you have time, please describe what you were doing during the crash:";
            // 
            // buttonViewLog
            // 
            this.buttonViewLog.Location = new System.Drawing.Point(13, 322);
            this.buttonViewLog.Name = "buttonViewLog";
            this.buttonViewLog.Size = new System.Drawing.Size(75, 23);
            this.buttonViewLog.TabIndex = 5;
            this.buttonViewLog.Text = "View report";
            this.buttonViewLog.UseVisualStyleBackColor = true;
            this.buttonViewLog.Click += new System.EventHandler(this.ButtonViewLog_Click);
            // 
            // labelMainContent
            // 
            this.labelMainContent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMainContent.Location = new System.Drawing.Point(114, 11);
            this.labelMainContent.Name = "labelMainContent";
            this.labelMainContent.Size = new System.Drawing.Size(433, 89);
            this.labelMainContent.TabIndex = 9;
            this.labelMainContent.Text = resources.GetString("labelMainContent.Text");
            this.labelMainContent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPrivacy
            // 
            this.labelPrivacy.Location = new System.Drawing.Point(12, 288);
            this.labelPrivacy.Name = "labelPrivacy";
            this.labelPrivacy.Size = new System.Drawing.Size(535, 33);
            this.labelPrivacy.TabIndex = 10;
            this.labelPrivacy.Text = "Privacy: you can see exactly what will be sent to us by pressing the View Log but" +
    "ton. We do not collect anything else.  By sending this report you accept our Pri" +
    "vacy Policy.";
            // 
            // linkPrivacyPolicy
            // 
            this.linkPrivacyPolicy.AutoSize = true;
            this.linkPrivacyPolicy.Location = new System.Drawing.Point(180, 327);
            this.linkPrivacyPolicy.Name = "linkPrivacyPolicy";
            this.linkPrivacyPolicy.Size = new System.Drawing.Size(78, 12);
            this.linkPrivacyPolicy.TabIndex = 7;
            this.linkPrivacyPolicy.TabStop = true;
            this.linkPrivacyPolicy.Text = "Privacy Policy";
            this.linkPrivacyPolicy.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkPrivacyPolicy_LinkClicked);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(94, 322);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Copy report";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // emailCheckbox
            // 
            this.emailCheckbox.Location = new System.Drawing.Point(11, 263);
            this.emailCheckbox.Name = "emailCheckbox";
            this.emailCheckbox.Size = new System.Drawing.Size(158, 20);
            this.emailCheckbox.TabIndex = 2;
            this.emailCheckbox.Text = "Remember my Email";
            this.emailCheckbox.UseVisualStyleBackColor = true;
            // 
            // textBoxDescription
            // 
            this.textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDescription.Location = new System.Drawing.Point(12, 126);
            this.textBoxDescription.Name = "textBoxDescription";
            this.textBoxDescription.Size = new System.Drawing.Size(535, 91);
            this.textBoxDescription.TabIndex = 0;
            this.textBoxDescription.Text = "";
            this.textBoxDescription.TextChanged += new System.EventHandler(this.TextBoxText_Changed);
            // 
            // CrashReportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AcceptButton = this.buttonSend;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(559, 532);
            this.CancelButton = this.buttonDontSend;
            this.Controls.Add(this.textBoxDescription);
            this.Controls.Add(this.emailCheckbox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.linkPrivacyPolicy);
            this.Controls.Add(this.labelPrivacy);
            this.Controls.Add(this.labelMainContent);
            this.Controls.Add(this.buttonViewLog);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.pictureBoxIcon);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.buttonDontSend);
            this.Controls.Add(this.buttonSend);
            this.Controls.Add(this.labelEmail);
            this.Controls.Add(this.textBoxEmail);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CrashReportForm";
            this.Text = "Report your crash";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.CrashReportForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxEmail;
        private System.Windows.Forms.Label labelEmail;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.Button buttonDontSend;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.PictureBox pictureBoxIcon;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.Button buttonViewLog;
        private System.Windows.Forms.Label labelMainContent;
        private System.Windows.Forms.Label labelPrivacy;
        private System.Windows.Forms.LinkLabel linkPrivacyPolicy;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox emailCheckbox;
        private System.Windows.Forms.RichTextBox textBoxDescription;
    }
}
