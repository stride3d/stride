// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class SessionNodeUpdater : AssetNodePresenterUpdaterBase
    {
        private readonly SessionViewModel session;

        public SessionNodeUpdater(SessionViewModel session)
        {
            this.session = session;
        }

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (AssetRegistry.IsContentType(node.Type) || typeof(AssetReference).IsAssignableFrom(node.Type))
            {
                node.AttachedProperties.Add(SessionData.SessionKey, session);
                node.AttachedProperties.Add(ReferenceData.Key, new ContentReferenceViewModel());
            }
            // Numeric and TimeSpan templates need access to the UndoRedoService to create transactions
            if (node.Type == typeof(TimeSpan) || node.Type.IsNumeric())
            {
                node.AttachedProperties.Add(SessionData.SessionKey, session);
            }
            if (AssetRegistry.IsContentType(node.Type))
            {
                var assetTypes = AssetRegistry.GetAssetTypes(node.Type);
                var thumbnailService = session.ServiceProvider.Get<IThumbnailService>();
                node.AttachedProperties.Add(SessionData.DynamicThumbnailKey, !assetTypes.All(thumbnailService.HasStaticThumbnail));
            }
        }
    }
}
