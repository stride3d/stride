// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using MarkView.Avalonia;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MarkdownViewerWindow : Window
{
    public MarkdownViewerWindow(string markdownText, string title = "Markdown Viewer")
    {
        InitializeComponent();

        Title = title;

        if (MarkdownViewer is not null)
        {
            MarkdownViewer.Markdown = markdownText;
        }
        else
        {
            Content = new TextBlock
            {
                Text = "Error: Markdown viewer could not be loaded.",
                Margin = new Thickness(20)
            };
        }
    }
}


