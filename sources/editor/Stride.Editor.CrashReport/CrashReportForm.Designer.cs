// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Modern.Forms;
using SkiaSharp;

namespace Stride.Editor.CrashReport;

partial class CrashReportForm
{
    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
	    var resources = new System.ComponentModel.ComponentResourceManager(typeof(CrashReportForm));
        var pictureimage = System.Convert.FromBase64String(resources.GetObject("pictureBoxIcon.Image").ToString());
        var icon = System.Convert.FromBase64String(resources.GetObject("$this.Icon").ToString());
        // 
        // openGithubBtn
        // 
        this.openGithubBtn.Anchor = ((Modern.Forms.AnchorStyles)((Modern.Forms.AnchorStyles.Top | Modern.Forms.AnchorStyles.Right)));
        this.openGithubBtn.Location = new System.Drawing.Point(447,  175);
        this.openGithubBtn.Name = "openGithubBtn";
        this.openGithubBtn.Size = new System.Drawing.Size(100, 37);
        this.openGithubBtn.TabIndex = 3;
        this.openGithubBtn.Text = "Open Github Issues";
        this.openGithubBtn.Click += new(this.OpenGithub_Click);
        // 
        // saveReportFileBtn
        // 
        this.saveReportFileBtn.Anchor = ((Modern.Forms.AnchorStyles)((Modern.Forms.AnchorStyles.Top | Modern.Forms.AnchorStyles.Right)));
        this.saveReportFileBtn.Location = new System.Drawing.Point(225, 180);
        this.saveReportFileBtn.Name = "saveReportFileBtn";
        this.saveReportFileBtn.Size = new System.Drawing.Size(100, 23);
        this.saveReportFileBtn.TabIndex = 3;
        this.saveReportFileBtn.Text = "Save report";
        this.saveReportFileBtn.Click += new(this.SaveReport_Click);
        // 
        // textBoxLog
        // 
        this.textBoxLog.Anchor = ((Modern.Forms.AnchorStyles)((((Modern.Forms.AnchorStyles.Top | Modern.Forms.AnchorStyles.Bottom) 
        | Modern.Forms.AnchorStyles.Left) 
        | Modern.Forms.AnchorStyles.Right)));
        this.textBoxLog.Location = new System.Drawing.Point(12, 230);
        this.textBoxLog.MultiLine = true;
        this.textBoxLog.Name = "textBoxLog";
        this.textBoxLog.ReadOnly = true;
        this.textBoxLog.ScrollBars = Modern.Forms.ScrollBars.Vertical;
        this.textBoxLog.Size = new System.Drawing.Size(535, 290);
        this.textBoxLog.TabIndex = 5;
        // 
        // pictureBoxIcon
        // 
        this.pictureBoxIcon.Image = SKBitmap.Decode(pictureimage);
        this.pictureBoxIcon.Location = new System.Drawing.Point(12, 45);
        this.pictureBoxIcon.Name = "pictureBoxIcon";
        this.pictureBoxIcon.Size = new System.Drawing.Size(96, 89);
        this.pictureBoxIcon.TabIndex = 6;
        this.pictureBoxIcon.TabStop = false;
        // 
        // buttonViewLog
        // 
        this.buttonViewLog.Location = new System.Drawing.Point(13, 180);
        this.buttonViewLog.Name = "buttonViewLog";
        this.buttonViewLog.Size = new System.Drawing.Size(100, 23);
        this.buttonViewLog.TabIndex = 5;
        this.buttonViewLog.Text = "View report";
        this.buttonViewLog.Click += new(this.ButtonViewLog_Click);
        // 
        // labelMainContent
        // 
        this.labelMainContent.Anchor = ((Modern.Forms.AnchorStyles)(((Modern.Forms.AnchorStyles.Top | Modern.Forms.AnchorStyles.Left) 
        | Modern.Forms.AnchorStyles.Right)));
        this.labelMainContent.Location = new System.Drawing.Point(114, 30);
        this.labelMainContent.Name = "labelMainContent";
        this.labelMainContent.Size = new System.Drawing.Size(433, 125);
        this.labelMainContent.TabIndex = 9;
        this.labelMainContent.Multiline=true;
        this.labelMainContent.Text = resources.GetString("labelMainContent.Text");
        this.labelMainContent.TextAlign = Modern.Forms.ContentAlignment.MiddleLeft;
        // 
        // copyReportBtn
        // 
        this.copyReportBtn.Location = new System.Drawing.Point(119, 180);
        this.copyReportBtn.Name = "copyReportBtn";
        this.copyReportBtn.Size = new System.Drawing.Size(100, 23);
        this.copyReportBtn.TabIndex = 6;
        this.copyReportBtn.Text = "Copy report";
        this.copyReportBtn.Click += new(this.copyReportBtn_Click);
        // 
        // CrashReportForm
        // 
        this.AllowMaximize=false;
        this.Resizeable=false;
        this.Size = new System.Drawing.Size(559, 532);
        this.Controls.Add(this.copyReportBtn);
        this.Controls.Add(this.labelMainContent);
        this.Controls.Add(this.buttonViewLog);
        this.Controls.Add(this.pictureBoxIcon);
        this.Controls.Add(this.textBoxLog);
        this.Controls.Add(this.openGithubBtn);
        this.Controls.Add(this.saveReportFileBtn);
        this.Image = SKBitmap.Decode(icon).Resize(new SKSizeI(24,24),SKFilterQuality.High);
        this.TitleBar.Text = "CrashReportForm";
        this.Text = "Report your crash";
        this.Shown += this.CrashReportForm_Load;
    }

    #endregion

    private Modern.Forms.Button openGithubBtn = new();
    private Modern.Forms.TextBox textBoxLog = new();
    private Modern.Forms.PictureBox pictureBoxIcon = new();
    private Modern.Forms.Button buttonViewLog = new();
    private Modern.Forms.Button saveReportFileBtn = new();
    private Modern.Forms.Label labelMainContent = new();
    private Modern.Forms.Button copyReportBtn = new();
}
