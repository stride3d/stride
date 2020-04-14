// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Quantum.Tests.Helpers
{
    public static class AssetHierarchyHelper
    {
        public static string PrintHierarchy(AssetCompositeHierarchy<Types.MyPartDesign, Types.MyPart> asset)
        {
            var stack = new Stack<Tuple<Types.MyPartDesign, int>>();
            asset.Hierarchy.RootParts.Select(x => asset.Hierarchy.Parts[x.Id]).Reverse().ForEach(x => stack.Push(Tuple.Create(x, 0)));
            var sb = new StringBuilder();
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                sb.Append("".PadLeft(current.Item2 * 2));
                sb.AppendLine($"- {current.Item1.Part.Name} [{current.Item1.Part.Id}]");
                foreach (var child in asset.EnumerateChildPartDesigns(current.Item1, asset.Hierarchy, false).Reverse())
                {
                    stack.Push(Tuple.Create(child, current.Item2 + 1));
                }
            }
            var str = sb.ToString();
            return str;
        }

        public static Types.MyAssetHierarchyPropertyGraph BuildAssetAndGraph(int rootCount, int depth, int childPerPart, Action<AssetCompositeHierarchyData<Types.MyPartDesign, Types.MyPart>> initializeProperties = null)
        {
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var asset = BuildHierarchy(rootCount, depth, childPerPart);
            var assetItem = new AssetItem("MyAsset", asset);
            initializeProperties?.Invoke(asset.Hierarchy);
            var graph = (Types.MyAssetHierarchyPropertyGraph)AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
            return graph;
        }

        public static AssetTestContainer<Types.MyAssetHierarchy, Types.MyAssetHierarchyPropertyGraph> BuildAssetContainer(int rootCount, int depth, int childPerPart, AssetPropertyGraphContainer graphContainer = null, Action<AssetCompositeHierarchyData<Types.MyPartDesign, Types.MyPart>> initializeProperties = null)
        {
            graphContainer = graphContainer ?? new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var asset = BuildHierarchy(rootCount, depth, childPerPart);
            initializeProperties?.Invoke(asset.Hierarchy);
            var container = new AssetTestContainer<Types.MyAssetHierarchy, Types.MyAssetHierarchyPropertyGraph>(graphContainer, asset);
            container.BuildGraph();
            return container;
        }

        private static Types.MyAssetHierarchy BuildHierarchy(int rootCount, int depth, int childPerPart)
        {
            var asset = new Types.MyAssetHierarchy();
            var guid = 0;
            for (var i = 0; i < rootCount; ++i)
            {
                var rootPart = BuildPart(asset, $"Part{i + 1}", depth - 1, childPerPart, ref guid);
                asset.Hierarchy.RootParts.Add(rootPart.Part);
            }
            return asset;
        }

        private static Types.MyPartDesign BuildPart(Types.MyAssetHierarchy asset, string name, int depth, int childPerPart, ref int guidCount)
        {
            var part = new Types.MyPartDesign { Part = new Types.MyPart { Id = GuidGenerator.Get(++guidCount), Name = name } };
            asset.Hierarchy.Parts.Add(part);
            if (depth <= 0)
                return part;

            for (var i = 0; i < childPerPart; ++i)
            {
                var child = BuildPart(asset, name + $"-{i + 1}", depth - 1, childPerPart, ref guidCount);
                part.Part.AddChild(child.Part);
            }
            return part;
        }
    }
}
