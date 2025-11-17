// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
            this.buttonGithubOpen = new System.Windows.Forms.Button();
            this.buttonSaveReportFile = new System.Windows.Forms.Button();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
            this.buttonViewLog = new System.Windows.Forms.Button();
            this.labelMainContent = new System.Windows.Forms.Label();
            this.buttonCopyReport = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonGithubOpen
            // 
            this.buttonGithubOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonGithubOpen.Location = new System.Drawing.Point(400, 100);
            this.buttonGithubOpen.Name = "buttonGithubOpen";
            this.buttonGithubOpen.Size = new System.Drawing.Size(140, 23);
            this.buttonGithubOpen.TabIndex = 3;
            this.buttonGithubOpen.Text = "Open Github Issues";
            this.buttonGithubOpen.UseVisualStyleBackColor = true;
            this.buttonGithubOpen.Click += new System.EventHandler(this.ButtonOpenGithubIssues_Click);
            // 
            // saveReportFileBtn
            // 
            this.buttonSaveReportFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSaveReportFile.Location = new System.Drawing.Point(118, 100);
            this.buttonSaveReportFile.Name = "buttonSaveReportFile";
            this.buttonSaveReportFile.Size = new System.Drawing.Size(100, 23);
            this.buttonSaveReportFile.TabIndex = 4;
            this.buttonSaveReportFile.Text = "Save report";
            this.buttonSaveReportFile.UseVisualStyleBackColor = true;
            this.buttonSaveReportFile.Click += new System.EventHandler(this.ButtonSaveReport_Click);
            // 
            // textBoxLog
            // 
            this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLog.Location = new System.Drawing.Point(12, 186);
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
            // buttonViewLog
            // 
            this.buttonViewLog.Location = new System.Drawing.Point(13, 140);
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
            // buttonCopyReport
            // 
            this.buttonCopyReport.Location = new System.Drawing.Point(12, 100);
            this.buttonCopyReport.Name = "buttonCopyReport";
            this.buttonCopyReport.Size = new System.Drawing.Size(100, 23);
            this.buttonCopyReport.TabIndex = 6;
            this.buttonCopyReport.Text = "Copy report";
            this.buttonCopyReport.UseVisualStyleBackColor = true;
            this.buttonCopyReport.Click += new System.EventHandler(this.ButtonCopyReport_Click);
            // 
            // CrashReportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(559, 360);
            this.Controls.Add(this.buttonCopyReport);
            this.Controls.Add(this.labelMainContent);
            this.Controls.Add(this.buttonViewLog);
            this.Controls.Add(this.pictureBoxIcon);
            this.Controls.Add(this.textBoxLog);        
            this.Controls.Add(this.buttonSaveReportFile);
            this.Controls.Add(this.buttonGithubOpen);
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

        private System.Windows.Forms.Button buttonGithubOpen;
        private System.Windows.Forms.Button buttonSaveReportFile;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.PictureBox pictureBoxIcon;
        private System.Windows.Forms.Button buttonViewLog;
        private System.Windows.Forms.Label labelMainContent;
        private System.Windows.Forms.Button buttonCopyReport;
    }
}
