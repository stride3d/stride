// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;

namespace Xenko.Navigation.Processors
{
    /// <summary>
    /// Manages the loading of the native side navigation meshes. Will only load one version of the navigation mesh if it is referenced by multiple components
    /// </summary>
    public class NavigationProcessor : EntityProcessor<NavigationComponent, NavigationProcessor.AssociatedData>
    {
        private readonly Dictionary<NavigationMesh, NavigationMeshData> loadedNavigationMeshes = new Dictionary<NavigationMesh, NavigationMeshData>();
        private DynamicNavigationMeshSystem dynamicNavigationMeshSystem;
        private GameSystemCollection gameSystemCollection;

        /// <inheritdoc />
        public override void Update(GameTime time)
        {
            // Update scene offsets for navigation components
            foreach (var p in ComponentDatas)
            {
                UpdateSceneOffset(p.Value);
            }
        }

        /// <inheritdoc />
        protected override void OnSystemAdd()
        {
            gameSystemCollection = Services.GetService<IGameSystemCollection>() as GameSystemCollection;
            if (gameSystemCollection == null)
                throw new Exception("NavigationProcessor can not access the game systems collection");

            gameSystemCollection.CollectionChanged += GameSystemsOnCollectionChanged;
        }

        /// <inheritdoc />
        protected override void OnSystemRemove()
        {
            if (gameSystemCollection != null)
            {
                gameSystemCollection.CollectionChanged += GameSystemsOnCollectionChanged;
            }

            if (dynamicNavigationMeshSystem != null)
            {
                dynamicNavigationMeshSystem.NavigationMeshUpdated -= DynamicNavigationMeshSystemOnNavigationMeshUpdatedUpdated;
            }
        }

        /// <inheritdoc />
        protected override AssociatedData GenerateComponentData(Entity entity, NavigationComponent component)
        {
            return new AssociatedData
            {
                Component = component,
            };
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, NavigationComponent component, AssociatedData associatedData)
        {
            return component == associatedData.Component;
        }

        /// <inheritdoc />
        protected override void OnEntityComponentAdding(Entity entity, NavigationComponent component, AssociatedData data)
        {
            UpdateNavigationMesh(data);

            // Handle either a change of NavigationMesh or Group
            data.Component.NavigationMeshChanged += ComponentOnNavigationMeshChanged;
        }

        /// <inheritdoc />
        protected override void OnEntityComponentRemoved(Entity entity, NavigationComponent component, AssociatedData data)
        {
            data.Component.NavigationMeshChanged -= ComponentOnNavigationMeshChanged;
        }

        private void DynamicNavigationMeshSystemOnNavigationMeshUpdatedUpdated(object sender, NavigationMeshUpdatedEventArgs eventArgs)
        {
            var newNavigationMesh = eventArgs.BuildResult.NavigationMesh;
            NavigationMeshData data;
            if (eventArgs.OldNavigationMesh != null && loadedNavigationMeshes.TryGetValue(eventArgs.OldNavigationMesh, out data))
            {
                // Move to new navigation mesh
                loadedNavigationMeshes.Remove(eventArgs.OldNavigationMesh);
                loadedNavigationMeshes.Add(newNavigationMesh, data);

                data.NavigationMesh = newNavigationMesh;

                // Replace tiles in recast navigation mesh for all loaded groups
                var updatedLayers = eventArgs.BuildResult.UpdatedLayers.ToDictionary(x => x.GroupId);
                var oldGroupKeys = data.LoadedGroups.Keys.ToList();
                foreach (var oldGroupKey in oldGroupKeys)
                {
                    var loadedGroup = data.LoadedGroups[oldGroupKey];

                    // See if this layer was updated
                    NavigationMeshLayerUpdateInfo layerUpdateInfo;
                    if (!updatedLayers.TryGetValue(oldGroupKey, out layerUpdateInfo))
                        continue;

                    // Check if the new navigation mesh contains this layer
                    //  if it does not, that means it was removed completely and we
                    //  will remove all the loaded tiles in the loop below
                    NavigationMeshLayer newLayer = null;
                    newNavigationMesh.Layers.TryGetValue(oldGroupKey, out newLayer);

                    foreach (var updatedTileCoord in layerUpdateInfo.UpdatedTiles)
                    {
                        NavigationMeshTile newTile = null;
                        if (newLayer != null)
                        {
                            if (!newLayer.Tiles.TryGetValue(updatedTileCoord, out newTile))
                                continue;
                        }

                        // Either add the tile if it is contained in the new navigation mesh or
                        //  try to remove it if it does not
                        if (newTile != null)
                        {
                            loadedGroup.RecastNavigationMesh.AddOrReplaceTile(newTile.Data);
                        }
                        else
                        {
                            loadedGroup.RecastNavigationMesh.RemoveTile(updatedTileCoord);
                        }
                    }
                }
            }

            // Update loaded navigation meshes for components that are useing it,
            //  in case a group was added
            var componentsToUpdate = ComponentDatas.Values.Where(x => x.Component.NavigationMesh == null).ToArray();
            foreach (var component in componentsToUpdate)
            {
                UpdateNavigationMesh(component);
            }
        }

