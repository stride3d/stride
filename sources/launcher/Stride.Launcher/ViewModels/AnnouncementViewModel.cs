// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Launcher.ViewModels;

public sealed class AnnouncementViewModel : DispatcherViewModel
{
    private readonly string announcementName;
    private bool dontShowAgain;
    private bool validated = true;

    public AnnouncementViewModel(IViewModelServiceProvider serviceProvider, string announcementName)
        : base(serviceProvider)
    {
        this.announcementName = announcementName;
        if (!MainViewModel.HasDoneTask(TaskName))
        {
            MarkdownAnnouncement = Initialize(announcementName);
        }

        CloseAnnouncementCommand = new AnonymousCommand(ServiceProvider, CloseAnnouncement);
        // We want to explicitely trigger the property change notification for the view storyboard
        Validated = false;
    }

    public bool DontShowAgain { get { return dontShowAgain; } set { SetValue(ref dontShowAgain, value); } }

    public string? MarkdownAnnouncement { get; }

    public bool Validated { get { return validated; } set { SetValue(ref validated, value); } }

    private string TaskName => "Announcement" + announcementName;

    public ICommandBase CloseAnnouncementCommand { get; }

    private void CloseAnnouncement()
    {
        Validated = true;
        if (DontShowAgain)
        {
            MainViewModel.SaveTaskAsDone(TaskName);
        }
    }

    private static string? Initialize(string announcementName)
    {
        try
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var path = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(x => x.EndsWith(announcementName + ".md"));
            using var stream = executingAssembly.GetManifestResourceStream(path);
            if (stream is null)
                return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
