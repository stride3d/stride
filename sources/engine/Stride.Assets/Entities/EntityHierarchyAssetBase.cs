// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Engine;

namespace Stride.Assets.Entities
{
    /// <summary>
    /// Base class for entity assets (<see cref="SceneAsset"/> and <see cref="PrefabAsset"/>)
    /// </summary>
    [DataContract]
    public abstract partial class EntityHierarchyAssetBase : AssetCompositeHierarchy<EntityDesign, Entity>
    {
        /// <inheritdoc/>
        public override Entity GetParent(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return entity.Transform.Parent?.Entity;
        }

        /// <inheritdoc/>
        public override int IndexOf(Entity part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            var parent = GetParent(part);
            return parent?.Transform.Children.IndexOf(part.Transform) ?? Hierarchy.RootParts.IndexOf(part);
        }

        /// <inheritdoc/>
        public override Entity GetChild(Entity part, int index)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return part.Transform.Children[index].Entity;
        }

        /// <inheritdoc/>
        public override int GetChildCount(Entity part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return part.Transform.Children.Count;
        }

        /// <inheritdoc/>
        public override IEnumerable<Entity> EnumerateChildParts(Entity entity, bool isRecursive)
        {
            if (entity.Transform == null)
                return Enumerable.Empty<Entity>();

            var enumerator = isRecursive ? entity.Transform.Children.DepthFirst(t => t.Children) : entity.Transform.Children;
            return enumerator.Select(t => t.Entity);
        }
    }
}
