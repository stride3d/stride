// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Styling;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Services;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// This class represents the release notes of a given version.
/// </summary>
public sealed partial class ReleaseNotesViewModel : DispatcherViewModel
{
    private readonly MainViewModel launcher;
    private bool isActive;
    // The release notes as downloaded, before ApplyTheme keeps the images of the current theme
    private string? loadedMarkdown;
    private string? markdownContent;
    private bool isLoading = true;
    private bool isLoaded;
    private bool isUnavailable;

    private const string RootUrl = "https://doc.stride3d.net";
    private const string ReleaseNotesFileName = "ReleaseNotes.md";
    private readonly string baseUrl;
    private readonly string releaseNotesFileName;

    internal ReleaseNotesViewModel(MainViewModel launcher, string version)
        : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(version);

        this.launcher = launcher;
        Version = version;
        // The release notes of every version (betas included) are in one place
        baseUrl = $"{RootUrl}/en/release-notes/";
        releaseNotesFileName = $"{Version}.md";
#if DEBUG
        if (Environment.CommandLine.ToLowerInvariant().Contains("/previewreleasenotes"))
        {
            var launcherPath = AppDomain.CurrentDomain.BaseDirectory;
            var mdPath = Path.Combine(launcherPath, @"..\..\..\..\..\doc\");
            if (File.Exists($"{mdPath}{ReleaseNotesFileName}"))
            {
                baseUrl = $"file:///{mdPath.Replace("\\", "/")}";
                releaseNotesFileName = ReleaseNotesFileName;
            }
        }
#endif

        ToggleCommand = new AnonymousCommand(ServiceProvider, Toggle);
    }

    public string BaseUrl { get { return baseUrl; } }

    public string Version { get; }

    public string? MarkdownContent { get { return markdownContent; } private set { SetValue(ref markdownContent, value); } }

    public bool IsActive { get { return isActive; } private set { SetValue(ref isActive, value); } }

    public bool IsLoading { get { return isLoading; } set { SetValue(ref isLoading, value); } }

    public bool IsLoaded { get { return isLoaded; } set { SetValue(ref isLoaded, value); } }

    public bool IsUnavailable { get { return isUnavailable; } set { SetValue(ref isUnavailable, value); } }

    public ICommandBase ToggleCommand { get; private set; }

    public async void FetchReleaseNotes()
    {
        string releaseNotesMarkdown;

        try
        {
            var url = new Uri($"{BaseUrl}{releaseNotesFileName}");
            // A local file in Debug (/previewreleasenotes): HttpClient doesn't read file: URLs
            if (url.IsFile)
            {
                releaseNotesMarkdown = await File.ReadAllTextAsync(url.LocalPath);
            }
            else
            {
                using var response = await LauncherHttpClient.Instance.GetAsync(url);
                response.EnsureSuccessStatusCode();
                releaseNotesMarkdown = await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception)
        {
            IsLoading = false;
            IsUnavailable = true;
            return;
        }

        if (releaseNotesMarkdown is not null)
        {
            // parse video tag
            var videoRegex = GetVideoRegex();
            loadedMarkdown = videoRegex.Replace(releaseNotesMarkdown, "![]($2)\r\n\r\n[_Click to watch the video_]($4)");
            ApplyTheme();
            IsLoading = false;
            IsLoaded = true;
        }
        else
        {
            IsLoading = false;
            IsUnavailable = true;
        }
    }

    /// <summary>
    /// Shows the release notes with the images of the current theme only (<see cref="ThemeImages"/>).
    /// </summary>
    internal void ApplyTheme()
    {
        if (loadedMarkdown is not null)
            MarkdownContent = ThemeImages.Keep(loadedMarkdown, Application.Current?.ActualThemeVariant != ThemeVariant.Light);
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

    [GeneratedRegex(@"
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
                    </video>", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex GetVideoRegex();
}
