// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.LauncherApp.ViewModels
{
    /// <summary>
    /// This class represents the release notes of a given version.
    /// </summary>
    internal class ReleaseNotesViewModel : DispatcherViewModel
    {
        private readonly LauncherViewModel launcher;
        private bool isActive;
        private string markdownContent;
        private bool isLoading = true;
        private bool isLoaded;
        private bool isUnavailable;

        private const string RootUrl = "https://doc.stride3d.net";
        private const string ReleaseNotesFileName = "ReleaseNotes.md";
        private string baseUrl;

        internal ReleaseNotesViewModel([NotNull] LauncherViewModel launcher, [NotNull] string version)
            : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            this.launcher = launcher;

            Version = version;
            baseUrl = $"{RootUrl}/{Version}/ReleaseNotes/";
#if DEBUG
            if (Environment.CommandLine.ToLowerInvariant().Contains("/previewreleasenotes"))
            {
                var launcherPath = AppDomain.CurrentDomain.BaseDirectory;
                var mdPath = Path.Combine(launcherPath, @"..\..\..\..\..\doc\");
                if (File.Exists($"{mdPath}{ReleaseNotesFileName}"))
                {
                    baseUrl = $"file:///{mdPath.Replace("\\", "/")}";
                }
            }
#endif

            ToggleCommand = new AnonymousCommand(ServiceProvider, Toggle);
        }

        public string BaseUrl { get { return baseUrl; } private set { SetValue(ref baseUrl, value); } }

        public string Version { get; }

        public string MarkdownContent { get { return markdownContent; } private set { SetValue(ref markdownContent, value); } }

        public bool IsActive { get { return isActive; } private set { SetValue(ref isActive, value); } }

        public bool IsLoading { get { return isLoading; } set { SetValue(ref isLoading, value); } }

        public bool IsLoaded { get { return isLoaded; } set { SetValue(ref isLoaded, value); } }

        public bool IsUnavailable { get { return isUnavailable; } set { SetValue(ref isUnavailable, value); } }

        public ICommandBase ToggleCommand { get; private set; }

        public async void FetchReleaseNotes()
        {
            string releaseNotesMarkdown = null;

            try
            {
                var request = WebRequest.Create($"{BaseUrl}{ReleaseNotesFileName}");
                using (var response = await request.GetResponseAsync())
                {
                    using (var str = response.GetResponseStream())
                    {

                        if (str != null)
                        {
                            using (var reader = new StreamReader(str))
                            {
                                releaseNotesMarkdown = reader.ReadToEnd();
                            }
                        }
                    }
                    // fetch the response Uri and update the base URL
                    var responseUri = response.ResponseUri.AbsoluteUri;
                    BaseUrl = responseUri.Remove(responseUri.Length - ReleaseNotesFileName.Length);
                }
            }
            catch (Exception)
            {
                IsLoading = false;
                IsUnavailable = true;
                return;
            }

            if (releaseNotesMarkdown != null)
            {
                // parse video tag
                var videoRegex = new Regex(@"
                    <video
                        [^>]*?                 # any valid HTML characters
                        poster                 # poster attribute
                        \s*=\s*
                        (['""])                # quote char = $1
                        ([^'"" >]+?)           # url of poster image
                        \1                     # matching quote
                        [^>]*?>\s*
                            <source
                                [^>]*?         # any valid HTML characters
                                src            # src attribute
                                \s*=\s*
                                (['""])        # quote char = $3
                                ([^'"" >] +?)  # url of video
                                \3             # matching quote
                                [^>] *?>\s*
                    </video>",
                    RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
                MarkdownContent = videoRegex.Replace(releaseNotesMarkdown, "![]($2)\r\n\r\n[_Click to watch the video_]($4)");
                IsLoading = false;
                IsLoaded = true;
            }
            else
            {
                IsLoading = false;
                IsUnavailable = true;
            }
        }

        public void Show()
        {
            IsActive = true;
            launcher.ActiveReleaseNotes = this;
        }

        private void Toggle()
        {
            IsActive = launcher.ActiveReleaseNotes != this || !IsActive;
            launcher.ActiveReleaseNotes = this;
        }
    }
}
