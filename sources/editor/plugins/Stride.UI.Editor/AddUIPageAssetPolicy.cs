// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.UI.Assets;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.UI.Editor.ViewModels;

namespace Stride.UI.Editor
{
    internal class AddUIPageAssetPolicy : CreateComponentPolicyBase<UIPageAsset, UIPageViewModel>
    {
        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, UIPageViewModel asset)
        {
            return new UIComponent
            {
                Page = ContentReferenceHelper.CreateReference<UIPage>(asset)
            };
        }
    }
}
