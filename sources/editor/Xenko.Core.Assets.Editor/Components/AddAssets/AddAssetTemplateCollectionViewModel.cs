// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.AddAssets
{
    public class AddAssetTemplateCollectionViewModel : AddItemTemplateCollectionViewModel
    {
        public AddAssetTemplateCollectionViewModel(SessionViewModel session)
            : base(session.ServiceProvider)
        {
            foreach (TemplateDescription template in session.FindTemplates(TemplateScope.Asset))
            {
                var group = ProcessGroup(RootGroup, template.Group);
                if (group != null)
                {
                    var viewModel = new TemplateDescriptionViewModel(session.ServiceProvider, template);
                    group.Templates.Add(viewModel);
                }
            }

            SelectedGroup = RootGroup;
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
