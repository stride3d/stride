// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;
using Stride.Video.Assets;

namespace Stride.Video.Editor
{
    public sealed class AddVideoAssetPolicy : CreateComponentPolicyBase<VideoAsset, AssetViewModel<VideoAsset>>
    {
        /// <inheritdoc />
        [NotNull]
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel<VideoAsset> asset)
        {
            return new VideoComponent
            {
                Source = ContentReferenceHelper.CreateReference<global::Stride.Video.Video>(asset)
            };
        }
    }
}
