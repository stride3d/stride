// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using Stride.Core.Extensions;

namespace Stride.Editor.EditorGame.ContentLoader
{
    public class LoaderReferenceManager
    {
        private readonly record struct ReferenceAccessor
        {
            public readonly IGraphNode ContentNode;
            public readonly NodeIndex Index;

            public ReferenceAccessor(IGraphNode contentNode, NodeIndex index)
            {
                this.ContentNode = contentNode;
                this.Index = index;
            }

            public void Update(object newValue)
            {
                if (Index == NodeIndex.Empty)
                {
                    ((IMemberNode)ContentNode).Update(newValue);
                }
                else
                {
                    ((IObjectNode)ContentNode).Update(newValue, Index);
                }
            }

            public Task Clear([NotNull] LoaderReferenceManager manager, AbsoluteId referencerId, AssetId contentId)
            {
                return manager.ClearContentReference(referencerId, contentId, ContentNode, Index);
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
                if (!references.TryGetValue(referencerId, out var referencer))
                    throw new InvalidOperationException("The given referencer is not registered.");

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
                if (!references.TryGetValue(referencerId, out var referencer))
                    throw new InvalidOperationException("The given referencer is not registered.");

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

                if (contents.TryGetValue(contentId, out var value))
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

        /// <summary>
        /// This will clear all references within <see cref="contentNode"/> starting with <see cref="rootIndex"/>.
        /// </summary>
        /// <param name="referencerId"></param>
        /// <param name="contentNode"></param>
        /// <param name="rootIndex"></param>
        /// <returns></returns>
        public async Task ClearContentReferencesFromNodes(AbsoluteId referencerId, IReadOnlySet<IGraphNode> nodes)
        {
            gameDispatcher.EnsureAccess();
            using (await loader.LockDatabaseAsynchronously())
            {
                if (!references.ContainsKey(referencerId))
                    throw new InvalidOperationException("The given referencer is not registered.");

                var referencer = references[referencerId];

                foreach (var accessors in referencer.ToList())
                {
                    for (int i = 0; i < accessors.Value.Count; ++i)
                    {
                        if (nodes.Contains(accessors.Value[i].ContentNode))
                        {
                            // Since accessor will be removed, we also adjust index for next iteration
                            await RemoveAccessor(accessors.Key, referencer, accessors.Value, i--);
                        }
                    }
                }
            }
        }

        public async Task ClearContentReference(AbsoluteId referencerId, AssetId contentId, IGraphNode contentNode, NodeIndex index)
        {
            gameDispatcher.EnsureAccess();
            using (await loader.LockDatabaseAsynchronously())
            {
                if (!references.TryGetValue(referencerId, out var referencer))
                    throw new InvalidOperationException("The given referencer is not registered.");

                if (!referencer.TryGetValue(contentId, out var accessors))
                    throw new InvalidOperationException("The given content is not registered to the given referencer.");

                var accessor = new ReferenceAccessor(contentNode, index);
                var accesorIndex = accessors.IndexOf(accessor);
                if (accesorIndex < 0)
                    throw new InvalidOperationException("The given reference is not registered for the given content and referencer.");

                await RemoveAccessor(contentId, referencer, accessors, accesorIndex);
            }
        }

        private async Task RemoveAccessor(AssetId contentId, Dictionary<AssetId, List<ReferenceAccessor>> referencer, List<ReferenceAccessor> accessors, int accesorIndex)
        {
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
