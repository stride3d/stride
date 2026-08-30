// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
    private readonly Exception currentException;
    public string ApplicationName { get; }

    public CrashReportWindow(CrashReportData crashReport, string applicationName, Exception exception = null)
    {
        InitializeComponent();
        currentData = crashReport;
        currentException = exception;
        textBoxLog.Text = crashReport.ToString();
        ApplicationName = applicationName;
        DataContext = this;

        // Host theme may be absent (crash before the application initialized); fall back to readable colors.
        // Checkboxes and radio buttons set their foreground from the system theme, so inheritance is not enough.
        if (TryFindResource("BackgroundBrush") is null)
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x30));
            System.Windows.Documents.TextElement.SetForeground(this, System.Windows.Media.Brushes.White);
            checkBoxMinidump.Foreground = System.Windows.Media.Brushes.White;
            radioDevChannel.Foreground = System.Windows.Media.Brushes.White;
            radioCustomDsn.Foreground = System.Windows.Media.Brushes.White;
        }

        if (CrashReportSender.IsDisabled)
        {
            buttonSendReport.Visibility = Visibility.Collapsed;
            panelSend.Visibility = Visibility.Collapsed;
        }
        else if (string.IsNullOrEmpty(CrashReportSender.BuildDsn) && CrashReportSender.DevChannelDsn.Length == 0)
        {
            radioDevChannel.Visibility = Visibility.Collapsed;
            radioCustomDsn.IsChecked = true;
        }
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

    private async void ButtonSendReport_Click(object sender, RoutedEventArgs e)
    {
        // Official builds send straight to the baked-in DSN; source builds get a destination chooser.
        if (!string.IsNullOrEmpty(CrashReportSender.BuildDsn))
        {
            await SendAsync(CrashReportSender.BuildDsn);
        }
        else
        {
            panelSendOptions.Visibility = panelSendOptions.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private async void ButtonSendNow_Click(object sender, RoutedEventArgs e)
    {
        var dsn = radioCustomDsn.IsChecked == true ? textBoxCustomDsn.Text.Trim() : CrashReportSender.DevChannelDsn;
        if (string.IsNullOrEmpty(dsn))
        {
            MessageBox.Show(this, "Please enter a Sentry DSN.", "Stride", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await SendAsync(dsn);
    }

    private async Task SendAsync(string dsn)
    {
        RefreshReport();
        buttonSendReport.IsEnabled = false;
        panelSend.IsEnabled = false;
        panelSendOptions.IsEnabled = false;
        try
        {
            await CrashReportSender.SendAsync(currentData, ApplicationName, currentException, dsn, checkBoxMinidump.IsChecked == true,
                textBoxName.Text, textBoxEmail.Text, textBoxDescription.Text);
            MessageBox.Show(this, "Crash report sent. Thank you for helping improve Stride.", "Stride",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "The report could not be sent: " + exception.Message + Environment.NewLine
                + "You can still copy or save the report and open a Github issue.", "Stride",
                MessageBoxButton.OK, MessageBoxImage.Error);
            buttonSendReport.IsEnabled = true;
            panelSend.IsEnabled = true;
            panelSendOptions.IsEnabled = true;
        }
    }

    private void TextBoxCustomDsn_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        radioCustomDsn.IsChecked = true;
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
