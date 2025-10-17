// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using System.Drawing;
using System;
using System.IO;
using System.Windows.Forms;

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

    public bool Expanded { get { return expanded; } set { expanded = value; RefreshSize(); } }

    private void RefreshSize()
    {
        if (!Expanded)
        {
            ClientSize = new Size(ClientSize.Width, textBoxLog.Top);
            buttonViewLog.Text = @"View report";
        }
        else
        {
            ClientSize = new Size(ClientSize.Width, initialHeight);
            buttonViewLog.Text = @"Hide report";
        }
    }

    private void RefreshReport()
    {
        textBoxLog.Text = currentData.ToString();
    }

    private void CrashReportForm_Load(object sender, EventArgs e)
    {
        initialHeight = ClientSize.Height;
        Expanded = false;
    }

    private void OpenGithub_Click(object sender, EventArgs e)
    {
        RefreshReport();
        try
        {
            CrashReporter.OpenGithub();
        }
        catch
        {
            MessageBox.Show("Failed to open browser","Error");
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

    private void copyReportBtn_Click(object sender, EventArgs e)
    {
        RefreshReport();
        Clipboard.SetText(currentData.ToString());
    }
    private async void SaveReport_Click(object sender, EventArgs e)
    {
        RefreshReport();

        var fileDialog = new SaveFileDialog();
        var result = fileDialog.ShowDialog(this);
        if (result == DialogResult.OK && fileDialog.FileName != null)
        {
            await File.WriteAllTextAsync(fileDialog.FileName, currentData.ToString());
        }
    }
}