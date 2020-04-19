// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Stride.CrashReport;

namespace Stride.Editor.CrashReport
{
    public partial class CrashReportForm : Form
    {
        public const string PrivacyPolicyUrl = "https://stride3d.net/legal/privacy-policy";

        private readonly CrashReportData currentData;
        private int initialHeight;
        private bool expanded;

        private readonly ICrashEmailSetting settings;

        public CrashReportForm(CrashReportData crashReport, ICrashEmailSetting storeCrashEmailSetting)
        {
            settings = storeCrashEmailSetting;
            currentData = crashReport;
            InitializeComponent();
            textBoxLog.Text = crashReport.ToString();            
            if (settings == null)
            {
                emailCheckbox.Visible = false;
            }
            else
            {
                textBoxEmail.Text = settings == null ? "" : settings.StoreCrashEmail ? settings.Email : "";
                if (!string.IsNullOrEmpty(textBoxEmail.Text))
                {
                    emailCheckbox.Checked = true;
                }
            }
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
            currentData["UserEmail"] = textBoxEmail.Text ?? "";
            currentData["UserMessage"] = textBoxDescription.Text ?? "";
            textBoxLog.Text = currentData.ToString();
        }

        private void CrashReportForm_Load(object sender, EventArgs e)
        {
            initialHeight = ClientSize.Height;
            Expanded = false;
        }

        private void ButtonSend_Click(object sender, EventArgs e)
        {
            if (emailCheckbox.Checked)
            {
                settings.StoreCrashEmail = true;
                settings.Email = textBoxEmail.Text;
                settings.Save();
            }
            else
            {
                settings.StoreCrashEmail = false;
                settings.Email = "";
                settings.Save();
            }
            
            RefreshReport();
            MailReport(currentData);

            DialogResult = DialogResult.Yes;

            Close();
        }

        private void ButtonDontSend_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;

            Close();
        }

        private void ButtonViewLog_Click(object sender, EventArgs e)
        {
            Expanded = !Expanded;
        }

        private void LinkPrivacyPolicy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(PrivacyPolicyUrl);
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

        private void button1_Click(object sender, EventArgs e)
        {
            RefreshReport();
            Clipboard.SetText(currentData.ToString());
        }

        private static void MailReport(CrashReportData report)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    await CrashReporter.Report(report);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
            var result = task.Result;
            if (!result)
            {
                MessageBox.Show(@"An error occurred while sending the report. Unable to contact the server.", @"Stride", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
