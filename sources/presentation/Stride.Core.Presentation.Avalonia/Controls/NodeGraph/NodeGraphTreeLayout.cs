// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// A left to right tree layout of the nodes of a graph, in place of the GraphX simple tree layout of the WPF graph: the roots (the
/// nodes no link goes to) in the first column, each other node one column right of the node that first reaches it, and each node
/// centered on the nodes it reaches.
/// </summary>
internal static class NodeGraphTreeLayout
{
    /// <summary>
    /// Computes the positions of the nodes.
    /// </summary>
    /// <param name="nodes">The nodes, in their order in the graph; the roots and the children keep that order.</param>
    /// <param name="getSize">Gives the size of a node.</param>
    /// <param name="links">The links, from a source node to a target node, in their order in the graph.</param>
    /// <param name="origin">The position of the top left corner of the first root.</param>
    /// <param name="vertexGap">The vertical space between two nodes of a column.</param>
    /// <param name="layerGap">The horizontal space between two columns.</param>
    /// <returns>The position of the top left corner of each node.</returns>
    public static Dictionary<object, Point> Compute(IReadOnlyList<object> nodes, Func<object, Size> getSize, IEnumerable<(object Source, object Target)> links,
        Point origin, double vertexGap, double layerGap)
    {
        var nodeSet = new HashSet<object>(nodes);
        var outgoing = new Dictionary<object, List<object>>();
        var hasIncoming = new HashSet<object>();
        foreach (var (source, target) in links)
        {
            if (!nodeSet.Contains(source) || !nodeSet.Contains(target) || Equals(source, target))
                continue;

            if (!outgoing.TryGetValue(source, out var targets))
                outgoing.Add(source, targets = []);
            if (!targets.Contains(target))
                targets.Add(target);
            hasIncoming.Add(target);
        }

        // A spanning tree, breadth first, so that a node reached from several nodes goes in the column after the nearest one.
        // When only a cycle is left, its first node becomes a root.
        var children = new Dictionary<object, List<object>>();
        var depth = new Dictionary<object, int>();
        var roots = new List<object>();
        foreach (var candidate in nodes.Where(x => !hasIncoming.Contains(x)).Concat(nodes))
        {
            if (depth.ContainsKey(candidate))
                continue;

            roots.Add(candidate);
            depth.Add(candidate, 0);
            var queue = new Queue<object>();
            queue.Enqueue(candidate);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!outgoing.TryGetValue(node, out var targets))
                    continue;

                foreach (var target in targets)
                {
                    if (depth.ContainsKey(target))
                        continue;

                    depth.Add(target, depth[node] + 1);
                    if (!children.TryGetValue(node, out var nodeChildren))
                        children.Add(node, nodeChildren = []);
                    nodeChildren.Add(target);
                    queue.Enqueue(target);
                }
            }
        }

        // The columns: each as wide as its widest node
        var columnWidths = new List<double>();
        foreach (var node in nodes)
        {
            var column = depth[node];
            while (columnWidths.Count <= column)
                columnWidths.Add(0);
            columnWidths[column] = Math.Max(columnWidths[column], getSize(node).Width);
        }
        var columnLefts = new double[columnWidths.Count];
        var left = origin.X;
        for (var i = 0; i < columnWidths.Count; ++i)
        {
            columnLefts[i] = left;
            left += columnWidths[i] + layerGap;
        }

        // The rows: a node takes the height of the nodes it reaches, and is centered on them
        var subtreeHeights = new Dictionary<object, double>();
        foreach (var root in roots)
            ComputeSubtreeHeight(root);

        var positions = new Dictionary<object, Point>();
        var top = origin.Y;
        foreach (var root in roots)
        {
            Place(root, top);
            top += subtreeHeights[root] + vertexGap;
        }
        return positions;

        double ComputeSubtreeHeight(object node)
        {
            var childrenHeight = 0.0;
            if (children.TryGetValue(node, out var nodeChildren))
            {
                foreach (var child in nodeChildren)
                    childrenHeight += ComputeSubtreeHeight(child);
                childrenHeight += vertexGap * (nodeChildren.Count - 1);
            }
            var height = Math.Max(getSize(node).Height, childrenHeight);
            subtreeHeights[node] = height;
            return height;
        }

        double ChildrenHeight(List<object> nodeChildren)
            => nodeChildren.Sum(x => subtreeHeights[x]) + vertexGap * (nodeChildren.Count - 1);

        void Place(object node, double nodeTop)
        {
            var height = subtreeHeights[node];
            positions[node] = new Point(columnLefts[depth[node]], nodeTop + (height - getSize(node).Height) / 2);
            if (!children.TryGetValue(node, out var nodeChildren))
                return;

            var childTop = nodeTop + (height - ChildrenHeight(nodeChildren)) / 2;
            foreach (var child in nodeChildren)
            {
                Place(child, childTop);
                childTop += subtreeHeights[child] + vertexGap;
            }
        }
    }
}
