// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Stride.Editor.CrashReport;

public partial class CrashReportWindow : Window
{
    public const string PrivacyPolicyUrl = "https://stride3d.net/legal/privacy-policy";
    private const string GithubIssuesUrl = "https://github.com/stride3d/stride/issues/new?labels=bug&template=bug_report.md";
    private readonly CrashReportData currentData;
    public string ApplicationName { get; }

    public CrashReportWindow(CrashReportData crashReport, string applicationName)
    {
        InitializeComponent();
        currentData = crashReport;
        textBoxLog.Text = crashReport.ToString();
        ApplicationName = applicationName;
        DataContext = this;
    }

    private bool Expanded { get; set { field = value; RefreshSize(); } } = false;

    private void RefreshSize()
    {
        if (!Expanded)
        {
            buttonViewLog.Content = "View report";
            textBoxLog.Visibility = Visibility.Collapsed;
        }
        else
        {
            buttonViewLog.Content = "Hide report";
            textBoxLog.Visibility = Visibility.Visible;
        }
    }

    private void RefreshReport()
    {
        textBoxLog.Text = currentData.ToString();
    }

    private void ButtonOpenGithubIssues_Click(object sender, RoutedEventArgs e)
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

            MessageBox.Show(error, "Stride", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        DialogResult = true;
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

    private async void ButtonSaveReport_Click(object sender, RoutedEventArgs e)
    {
        RefreshReport();

        var fileDialog = new SaveFileDialog()
        {
            FileName = "Report.txt",
            DefaultExt = "txt",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (fileDialog.ShowDialog() == true)
        {
            await File.WriteAllTextAsync(fileDialog.FileName, currentData.ToString());
        }
    }
}
