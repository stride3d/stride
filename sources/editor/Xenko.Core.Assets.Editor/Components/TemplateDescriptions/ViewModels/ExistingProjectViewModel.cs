// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Templates;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class ExistingProjectViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
    {
        private Action<ExistingProjectViewModel> RemoveAction;

        public ExistingProjectViewModel(IViewModelServiceProvider serviceProvider, UFile path, Action<ExistingProjectViewModel> removeAction)
            : base(serviceProvider)
        {
            Path = path;
            Id = Guid.NewGuid();
            RemoveAction = removeAction ?? throw new ArgumentNullException(nameof(removeAction));
            ExploreCommand = new AnonymousCommand(serviceProvider, Explore);
            RemoveCommand = new AnonymousCommand(serviceProvider, Remove);
        }

        public string Name => Path.GetFileNameWithoutExtension();

        public string Description => Path.ToWindowsPath();

        public string FullDescription => "";

        public string Group => "";

        public Guid Id { get; }

        public string DefaultOutputName => "";

        public UFile Path { get; }
        // TODO
        public BitmapImage Icon => null;

        public IEnumerable<BitmapImage> Screenshots => Enumerable.Empty<BitmapImage>();

        public ICommandBase ExploreCommand { get; }

        public ICommandBase RemoveCommand { get; }

        public TemplateDescription GetTemplate()
        {
            return null;
        }

        private void Explore()
        {
            var startInfo = new ProcessStartInfo("explorer.exe", $"/select,{this.Path.ToWindowsPath()}") { UseShellExecute = true };
            var explorer = new Process { StartInfo = startInfo };
            explorer.Start();
        }

        private void Remove()
        {
            RemoveAction(this);
        }
    }
}
