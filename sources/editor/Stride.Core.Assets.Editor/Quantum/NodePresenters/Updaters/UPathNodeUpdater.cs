// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class UPathNodeUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            if (typeof(UPath).IsAssignableFrom(node.Type))
            {
                node.AttachedProperties.Add(ReferenceData.Key, new UPathReferenceViewModel());
            }
        }
    }
}
