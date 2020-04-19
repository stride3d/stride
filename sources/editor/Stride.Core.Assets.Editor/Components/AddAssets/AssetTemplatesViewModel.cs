// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Annotations;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.AddAssets
{
    public class AssetTemplatesViewModel : DispatcherViewModel
    {
        private KeyValuePair<TemplateDescriptionViewModel, int> selectedTemplates;

        public AssetTemplatesViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEnumerable<TemplateAssetDescription> templates)
            : base(serviceProvider)
        {
            if (templates == null) throw new ArgumentNullException(nameof(templates));
            var list = templates.Select(x => new KeyValuePair<TemplateDescriptionViewModel, int>(new TemplateDescriptionViewModel(serviceProvider, x), 0)).ToList();
            list.Sort((x, y) => x.Key.Order.CompareTo(y.Key.Order));
            Templates = list;
        }

        public AssetTemplatesViewModel([NotNull] IViewModelServiceProvider serviceProvider, int fileCount, [NotNull] IEnumerable<KeyValuePair<TemplateAssetDescription, int>> templates)
            : base(serviceProvider)
        {
            if (templates == null) throw new ArgumentNullException(nameof(templates));
            FileCount = fileCount;
            var list = templates.Select(x => new KeyValuePair<TemplateDescriptionViewModel, int>(new TemplateDescriptionViewModel(serviceProvider, x.Key), x.Value)).ToList();
            list.Sort((x, y) => x.Key.Order.CompareTo(y.Key.Order));
            Templates = list;
        }

        [NotNull]
        public IEnumerable<KeyValuePair<TemplateDescriptionViewModel, int>> Templates { get; }

        public int FileCount { get; }

        public KeyValuePair<TemplateDescriptionViewModel, int> SelectedTemplate { get { return selectedTemplates; } set { SetValue(ref selectedTemplates, value); } }
    }
}
