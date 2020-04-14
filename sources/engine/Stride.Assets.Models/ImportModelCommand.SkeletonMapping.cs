// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    public partial class ImportModelCommand
    {
        class SkeletonMapping
        {
            // Node mapping from source to target skeletons
            public readonly int[] SourceToTarget;

            // Round-trip through TargetToSource[SourceToTarget[i]] so that we know easily what nodes are remapped in source skeleton side
            public readonly int[] SourceToSource;

            public SkeletonMapping(Skeleton targetSkeleton, Skeleton sourceSkeleton)
            {
                SourceToTarget = new int[sourceSkeleton.Nodes.Length]; // model => skeleton mapping
                SourceToSource = new int[sourceSkeleton.Nodes.Length]; // model => model mapping

                if (targetSkeleton == null)
                {
                    // No skeleton, we can compact everything
                    for (int i = 0; i < sourceSkeleton.Nodes.Length; ++i)
                    {
                        // Map everything to root node
                        SourceToTarget[i] = 0;
                        SourceToSource[i] = 0;
                    }
                    return;
                }

                var targetToSource = new int[targetSkeleton.Nodes.Length]; // skeleton => model mapping
                for (int i = 0; i < targetToSource.Length; ++i)
                    targetToSource[i] = -1;

                // Build mapping from model to actual skeleton
                for (int modelIndex = 0; modelIndex < sourceSkeleton.Nodes.Length; ++modelIndex)
                {
                    var node = sourceSkeleton.Nodes[modelIndex];
                    var parentModelIndex = node.ParentIndex;

                    // Find matching node in skeleton (or map to best parent)
                    var skeletonIndex = targetSkeleton.Nodes.IndexOf(x => x.Name == node.Name);

                    if (skeletonIndex == -1)
                    {
                        // Nothing match, remap to parent node
                        SourceToTarget[modelIndex] = parentModelIndex != -1 ? SourceToTarget[parentModelIndex] : 0;
                        continue;
                    }

                    // TODO: Check hierarchy for inconsistencies

                    // Name match
                    SourceToTarget[modelIndex] = skeletonIndex;
                    targetToSource[skeletonIndex] = modelIndex;
                }

                for (int modelIndex = 0; modelIndex < sourceSkeleton.Nodes.Length; ++modelIndex)
                {
                    SourceToSource[modelIndex] = targetToSource[SourceToTarget[modelIndex]];
                }
            }
        }
    }
}
