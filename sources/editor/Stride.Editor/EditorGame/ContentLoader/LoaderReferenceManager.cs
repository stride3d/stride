// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Editor.EditorGame.ContentLoader
{
    public class LoaderReferenceManager
    {
        private struct ReferenceAccessor
        {
            private readonly IGraphNode contentNode;
            private readonly NodeIndex index;

            public ReferenceAccessor(IGraphNode contentNode, NodeIndex index)
            {
                this.contentNode = contentNode;
                this.index = index;
            }

            public void Update(object newValue)
            {
                if (index == NodeIndex.Empty)
                {
                    ((IMemberNode)contentNode).Update(newValue);
                }
                else
                {
                    ((IObjectNode)contentNode).Update(newValue, index);
                }
            }

            public Task Clear([NotNull] LoaderReferenceManager manager, AbsoluteId referencerId, AssetId contentId)
            {
                return manager.ClearContentReference(referencerId, contentId, contentNode, index);
            }
        }

        private readonly IDispatcherService gameDispatcher;
        private readonly IEditorContentLoader loader;
        private readonly Dictionary<AbsoluteId, Dictionary<AssetId, List<ReferenceAccessor>>> references = new Dictionary<AbsoluteId, Dictionary<AssetId, List<ReferenceAccessor>>>();
        private readonly Dictionary<AssetId, object> contents = new Dictionary<AssetId, object>();
        private readonly HashSet<AssetId> buildPending = new HashSet<AssetId>();

        public LoaderReferenceManager(IDispatcherService gameDispatcher, IEditorContentLoader loader)
        {
            this.gameDispatcher = gameDispatcher;
            this.loader = loader;
        }

        public async Task RegisterReferencer(AbsoluteId referencerId)
        {
            gameDispatcher.EnsureAccess();
            using (await loader.LockDatabaseAsynchronously())
            {
                if (references.ContainsKey(referencerId))
                    throw new InvalidOperationException("The given referencer is already registered.");

                references.Add(referencerId, new Dictionary<AssetId, List<ReferenceAccessor>>());
            }
        }

        public async Task RemoveReferencer(AbsoluteId referencerId)
        {
            gameDispatcher.EnsureAccess();
            using (await loader.LockDatabaseAsynchronously())
            {
                if (!references.ContainsKey(referencerId))
                    throw new InvalidOperationException("The given referencer is not registered.");

                var referencer = references[referencerId];
                // Properly clear all reference first
                foreach (var content in referencer.ToDictionary(x => x.Key, x => x.Value))
                {
                    foreach (var reference in content.Value.ToList())
                    {
                        // Ok to await in the loop, Clear should never yield because we already own the lock.
                        await reference.Clear(this, referencerId, content.Key);
                    }
                }
                references.Remove(referencerId);
            }
        }

        public async Task PushContentReference(AbsoluteId referencerId, AssetId contentId, IGraphNode contentNode, NodeIndex index)
        {
            gameDispatcher.EnsureAccess();
            using (await loader.LockDatabaseAsynchronously())
            {
                if (!references.ContainsKey(referencerId))
                    throw new InvalidOperationException("The given referencer is not registered.");

                var referencer = references[referencerId];
                List<ReferenceAccessor> accessors;
                if (!referencer.TryGetValue(contentId, out accessors))
                {
                    accessors = new List<ReferenceAccessor>();
                    referencer[contentId] = accessors;
                }
                var accessor = new ReferenceAccessor(contentNode, index);
                if (accessors.Contains(accessor))
                {
                    // If the reference already exists, clear it and re-enter
                    await ClearContentReference(referencerId, contentId, contentNode, index);
                    await PushContentReference(referencerId, contentId, contentNode, index);
                    return;
                }

                accessors.Add(accessor);

                object value;
                if (contents.TryGetValue(contentId, out value))
                {
                    accessor.Update(value);
                }
                else
                {
                    // Build only if not requested yet (otherwise we just need to wait for ReplaceContent() to be called, it will also replace this reference since it was added just before)
                    if (buildPending.Add(contentId))
                        loader.BuildAndReloadAsset(contentId);
                }
            }
        }

        public async Task ClearContentReference(AbsoluteId referencerId, AssetId contentId, IGraphNode contentNode, NodeIndex index)
        {
            gameDispatcher.EnsureAccess();
            using (await loader.LockDatabaseAsynchronously())
            {
                if (!references.ContainsKey(referencerId))
                    throw new InvalidOperationException("The given referencer is not registered.");

                var referencer = references[referencerId];
                if (!referencer.ContainsKey(contentId))
                    throw new InvalidOperationException("The given content is not registered to the given referencer.");

                var accessors = referencer[contentId];
                var accessor = new ReferenceAccessor(contentNode, index);
                var accesorIndex = accessors.IndexOf(accessor);
                if (accesorIndex < 0)
                    throw new InvalidOperationException("The given reference is not registered for the given content and referencer.");

                accessors.RemoveAt(accesorIndex);
                if (accessors.Count == 0)
                {
                    referencer.Remove(contentId);
                    // Unload the content if nothing else is referencing it anymore
                    var unloadContent = references.Values.SelectMany(x => x.Keys).All(x => x != contentId);
                    if (unloadContent)
                    {
                        await loader.UnloadAsset(contentId);
                        contents.Remove(contentId);
                    }
                }
            }
        }

        public async Task ReplaceContent(AssetId contentId, object newValue)
        {
            gameDispatcher.EnsureAccess();
            using (await loader.LockDatabaseAsynchronously())
            {
                buildPending.Remove(contentId);

                // In case content was not properly loaded, just keep existing one
                if (newValue != null)
                {
                    foreach (var referencer in references.Values)
                    {
                        List<ReferenceAccessor> accessors;
                        if (referencer.TryGetValue(contentId, out accessors))
                        {
                            foreach (var accessor in accessors)
                            {
                                accessor.Update(newValue);
                            }
                        }
                    }
                    contents[contentId] = newValue;
                }
            }
        }

        public async Task<HashSet<AssetId>> ComputeReferencedAssets()
        {
            using ((await loader.ReserveDatabaseSyncLock()).Lock())
            {
                return new HashSet<AssetId>(references.Values.SelectMany(x => x.Keys));
            }
        }
    }
}
