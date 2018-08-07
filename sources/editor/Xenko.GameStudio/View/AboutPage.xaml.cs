// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Extensions;

namespace Xenko.GameStudio.View
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
            LoadBakers().Forget();
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

        private void License_OnClick(object sender, RoutedEventArgs e)
        {
            Service.MessageBox(LoadMarkdown("LICENSE.md"));
        }

        private void ThirdParty_OnClick(object sender, RoutedEventArgs e)
        {
            Service.MessageBox(LoadMarkdown("THIRD PARTY.md"));
        }
        
        private static string LoadMarkdown(string file)
        {
            try
            {
                var filePath = Path.Combine(PackageStore.Instance.DefaultPackage.RootDirectory, file);
                string fileMarkdown;
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        fileMarkdown = reader.ReadToEnd();
                    }
                }

                return fileMarkdown;
            }
            catch (Exception)
            {
                return MarkdownNotLoaded;
            }
        }

        private async Task LoadBakers()
        {
            MarkdownBackers = await Task.Run(() => LoadMarkdown("BACKERS.md")).ConfigureAwait(true);
        }
    }
}