        private void ComponentOnNavigationMeshChanged(object sender, EventArgs eventArgs)
        {
            var data = ComponentDatas[(NavigationComponent)sender];
            UpdateNavigationMesh(data);
        }

        private void UpdateNavigationMesh(AssociatedData data)
        {
            var navigationMeshToLoad = data.Component.NavigationMesh;
            if (navigationMeshToLoad == null && dynamicNavigationMeshSystem != null)
            {
                // Load dynamic navigation mesh when no navigation mesh is specified on the component
                navigationMeshToLoad = dynamicNavigationMeshSystem?.CurrentNavigationMesh;
            }

            NavigationMeshGroupData loadedGroup = Load(navigationMeshToLoad, data.Component.GroupId);
            if (data.LoadedGroup != null)
                Unload(data.LoadedGroup);

            data.Component.RecastNavigationMesh = loadedGroup?.RecastNavigationMesh;
            data.LoadedGroup = loadedGroup;

            UpdateSceneOffset(data);
        }

        private void UpdateSceneOffset(AssociatedData data)
        {
            // Store scene offset of entity in the component, which will make all the queries local to the baked navigation mesh (for baked navigation only)
            data.Component.SceneOffset = data.Component.NavigationMesh != null ? data.Component.Entity.Scene.Offset : Vector3.Zero;
        }

        private void GameSystemsOnCollectionChanged(object sender, TrackingCollectionChangedEventArgs trackingCollectionChangedEventArgs)
        {
            // Detect addition of dynamic navigation mesh system
            if (dynamicNavigationMeshSystem == null)
            {
                dynamicNavigationMeshSystem = gameSystemCollection.OfType<DynamicNavigationMeshSystem>().FirstOrDefault();
                if (dynamicNavigationMeshSystem != null)
                {
                    dynamicNavigationMeshSystem.NavigationMeshUpdated += DynamicNavigationMeshSystemOnNavigationMeshUpdatedUpdated;
                }
            }
        }

        /// <summary>
        /// Loads or references a <see cref="RecastNavigationMesh"/> for a group of a navigation mesh
        /// </summary>
        [CanBeNull]
        private NavigationMeshGroupData Load(NavigationMesh mesh, Guid groupId)
        {
            if (mesh == null || groupId == Guid.Empty)
                return null;

            NavigationMeshData data;
            if (!loadedNavigationMeshes.TryGetValue(mesh, out data))
            {
                loadedNavigationMeshes.Add(mesh, data = new NavigationMeshData
                {
                    NavigationMesh = mesh,
                });
            }

            NavigationMeshGroupData groupData;
            if (!data.LoadedGroups.TryGetValue(groupId, out groupData))
            {
                NavigationMeshLayer layer;
                if (!mesh.Layers.TryGetValue(groupId, out layer))
                    return null; // Group not present in navigation mesh

                data.LoadedGroups.Add(groupId, groupData = new NavigationMeshGroupData
                {
                    Data = data,
                    RecastNavigationMesh = new RecastNavigationMesh(mesh),
                    Id = groupId,
                });

                // Add initial tiles to the navigation mesh
                foreach (var tile in layer.Tiles)
                {
                    if (!groupData.RecastNavigationMesh.AddOrReplaceTile(tile.Value.Data))
                        throw new InvalidOperationException("Failed to add tile");
                }
            }

            groupData.AddReference();
            return groupData;
        }

