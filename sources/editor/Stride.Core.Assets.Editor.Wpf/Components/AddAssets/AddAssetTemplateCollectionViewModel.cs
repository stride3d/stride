// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;

namespace Stride.Core.Assets.Editor.Components.AddAssets
{
    public class AddAssetTemplateCollectionViewModel : AddItemTemplateCollectionViewModel
    {
        private readonly SessionViewModel session;

        public AddAssetTemplateCollectionViewModel(SessionViewModel session)
            : base(session.ServiceProvider)
        {
            this.session = session;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the list from the session's packages: a package added since (a plugin joining the solution)
        /// brings its own asset templates.
        /// </summary>
        public void Refresh()
        {
            RootGroup.Clear();
            foreach (TemplateDescription template in session.FindTemplates(TemplateScope.Asset))
            {
                // A package can be in the session while the assembly defining its asset type is not loaded
                if (template is TemplateAssetDescription assetTemplate && assetTemplate.FindAssetType() is null)
                    continue;

                var group = ProcessGroup(RootGroup, template.Group);
                if (group != null)
                {
                    var viewModel = new TemplateDescriptionViewModel(session.ServiceProvider, template);
                    group.Templates.Add(viewModel);
                }
            }

            SelectedGroup = RootGroup;
            UpdateTemplateList();
        }

        public DirectoryBaseViewModel CurrentDirectory { get; set; }

        public DirectoryBaseViewModel TargetDirectory { get; private set; }

        protected override string UpdateNameFromSelectedTemplate()
        {
            var selectedTemplate = SelectedTemplate?.GetTemplate() as TemplateAssetDescription;
            if (selectedTemplate == null || !selectedTemplate.RequireName)
                return null;

            // If the mount point of the current folder does not support this type of asset, try to select the first mount point that support it.
            var assetType = selectedTemplate.GetAssetType();
            TargetDirectory = AssetViewModel.FindValidCreationLocation(assetType, CurrentDirectory);
            if (TargetDirectory == null)
                return null;

            var baseName = selectedTemplate.DefaultOutputName ?? selectedTemplate.AssetTypeName;
            var name = NamingHelper.ComputeNewName(baseName, TargetDirectory.Assets, x => x.Name, "{0}{1}");

            return name;
        }
    }
}
