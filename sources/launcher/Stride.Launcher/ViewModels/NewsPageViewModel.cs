// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Stride.LauncherApp.Resources;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.LauncherApp.ViewModels
{
    internal class NewsPageViewModel : DispatcherViewModel
    {
        public NewsPageViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            OpenUrlCommand = new AnonymousTaskCommand(ServiceProvider, OpenUrl);
        }

        private async Task OpenUrl()
        {
            try
            {
                Process.Start(Url);
            }
            catch (Exception)
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Strings.ErrorOpeningBrowser, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets or sets the title of this documentation page.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of this documentation page.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the url of this documentation page.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the url of this documentation page.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets a command that will open the documentation page in the default web browser.
        /// </summary>
        public ICommandBase OpenUrlCommand { get; private set; }

        public static async Task<List<NewsPageViewModel>> FetchNewsPages(IViewModelServiceProvider serviceProvider, int maxCount)
        {
            var result = new List<NewsPageViewModel>();
            var rss = new MemoryStream();
            try
            {
                WebRequest request = WebRequest.Create(Urls.RssFeed);
                using (var reponse = await request.GetResponseAsync())
                {
                    using (var str = reponse.GetResponseStream())
                    {
                        str?.CopyTo(rss);
                    }
                }
            }
            catch (Exception)
            {
                // Unable to reach the url, return an empty list.
                return result;
            }

            rss.Position = 0;
            if (rss.Length == 0)
                return result;

            try
            {
                int count = 0;
                using (XmlReader rssReader = XmlReader.Create(rss))
                {
                    rssReader.MoveToContent();
                    while (rssReader.ReadToFollowing("item") && count < maxCount)
                    {
                        rssReader.ReadToFollowing("title");
                        string title = rssReader.Read() ? rssReader.Value : null;
                        rssReader.ReadToFollowing("description");
                        string description = rssReader.Read() ? rssReader.Value : null;
                        rssReader.ReadToFollowing("pubDate");
                        var date = new DateTime();
                        bool dateValid = rssReader.Read() && DateTime.TryParseExact(rssReader.Value, "ddd, dd MMM yyyy HH:mm:ss zz00", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                        rssReader.ReadToFollowing("link");
                        string link = rssReader.Read() ? rssReader.Value : null;
                        if (dateValid && title != null && link != null && description != null)
                        {
                            var page = new NewsPageViewModel(serviceProvider)
                            {
                                Title = title,
                                Url = link,
                                Description = description,
                                Date = date
                            };
                            result.Add(page);
                            ++count;
                        }
                    }
                }
            }
            catch (Exception)
            {
                result.Clear();
            }

            return result;
        }
    }
}
