// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Mathematics;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.AssetEditors.SceneEditor.ViewModels
{
    internal class AddSceneAssetPolicy : CustomPolicyBase<SceneAsset, SceneViewModel>
    {
        /// <inheritdoc />
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, SceneViewModel asset, AddChildModifiers modifiers, int index, out string message, params object[] messageArgs)
        {
            var currentRoot = parent.Owner as SceneRootViewModel;
            // Note: scene are inserted after all entities
            if (currentRoot == null || index < currentRoot.EntityCount)
            {
                message = "Can only add a scene to another scene";
                return false;
            }
            if (!currentRoot.SceneAsset.CanBeParentOf(asset, out message, true))
            {
                return false;
            }
            message = "Add to this scene";
            return true;
        }

        /// <inheritdoc />
        protected override void ApplyPolicy(EntityHierarchyItemViewModel parent, IReadOnlyCollection<SceneViewModel> assets, int index, Vector3 position)
        {
            var currentRoot = parent.Owner as SceneRootViewModel;
            var currentScene = currentRoot?.Asset as SceneViewModel;
            if (currentScene == null)
                return;

            // Note: scene are inserted after all entities
            index -= currentRoot.EntityCount;
            // Get common "root" scenes (in case we are trying to drop two scenes that are already part of the same hierarchy.
            var commonRoots = GetCommonRoots(assets);
            // Move the scenes in two steps: first remove all, then insert all
            foreach (var scene in commonRoots)
            {
                // Some of the scenes we're moving might already be children of the current scene, let's count for their removal in the insertion index.
                var rootIndex = currentScene.Children.IndexOf(scene);
                if (rootIndex >= 0 && rootIndex < index)
                    --index;

                scene.Parent?.Children.Remove(scene);
            }
            foreach (var scene in commonRoots)
            {
                currentScene.Children.Insert(index, scene);
                currentRoot.ChildScenes[index].Offset = position;
                // Make sure the scene is loaded
                currentRoot.ChildScenes[index].RequestLoading(true).Forget();
                ++index;
            }
        }

        [NotNull]
        private static ISet<SceneViewModel> GetCommonRoots([NotNull] IReadOnlyCollection<SceneViewModel> items)
        {
            var hashSet = new HashSet<SceneViewModel>(items);
            foreach (var item in items)
            {
                var parent = item.Parent;
                while (parent != null)
                {
                    if (hashSet.Contains(parent))
                    {
                        hashSet.Remove(item);
                        break;
                    }
                    parent = parent.Parent;
                }
            }
            return hashSet;
        }
    }
}
