// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    public partial class ImportModelCommand
    {
        public string SkeletonUrl { get; set; }
        public List<KeyValuePair<string, bool>> SkeletonNodesWithPreserveInfo { get; set; }

        private object ExportSkeleton(ICommandContext commandContext, ContentManager contentManager)
        {
            var skeleton = LoadSkeleton(commandContext, contentManager);
            AdjustSkeleton(skeleton);

            var modelNodes = new HashSet<string>(skeleton.Nodes.Select(x => x.Name));
            var skeletonNodes = new HashSet<string>(SkeletonNodesWithPreserveInfo.Select(x => x.Key));

            // List missing nodes on both sides, to display warnings
            var missingNodesInModel = new HashSet<string>(skeletonNodes);
            missingNodesInModel.ExceptWith(modelNodes);

            var missingNodesInAsset = new HashSet<string>(modelNodes);
            missingNodesInAsset.ExceptWith(skeletonNodes);

            // Output warnings if skeleton was not properly reimported from latest FBX
            if (missingNodesInAsset.Count > 0)
                commandContext.Logger.Warning($"{missingNodesInAsset.Count} node(s) were present in model [{SourcePath}] but not in asset [{Location}], please reimport: {string.Join(", ", missingNodesInAsset)}");

            if (missingNodesInModel.Count > 0)
                commandContext.Logger.Warning($"{missingNodesInModel.Count} node(s) were present in asset [{Location}] but not in model [{SourcePath}], please reimport: {string.Join(", ", missingNodesInModel)}");

            // Build node mapping to expected structure
            var optimizedNodes = new HashSet<string>(SkeletonNodesWithPreserveInfo.Where(x => !x.Value).Select(x => x.Key));

            // Refresh skeleton updater with loaded skeleton (to be able to compute matrices)
            var hierarchyUpdater = new SkeletonUpdater(skeleton);
            hierarchyUpdater.UpdateMatrices();

            // Removed optimized nodes
            var filteredSkeleton = new Skeleton { Nodes = skeleton.Nodes.Where(x => !optimizedNodes.Contains(x.Name)).ToArray() };

            // Fix parent indices (since we removed some nodes)
            for (int i = 0; i < filteredSkeleton.Nodes.Length; ++i)
            {
                var parentIndex = filteredSkeleton.Nodes[i].ParentIndex;
                if (parentIndex != -1)
                {
                    // Find appropriate parent to map to
                    var newParentIndex = -1;
                    while (newParentIndex == -1 && parentIndex != -1)
                    {
                        var nodeName = skeleton.Nodes[parentIndex].Name;
                        parentIndex = skeleton.Nodes[parentIndex].ParentIndex;
                        newParentIndex = filteredSkeleton.Nodes.IndexOf(x => x.Name == nodeName);
                    }
                    filteredSkeleton.Nodes[i].ParentIndex = newParentIndex;
                }
            }

            // Generate mapping
            var skeletonMapping = new SkeletonMapping(filteredSkeleton, skeleton);

            // Children of remapped nodes need to have their matrices updated
            for (int i = 0; i < skeleton.Nodes.Length; ++i)
            {
                // Skip node if it doesn't exist in source skeleton
                if (skeletonMapping.SourceToSource[i] != i)
                    continue;

                var node = skeleton.Nodes[i];
                var filteredIndex = skeletonMapping.SourceToTarget[i];
                var oldParentIndex = node.ParentIndex;

                if (oldParentIndex != -1 && skeletonMapping.SourceToSource[oldParentIndex] != oldParentIndex)
                {
                    // Compute matrix for intermediate missing nodes
                    var transformMatrix = CombineMatricesFromNodeIndices(hierarchyUpdater.NodeTransformations, skeletonMapping.SourceToSource[oldParentIndex], oldParentIndex);
                    var localMatrix = hierarchyUpdater.NodeTransformations[i].LocalMatrix;

                    // Combine it with local matrix, and use that instead in the new skeleton; resulting node should be same position as before optimized nodes were removed
                    localMatrix = Matrix.Multiply(localMatrix, transformMatrix);
                    localMatrix.Decompose(out filteredSkeleton.Nodes[filteredIndex].Transform.Scale, out filteredSkeleton.Nodes[filteredIndex].Transform.Rotation, out filteredSkeleton.Nodes[filteredIndex].Transform.Position);
                }
            }

            return filteredSkeleton;
        }

        private void AdjustSkeleton(Skeleton skeleton)
        {
            // Translate node with parent 0 using PivotPosition
            for (int i = 0; i < skeleton.Nodes.Length; ++i)
            {
                if (skeleton.Nodes[i].ParentIndex == 0)
                    skeleton.Nodes[i].Transform.Position -= PivotPosition;

                skeleton.Nodes[i].Transform.Position *= ScaleImport;
            }
        }
    }
}
