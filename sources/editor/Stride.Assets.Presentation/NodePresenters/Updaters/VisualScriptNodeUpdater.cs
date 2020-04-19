// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Presentation.AssetEditors.VisualScriptEditor;
using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class VisualScriptNodeUpdater : AssetNodePresenterUpdaterBase
    {
        public const string OwnerAsset = nameof(OwnerAsset);

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            var provider = node.PropertyProvider as VisualScriptBlockViewModel;
            if (provider == null)
                return;

            var isBlock = typeof(Block).IsAssignableFrom(node.Type);
            if (isBlock)
            {
                node.AttachedProperties.Add(VisualScriptData.OwnerBlockKey, provider);
            }

            var memberNode = node as MemberNodePresenter;
            var isVariableReference = node.Type == typeof(string) && memberNode != null && memberNode.MemberAttributes.OfType<ScriptVariableReferenceAttribute>().Any();
            if (isVariableReference)
            {
                node.AttachedProperties.Add(ReferenceData.Key, new SymbolReferenceViewModel());
            }
        }
    }
}
