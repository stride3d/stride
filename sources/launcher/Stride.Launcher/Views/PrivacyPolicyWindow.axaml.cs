// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Launcher.Crash;
using Stride.Launcher.Services;

namespace Stride.Launcher.Views;

public partial class PrivacyPolicyWindow : Window
{
    public PrivacyPolicyWindow(bool canAccept)
    {
        CanAccept = canAccept;
        InitializeComponent();
    }

    /// <summary>
    /// Identifies the <see cref="PrivacyPolicyAccepted"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> PrivacyPolicyAcceptedProperty =
        AvaloniaProperty.Register<PrivacyPolicyWindow, bool>(nameof(PrivacyPolicyAccepted));


    /// <summary>
    /// Gets whether the Privacy Policy can be accepted.
    /// </summary>
    public bool CanAccept { get; }

    /// <summary>
    /// Gets or sets whether the Privacy Policy has been accepted.
    /// </summary>
    public bool PrivacyPolicyAccepted
    {
        get => GetValue(PrivacyPolicyAcceptedProperty);
        set => SetValue(PrivacyPolicyAcceptedProperty, value);
    }

    private static void OpenLink(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        // FIXME: catch only specific exceptions?
        catch (Exception)
        {
        }
    }

    private void ButtonPrivacyPolicyAccepted(object sender, RoutedEventArgs e)
    {
        if (PrivacyPolicyAccepted)
            PrivacyPolicyHelper.AcceptStride40();

        Close();
    }

    private void ButtonPrivacyPolicyDeclined(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Privacy_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenLink(CrashReportViewModel.PrivacyPolicyUrl);
    }
}
