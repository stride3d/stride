// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Xenko.Core.Assets.Templates;
using Xenko.Core.IO;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class ExistingProjectViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
    {
        public ExistingProjectViewModel(IViewModelServiceProvider serviceProvider, UFile path)
            : base(serviceProvider)
        {
            Path = path;
            Id = Guid.NewGuid();
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

        public TemplateDescription GetTemplate()
        {
            return null;
        }
    }
}
