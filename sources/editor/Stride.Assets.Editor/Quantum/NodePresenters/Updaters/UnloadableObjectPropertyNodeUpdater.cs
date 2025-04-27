// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Yaml;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class UnloadableObjectPropertyNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Value is not IUnloadable || node.Name == DisplayData.UnloadableObjectInfo)
            return;

        node.AttachedProperties.Add(DisplayData.AutoExpandRuleKey, ExpandRule.Once);
        node.Factory.CreateVirtualNodePresenter(node, DisplayData.UnloadableObjectInfo, typeof(object), 0,
            () => node.Value,
            null,
            () => node.HasBase,
            () => node.IsInherited,
            () => node.IsOverridden);
    }
}
