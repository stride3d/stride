// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Drawing;
using System;
using System.IO;
using Modern.Forms;

namespace Stride.Editor.CrashReport;

public partial class CrashReportForm : Form
{
    public const string PrivacyPolicyUrl = "https://stride3d.net/legal/privacy-policy";

    private readonly CrashReportData currentData;
    private int initialHeight;
    private bool expanded;

    public CrashReportForm(CrashReportData crashReport)
    {
        currentData = crashReport;
        InitializeComponent();
        textBoxLog.Text = crashReport.ToString();
    }
    public void Run()
    {
        Application.Run(this);
    }

    public bool Expanded { get { return expanded; } set { expanded = value; RefreshSize(); } }

    private void RefreshSize()
    {
        if (!Expanded)
        {
            Size = new Size(Size.Width, textBoxLog.Top);
            buttonViewLog.Text = @"View report";
        }
        else
        {
            Size = new Size(Size.Width, initialHeight);
            buttonViewLog.Text = @"Hide report";
        }
    }

    private void RefreshReport()
    {
        textBoxLog.Text = currentData.ToString();
    }

    private void CrashReportForm_Load(object sender, EventArgs e)
    {
        initialHeight = Size.Height;
        Expanded = false;
    }

    private void OpenGithub_Click(object sender, EventArgs e)
    {
        RefreshReport();
        try{
            CrashReporter.OpenGithub();
        }
        catch{
            var mb = new MessageBoxForm("Error", "Failed to open browser");
            mb.Show();
        }
    }

    private void ButtonViewLog_Click(object sender, EventArgs e)
    {
        Expanded = !Expanded;
    }

    private void LinkPrivacyPolicy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            //Open URL in user's default browser when clicked
            Process process = new Process();
            process.StartInfo.FileName = PrivacyPolicyUrl;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }
        // FIXME: catch only specific exceptions?
        catch (Exception)
        {
            var error = "An error occurred while opening the browser. You can access the privacy policy at the following url:"
                + Environment.NewLine + Environment.NewLine + PrivacyPolicyUrl;

            MessageBox.Show(error, @"Stride", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TextBoxText_Changed(object sender, EventArgs e)
    {
        RefreshReport();
    }

    private async void copyReportBtn_Click(object sender, EventArgs e)
    {
        RefreshReport();
        await Clipboard.SetTextAsync(currentData.ToString());
    }
    private async void SaveReport_Click(object sender, EventArgs e)
    {
        RefreshReport();

        var fileDialog = new SaveFileDialog();
        var result = await fileDialog.ShowDialog(this);
        if (result == DialogResult.OK && fileDialog.FileName != null)
        {
            await File.WriteAllTextAsync(fileDialog.FileName, currentData.ToString());
        }
    }
}
