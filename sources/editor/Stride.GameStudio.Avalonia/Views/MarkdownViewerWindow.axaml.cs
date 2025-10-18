// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Markdown.Avalonia;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MarkdownViewerWindow : Window
{
    private MarkdownScrollViewer? _markdownViewer;

    public MarkdownViewerWindow(string markdownText, string title = "Markdown Viewer")
    {
        InitializeComponent();

        Title = title;

        _markdownViewer = this.FindControl<MarkdownScrollViewer>("MarkdownViewer");

        if (_markdownViewer is not null)
        {
            _markdownViewer.Markdown = markdownText;
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


