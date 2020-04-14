// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Assets.UI;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
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
