using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Services
{
    public static class MarkdownFileViewerService
    {
        public static void ShowFile(string filePath, string title = "Markdown Viewer")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ShowError($"We are facing some technical challenges to fetch the content: {filePath}", title);
                    return;
                }

                string markdownContent = File.ReadAllText(filePath);
                var window = new MarkdownViewerWindow(markdownContent, title);
                window.Show();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to open markdown file:\n{ex.Message}", title);
            }
        }

        private static void ShowError(string message, string title)
        {
            var errorWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                Content = new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(20)
                },
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            errorWindow.Show();
        }
    }
}
