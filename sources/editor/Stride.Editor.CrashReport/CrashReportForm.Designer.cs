// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Editor.CrashReport;
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
        this.openGithubBtn = new System.Windows.Forms.Button();
        this.saveReportFileBtn = new System.Windows.Forms.Button();
        this.textBoxLog = new System.Windows.Forms.TextBox();
        this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
        this.buttonViewLog = new System.Windows.Forms.Button();
        this.labelMainContent = new System.Windows.Forms.Label();
        this.copyReportBtn = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
        this.SuspendLayout();
        // 
        // openGithubBtn
        // 
        this.openGithubBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.openGithubBtn.Location = new System.Drawing.Point(440, 100);
        this.openGithubBtn.Name = "openGithubBtn";
        this.openGithubBtn.Size = new System.Drawing.Size(100, 37);
        this.openGithubBtn.TabIndex = 3;
        this.openGithubBtn.Text = "Open Github Issues";
        this.openGithubBtn.UseVisualStyleBackColor = true;
        this.openGithubBtn.Click += new System.EventHandler(this.OpenGithub_Click);
        // 
        // saveReportFileBtn
        // 
        this.saveReportFileBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.saveReportFileBtn.Location = new System.Drawing.Point(118, 100);
        this.saveReportFileBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.saveReportFileBtn.Name = "saveReportFileBtn";
        this.saveReportFileBtn.Size = new System.Drawing.Size(100, 23);
        this.saveReportFileBtn.TabIndex = 4;
        this.saveReportFileBtn.Text = "Save report";
        this.saveReportFileBtn.UseVisualStyleBackColor = true;
        this.saveReportFileBtn.Click += new System.EventHandler(this.SaveReport_Click);
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
        this.buttonViewLog.Location = new System.Drawing.Point(13, 150);
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
        // button1
        // 
        this.copyReportBtn.Location = new System.Drawing.Point(12, 100);
        this.copyReportBtn.Name = "copyReportBtn";
        this.copyReportBtn.Size = new System.Drawing.Size(100, 23);
        this.copyReportBtn.TabIndex = 6;
        this.copyReportBtn.Text = "Copy report";
        this.copyReportBtn.UseVisualStyleBackColor = true;
        this.copyReportBtn.Click += new System.EventHandler(this.copyReportBtn_Click);
        // 
        // CrashReportForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
        this.AcceptButton = this.openGithubBtn;
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(559, 360);
        this.CancelButton = this.saveReportFileBtn;
        this.Controls.Add(this.copyReportBtn);
        this.Controls.Add(this.labelMainContent);
        this.Controls.Add(this.buttonViewLog);
        this.Controls.Add(this.pictureBoxIcon);
        this.Controls.Add(this.textBoxLog);
        this.Controls.Add(this.saveReportFileBtn);
        this.Controls.Add(this.openGithubBtn);
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

    private System.Windows.Forms.Button openGithubBtn;
    private System.Windows.Forms.Button saveReportFileBtn;
    private System.Windows.Forms.TextBox textBoxLog;
    private System.Windows.Forms.PictureBox pictureBoxIcon;
    private System.Windows.Forms.Button buttonViewLog;
    private System.Windows.Forms.Label labelMainContent;
    private System.Windows.Forms.Button copyReportBtn;
}

