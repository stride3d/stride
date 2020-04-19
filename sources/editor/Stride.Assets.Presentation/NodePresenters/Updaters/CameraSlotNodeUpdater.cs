// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Rendering;
using Stride.Editor.Build;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class CameraSlotNodeUpdater : AssetNodePresenterUpdaterBase
    {
        private readonly SessionViewModel session;

        /// <summary>
        /// Creates a new instance of <see cref="UIAssetNodeUpdater"/>.
        /// </summary>
        /// <param name="session"></param>
        public CameraSlotNodeUpdater(SessionViewModel session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            this.session = session;
        }

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (node.Value is SceneCameraSlot)
            {
                SceneCameraSlot slot = (SceneCameraSlot)node.Value;

                // Hide children
                node.Children.ForEach(x => x.IsVisible = false);
                node.AttachedProperties.Set(DisplayData.AttributeDisplayNameKey, slot.Name);
            }
            else if (node.Value is SceneCameraSlotId)
            {
                // Grab all graphics compositor of the same package as the asset.
                // TODO: we do not take all package dependencies for now because we don't want to include Stride package compositors.
                var graphicsCompositors = node.Asset.Directory.Package.Assets.Where(x => x.AssetType == typeof(GraphicsCompositorAsset));

                var entries = new List<AbstractNodeEntry>();
                foreach (var compositor in graphicsCompositors)
                {
                    var asset = (GraphicsCompositorAsset)compositor.Asset;
                    entries.Add(new AbstractNodeValue(new SceneCameraSlotId(), AbstractNodeValue.Null.DisplayValue, AbstractNodeValue.Null.Order));
                    var i = 0;
                    foreach (var slot in asset.Cameras)
                    {
                        var slotId = slot.ToSlotId();
                        var entry = new AbstractNodeValue(slotId, $"{compositor.Url} > {slot.Name}", i++);
                        entries.Add(entry);
                    }
                }
                node.AttachedProperties.Add(AbstractNodeEntryData.Key, entries);

                node.Children.ForEach(x => x.IsVisible = false);
                // TODO: turn this into a real command!
                if (node.Commands.All(x => x.Name != CameraSlotData.UpdateCameraSlotIndex))
                {
                    node.Commands.Add(new SyncAnonymousNodePresenterCommand(CameraSlotData.UpdateCameraSlotIndex, (n, id) => UpdateCameraSlotIndex(n, (SceneCameraSlotId)id)));
                }
            }
        }

        private static void UpdateCameraSlotIndex(INodePresenter node, SceneCameraSlotId id)
        {
            node.UpdateValue(id);
            // TODO: we shouldn't have to manually refresh this.
            node.Children.ForEach(x => x.IsVisible = false);        
        }
    }
}