        /// <summary>
        /// Removes a reference to a group
        /// </summary>
        private void Unload(NavigationMeshGroupData group)
        {
            int referenceCount = group.Release();
            if (referenceCount < 0)
                throw new ArgumentOutOfRangeException();

            if (referenceCount == 0)
            {
                // Remove group
                var data = group.Data;
                data.LoadedGroups.Remove(group.Id);

                // Remove data
                if (data.LoadedGroups.Count == 0)
                {
                    loadedNavigationMeshes.Remove(data.NavigationMesh);
                }
            }
        }

        /// <summary>
        /// Associated data for navigation mesh components
        /// </summary>
        public class AssociatedData
        {
            internal NavigationComponent Component;
            internal NavigationMeshGroupData LoadedGroup;
        }

        /// <summary>
        /// Contains groups that are loaded for a navigation mesh
        /// </summary>
        internal class NavigationMeshData
        {
            public NavigationMesh NavigationMesh;
            public readonly Dictionary<Guid, NavigationMeshGroupData> LoadedGroups = new Dictionary<Guid, NavigationMeshGroupData>();
        }

        /// <summary>
        /// A loaded group of a navigation mesh
        /// </summary>
        internal class NavigationMeshGroupData : IReferencable
        {
            public NavigationMeshData Data;
            public RecastNavigationMesh RecastNavigationMesh;
            public Guid Id;

            public int ReferenceCount { get; private set; } = 0;

            public int AddReference()
            {
                return ++ReferenceCount;
            }

            public int Release()
            {
                return --ReferenceCount;
            }
        }

/*
        internal class NavigationMeshInternal : IDisposable
        {
            private readonly float cellTileSize;
            private readonly HashSet<object> references = new HashSet<object>();

            public IntPtr[] Layers;

            public NavigationMeshInternal(NavigationMesh navigationMesh)
            {
                cellTileSize = navigationMesh.TileSize * navigationMesh.CellSize;
                Layers = new IntPtr[navigationMesh];
                for (int i = 0; i < navigationMesh.NumLayers; i++)
                {
                    Layers[i] = LoadLayer(navigationMesh.Layers[i]);
                }
            }

            public void Dispose()
            {
                if (Layers == null)
                    return;
                for (int i = 0; i < Layers.Length; i++)
                {
                    if (Layers[i] != IntPtr.Zero)
                        Navigation.DestroyNavmesh(Layers[i]);
                }
                Layers = null;
            }

            /// <summary>
            /// Adds a reference to this object
            /// </summary>
            /// <param name="reference"></param>
            public void AddReference(object reference)
            {
                references.Add(reference);
            }

            /// <summary>
            ///  Removes a reference to this object
            /// </summary>
            /// <param name="reference"></param>
            /// <returns>true if the object is no longer referenced</returns>
            public bool RemoveReference(object reference)
            {
                references.Remove(reference);
                return references.Count == 0;
            }

            private unsafe IntPtr LoadLayer(NavigationMeshLayer navigationMeshLayer)
            {
                IntPtr layer = Navigation.CreateNavmesh(cellTileSize);
                if (layer == IntPtr.Zero)
                    return layer;

                // Add all the tiles to the navigation mesh
                foreach (var tile in navigationMeshLayer.Tiles)
                {
                    if (tile.Value.Data == null)
                        continue; // Just skip empty tiles
                    fixed (byte* inputData = tile.Value.Data)
                    {
                        Navigation.AddTile(layer, tile.Key, new IntPtr(inputData), tile.Value.Data.Length);
                    }
                }

                return layer;
            }
        }
*/
    }
}
