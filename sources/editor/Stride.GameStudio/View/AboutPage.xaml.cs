// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Extensions;

namespace Stride.GameStudio.View
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage
    {
        private const string MarkdownNotLoaded = "Unable to load the file.";

        public static readonly DependencyProperty MarkdownBackersProperty =
            DependencyProperty.Register("MarkdownBackers", typeof(string), typeof(AboutPage), new PropertyMetadata(null));

        public AboutPage()
        {
            InitializeComponent();

            DataContext = this;
            LoadBackers().Forget();
        }

        public AboutPage(IEditorDialogService service)
            : this()
        {
            Service = service;
        }

        public string MarkdownBackers
        {
            get { return (string)GetValue(MarkdownBackersProperty); }
            set { SetValue(MarkdownBackersProperty, value); }
        }

        private IEditorDialogService Service { get; }

        private void ButtonCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void License_OnClick(object sender, RoutedEventArgs e)
        {
            var message = await LoadMarkdown("LICENSE.md");
            Service.MessageBox(message).Forget();
        }

        private async void ThirdParty_OnClick(object sender, RoutedEventArgs e)
        {
            var message = await LoadMarkdown("THIRD PARTY.md");
            Service.MessageBox(message).Forget();
        }
        
        private async static Task<string> LoadMarkdown(string file)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), file);
                    if (!File.Exists(filePath))
                        filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\content", file);
                    string fileMarkdown;
                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        using (var reader = new StreamReader(fileStream))
                        {
                            fileMarkdown = reader.ReadToEnd();
                        }
                    }

                    return fileMarkdown;
                });
            }
            catch (Exception)
            {
                return MarkdownNotLoaded;
            }
        }

        private async Task LoadBackers()
        {
            MarkdownBackers = await LoadMarkdown("BACKERS.md").ConfigureAwait(true);
        }
    }
}
