// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Markdown.Avalonia;
using Stride.GameStudio.Avalonia.Services;

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

    private async void License_OnClick(object? sender, RoutedEventArgs e)
    {
        await MarkdownFileViewerService.ShowFileAsync("LICENSE.md", "License");
    }

    private async void ThirdParty_OnClick(object? sender, RoutedEventArgs e)
    {
        await MarkdownFileViewerService.ShowFileAsync("THIRD PARTY.md", "Third Party Licenses");
    }
}
