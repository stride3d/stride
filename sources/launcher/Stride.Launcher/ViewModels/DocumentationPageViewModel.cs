// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stride.LauncherApp.Resources;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.LauncherApp.ViewModels
{
    internal class DocumentationPageViewModel : DispatcherViewModel
    {
        private static readonly Regex ParsingRegex = new Regex(@"\{([^\{\}]+)\}\{([^\{\}]+)\}\{([^\{\}]+)\}");
        private const string DocPageScheme = "page:";
        private const string PageUrlFormatString = "{0}{1}";

        public DocumentationPageViewModel(IViewModelServiceProvider serviceProvider, string version)
            : base(serviceProvider)
        {
            Version = version;
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
        /// Gets the root url of the documentation that should be opened when the user want to open Stride help.
        /// </summary>
        public string DocumentationRootUrl => GetDocumentationRootUrl(Version);

        /// <summary>
        /// Gets the version related to this documentation page.
        /// </summary>
        public string Version { get; }

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
        /// Gets a command that will open the documentation page in the default web browser.
        /// </summary>
        public ICommandBase OpenUrlCommand { get; private set; }

        public static async Task<List<DocumentationPageViewModel>> FetchGettingStartedPages(IViewModelServiceProvider serviceProvider, string version)
        {
            string urlData = null;
            var result = new List<DocumentationPageViewModel>();
            try
            {
                WebRequest request = WebRequest.Create(string.Format(Urls.GettingStarted, version));
                using (var reponse = await request.GetResponseAsync())
                {
                    using (var str = reponse.GetResponseStream())
                    {
                        if (str != null)
                        {
                            using (var reader = new StreamReader(str))
                            {
                                urlData = reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Unable to reach the url, return an empty list.
                return result;
            }

            if (urlData == null)
                return result;

            try
            {
                var urls = urlData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var url in urls)
                {
                    var match = ParsingRegex.Match(url);
                    if (match.Success && match.Groups.Count == 4)
                    {
                        var link = match.Groups[3].Value;
                        if (link.StartsWith(DocPageScheme))
                        {
                            link = GetDocumentationPageUrl(version, link.Substring(DocPageScheme.Length));
                        }
                        var page = new DocumentationPageViewModel(serviceProvider, version)
                        {
                            Title = match.Groups[1].Value.Trim(),
                            Description = match.Groups[2].Value.Trim(),
                            Url = link.Trim()
                        };
                        result.Add(page);
                    }
                }
            }
            catch (Exception)
            {
                result.Clear();
            }
            return result;
        }

        /// <summary>
        /// Compute the url of a documentation page, given the page name.
        /// </summary>
        /// <param name="version">The version related to this documentation page.</param>
        /// <param name="pageName">The name of the page.</param>
        /// <returns>The complete url of the documentation page.</returns>
        private static string GetDocumentationPageUrl(string version, string pageName)
        {
            return string.Format(PageUrlFormatString, GetDocumentationRootUrl(version), pageName);
        }

        private static string GetDocumentationRootUrl(string version)
        {
            return string.Format(Urls.Documentation, version);
        }
    }
}
