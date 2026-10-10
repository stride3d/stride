// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Engine;
using Stride.SpriteStudio.Assets;
using Stride.SpriteStudio.Runtime;

namespace Stride.SpriteStudio.Editor;

/// <summary>
/// Dropping a SpriteStudio sheet on the scene creates an entity with a <see cref="SpriteStudioComponent"/>.
/// </summary>
internal sealed class AddSpriteStudioModelAssetPolicy : CreateComponentPolicyBase<SpriteStudioModelAsset, SpriteStudioModelViewModel>
{
    /// <inheritdoc />
    [NotNull]
    protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, SpriteStudioModelViewModel asset)
    {
        return new SpriteStudioComponent
        {
            Sheet = ContentReferenceHelper.CreateReference<SpriteStudioSheet>(asset),
        };
    }
}
