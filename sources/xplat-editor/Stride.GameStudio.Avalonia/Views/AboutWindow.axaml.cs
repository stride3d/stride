// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Markdown.Avalonia;

namespace Stride.GameStudio.Avalonia.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await Task.Delay(2000);

        if (this.FindControl<MarkdownScrollViewer>("BackersView") is { } view)
        {
            Vector previousValue;
            do
            {
                previousValue = view.ScrollValue;
                view.ScrollValue += new Vector(0, 2);
                await Task.Delay(25);
            } while (view.ScrollValue.Y > previousValue.Y);
        }
    }

    // TODO xplat-editor make it an utility
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

    private void License_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenLink("LICENSE.md");
    }

    private void Privacy_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenLink("https://stride3d.net/legal/privacy-policy");
    }

    private void ThirdParty_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenLink("THIRD PARTY.md");
    }
}
