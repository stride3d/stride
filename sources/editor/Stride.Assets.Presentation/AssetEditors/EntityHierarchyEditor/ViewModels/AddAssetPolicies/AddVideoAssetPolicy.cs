// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Media;
using Stride.Engine;
using Stride.Video;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal class AddVideoAssetPolicy : CreateComponentPolicyBase<VideoAsset, AssetViewModel<VideoAsset>>
    {
        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel<VideoAsset> asset)
        {
            return new VideoComponent
            {
                Source = ContentReferenceHelper.CreateReference<Video.Video>(asset)
            };
        }
    }
}
