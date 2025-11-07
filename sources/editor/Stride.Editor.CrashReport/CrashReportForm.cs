// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;

namespace Stride.Editor.CrashReport
{
    public partial class CrashReportForm : Form
    {
        public const string PrivacyPolicyUrl = "https://stride3d.net/legal/privacy-policy";
        private const string GithubIssuesUrl = "https://github.com/stride3d/stride/issues/new?labels=bug&template=bug_report.md";

        private readonly CrashReportData currentData;
        private int initialHeight;

        public CrashReportForm(CrashReportData crashReport)
        {
            currentData = crashReport;
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            textBoxLog.Text = crashReport.ToString();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Expanded { get; set { field = value; RefreshSize(); } }

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

        private void ButtonOpenGithubIssues_Click(object sender, EventArgs e)
        {
            try
            {
                Process browser = new();
                browser.StartInfo.FileName = GithubIssuesUrl;
                browser.StartInfo.UseShellExecute = true;
                browser.Start();
            }
            catch (Exception)
            {
                var error = "An error occurred while opening the browser. You can access Github Issues at the following url:"
                            + Environment.NewLine + Environment.NewLine + GithubIssuesUrl;

                MessageBox.Show(error, @"Stride", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            DialogResult = DialogResult.Yes;
        }

        private void ButtonViewLog_Click(object sender, EventArgs e)
        {
            Expanded = !Expanded;
        }

        private void ButtonCopyReport_Click(object sender, EventArgs e)
        {
            RefreshReport();
            Clipboard.SetText(currentData.ToString());
        }

        private async void ButtonSaveReport_Click(object sender, EventArgs e)
        {
            RefreshReport();

            var fileDialog = new SaveFileDialog()
            {
                FileName = "Report.txt",
                DefaultExt = "txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            var result = fileDialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                await File.WriteAllTextAsync(fileDialog.FileName, currentData.ToString());
            }
        }
    }
}
