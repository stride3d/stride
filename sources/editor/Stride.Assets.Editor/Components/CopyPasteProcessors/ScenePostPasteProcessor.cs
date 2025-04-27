// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;

namespace Stride.Assets.Editor.Components.CopyPasteProcessors;

internal sealed class ScenePostPasteProcessor : AssetPostPasteProcessorBase<SceneAsset>
{
    /// <inheritdoc />
    protected override void PostPasteDeserialization(SceneAsset asset)
    {
        // Clear all references (for now)
        asset.Parent = null;
        asset.ChildrenIds.Clear();
    }
}
