// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class ExistingProjectViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
    {
        private Action<ExistingProjectViewModel> RemoveAction;

        public ExistingProjectViewModel(IViewModelServiceProvider serviceProvider, UFile path, Action<ExistingProjectViewModel> openAction, Action<ExistingProjectViewModel> removeAction)
            : base(serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(openAction);
            Path = path;
            Id = Guid.NewGuid();
            RemoveAction = removeAction ?? throw new ArgumentNullException(nameof(removeAction));
            OpenCommand = new AnonymousCommand(serviceProvider, () => openAction(this));
            ExploreCommand = new AnonymousCommand(serviceProvider, Explore);
            CopySolutionPathCommand = new AnonymousCommand(serviceProvider, () => CopyToClipboard(Path.ToOSPath()));
            CopyFolderPathCommand = new AnonymousCommand(serviceProvider, () => CopyToClipboard(Path.GetFullDirectory().ToOSPath()));
            RemoveCommand = new AnonymousCommand(serviceProvider, Remove);
        }

        public string Name => Path.GetFileNameWithoutExtension();

        public string Description => Path.ToOSPath();

        public string FullDescription => "";

        public string Group => "";

        public Guid Id { get; }

        public string DefaultOutputName => "";

        public UFile Path { get; }
        // TODO
        public BitmapImage Icon => null;

        public IEnumerable<BitmapImage> Screenshots => Enumerable.Empty<BitmapImage>();

        public ICommandBase OpenCommand { get; }

        public ICommandBase ExploreCommand { get; }

        public ICommandBase CopySolutionPathCommand { get; }

        public ICommandBase CopyFolderPathCommand { get; }

        public ICommandBase RemoveCommand { get; }

        public TemplateDescription GetTemplate()
        {
            return null;
        }

        private void Explore()
        {
            var startInfo = new ProcessStartInfo("explorer.exe", $"/select,\"{this.Path.ToOSPath()}\"") { UseShellExecute = true };
            var explorer = new Process { StartInfo = startInfo };
            explorer.Start();
        }

        private static void CopyToClipboard(string text)
        {
            try
            {
                SafeClipboard.SetText(text);
            }
            catch (SystemException e)
            {
                // We don't provide feedback when copying fails.
                e.Ignore();
            }
        }

        private void Remove()
        {
            RemoveAction(this);
        }
    }
}
