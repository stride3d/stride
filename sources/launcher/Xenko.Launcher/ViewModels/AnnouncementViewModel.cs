// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.LauncherApp.ViewModels
{
    internal class AnnouncementViewModel : DispatcherViewModel
    {
        private readonly LauncherViewModel launcher;
        private readonly string announcementName;
        private bool validated = true;
        private bool dontShowAgain;

        public AnnouncementViewModel(LauncherViewModel launcher, string announcementName)
            : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider)
        {
            this.launcher = launcher;
            this.announcementName = announcementName;
            if (!LauncherViewModel.HasDoneTask(TaskName))
            {
                MarkdownAnnouncement = Initialize(announcementName);
            }
            // We want to explicitely trigger the property change notification for the view storyboard
            Dispatcher.InvokeAsync(() => Validated = false);
            CloseAnnouncementCommand = new AnonymousCommand(ServiceProvider, CloseAnnouncement);
        }

        private void CloseAnnouncement()
        {
            Validated = true;
            if (DontShowAgain)
            {
                LauncherViewModel.SaveTaskAsDone(TaskName);
            }
        }

        public string MarkdownAnnouncement { get; }

        public bool Validated { get { return validated; } set { SetValue(ref validated, value); } }

        public bool DontShowAgain { get { return dontShowAgain; } set { SetValue(ref dontShowAgain, value); } }

        public ICommandBase CloseAnnouncementCommand { get; }

        private string TaskName => GetTaskName(announcementName);

        public static string GetTaskName(string announcementName)
        {
            return "Announcement" + announcementName;
        }

        private static string Initialize(string announcementName)
        {
            try
            {
                var executingAssembly = Assembly.GetExecutingAssembly();
                var path = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(x => x.EndsWith(announcementName + ".md"));
                using (var stream = executingAssembly.GetManifestResourceStream(path))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
