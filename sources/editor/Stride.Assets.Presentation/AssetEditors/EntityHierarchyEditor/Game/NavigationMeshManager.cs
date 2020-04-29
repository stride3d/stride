// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Navigation;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    /// <summary>
    /// Used to always load the navigation meshes associated with a given scene by using <see cref="AddUnique"/> to add a navigation mesh to keep loaded.
    /// When a registed navigation mesh is reloaded, <see cref="Changed"/> will get called
    /// </summary>
    [DataContract]
    public class NavigationMeshManager : Core.IAsyncDisposable
    {
        [DataMember]
        public readonly Dictionary<AssetId, NavigationMesh> Meshes = new Dictionary<AssetId, NavigationMesh>();

        private readonly AbsoluteId referencerId;
        private readonly IEditorContentLoader loader;
        private readonly IObjectNode meshesNode;

        public NavigationMeshManager([NotNull] IEditorGameController controller)
        {
            referencerId = new AbsoluteId(AssetId.Empty, Guid.NewGuid());
            loader = controller.Loader;
            var root = controller.GameSideNodeContainer.GetOrCreateNode(this);
            meshesNode = root[nameof(Meshes)].Target;
            meshesNode.ItemChanged += (sender, args) => { Changed?.Invoke(this, args); };
        }

        public async Task DisposeAsync()
        {
            foreach (var pair in Meshes)
            {
                await loader.Manager.ClearContentReference(referencerId, pair.Key, meshesNode, new NodeIndex(pair.Key));
            }
            await loader.Manager.RemoveReferencer(referencerId);
        }

        public event EventHandler<ItemChangeEventArgs> Changed;

        public Task Initialize()
        {
            return loader.Manager.RegisterReferencer(referencerId);
        }

        /// <summary>
        /// Adds a reference to a navigation mesh if it doesn't already exist
        /// </summary>
        /// <param name="assetId"></param>
        public Task AddUnique(AssetId assetId)
        {
            if (Meshes.ContainsKey(assetId))
                return Task.CompletedTask;

            Meshes.Add(assetId, new NavigationMesh());
            return loader.Manager.PushContentReference(referencerId, assetId, meshesNode, new NodeIndex(assetId));
        }

        /// <summary>
        /// Removes a reference if it exists
        /// </summary>
        /// <param name="assetId"></param>
        public Task Remove(AssetId assetId)
        {
            if (!Meshes.ContainsKey(assetId))
                throw new InvalidOperationException();

            Meshes.Remove(assetId);
            return loader.Manager.ClearContentReference(referencerId, assetId, meshesNode, new NodeIndex(assetId));
        }
    }
}
