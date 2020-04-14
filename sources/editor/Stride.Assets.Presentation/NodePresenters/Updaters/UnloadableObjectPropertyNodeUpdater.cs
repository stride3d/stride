// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Xenko.Core;
using Xenko.Core.Yaml;

namespace Xenko.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class UnloadableObjectPropertyNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (node.Value is IUnloadable && node.Name != DisplayData.UnloadableObjectInfo)
            {
                node.AttachedProperties.Add(DisplayData.AutoExpandRuleKey, ExpandRule.Once);
                node.Factory.CreateVirtualNodePresenter(node, DisplayData.UnloadableObjectInfo, typeof(object), 0,
                    () => node.Value,
                    null,
                    () => node.HasBase,
                    () => node.IsInherited,
                    () => node.IsOverridden);
            }
        }
    }
}
