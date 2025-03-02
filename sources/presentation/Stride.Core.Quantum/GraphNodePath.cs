// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Diagnostics.Contracts;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum;

/// <summary>
/// A class describing the path of a node, relative to a root node. The path can cross references, array, etc.
/// </summary>
/// <remarks>This class is immutable.</remarks>
public sealed class GraphNodePath : IEnumerable<IGraphNode>, IEquatable<GraphNodePath>
{
    /// <summary>
    /// An enum that describes the type of an item of a model node path.
    /// </summary>
    public enum ElementType
    {
        /// <summary>
        /// This item is a member (child) of the previous node
        /// </summary>
        Member,
        /// <summary>
        /// This item is the target of the object reference of the previous node.
        /// </summary>
        Target,
        /// <summary>
        /// This item is the target of a enumerable reference of the previous node corresponding to the associated index.
        /// </summary>
        Index,
    }

    /// <summary>
    /// A structure that represents an element of the path.
    /// </summary>
    public readonly struct NodePathElement : IEquatable<NodePathElement>
    {
        public readonly ElementType Type;
        public readonly string? Name;
        public readonly NodeIndex Index;
        public readonly Guid Guid;

        private NodePathElement(string value)
        {
            Name = value;
            Index = NodeIndex.Empty;
            Guid = Guid.Empty;
            Type = ElementType.Member;
        }

        private NodePathElement(NodeIndex value)
        {
            Name = null;
            Index = value;
            Guid = Guid.Empty;
            Type = ElementType.Index;
        }

        private NodePathElement(Guid value)
        {
            Name = null;
            Index = NodeIndex.Empty;
            Guid = value;
            Type = ElementType.Target;
        }

        public static NodePathElement CreateMember(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            return new NodePathElement(name);
        }

        public static NodePathElement CreateTarget()
        {
            // We use a guid to allow equality test to fail between two different instances returned by CreateTarget
            return new NodePathElement(Guid.NewGuid());
        }

        public static NodePathElement CreateIndex(NodeIndex index)
        {
            return new NodePathElement(index);
        }

        public readonly bool EqualsInPath(NodePathElement other)
        {
            return (Type == ElementType.Target && other.Type == ElementType.Target) || Equals(other);
        }

        public readonly bool Equals(NodePathElement other)
        {
            return Type == other.Type && Equals(Guid, other.Guid) && Equals(Index, other.Index) && Equals(Name, other.Name);
        }

        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NodePathElement nodePathElement && Equals(nodePathElement);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Type, Name, Index, Guid);
        }

        public static bool operator ==(NodePathElement left, NodePathElement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NodePathElement left, NodePathElement right)
        {
            return !left.Equals(right);
        }

        public override readonly string ToString()
        {
            return Type switch
            {
                ElementType.Member => $".{Name}",
                ElementType.Target => "-> (Target)",
                ElementType.Index => $"[{Index}]",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }

    /// <summary>
    /// An enumerator for <see cref="GraphNodePath"/>
    /// </summary>
    private struct GraphNodePathEnumerator : IEnumerator<IGraphNode>
    {
        private readonly GraphNodePath path;
        private IGraphNode? current;
        private int index = -1;

        public GraphNodePathEnumerator(GraphNodePath path)
        {
            this.path = path;
        }

        public void Dispose()
        {
            index = -1;
        }

        public bool MoveNext()
        {
            if (index == path.path.Count)
                return false;

            if (index == -1)
            {
                current = path.RootNode;
            }
            else
            {
                var element = path.path[index];
                current = element.Type switch
                {
                    ElementType.Member => ((IObjectNode)Current)[element.Name!],
                    ElementType.Target => ((IMemberNode)Current).Target,
                    ElementType.Index => ((IObjectNode)Current).ItemReferences?[element.Index].TargetNode,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
            ++index;

            // If a node that is not the last one is null, we cannot process the path.
            if (current is null && index < path.path.Count)
                throw new InvalidOperationException("A node of the path is null but is not the last node.");

            return true;
        }

        public void Reset()
        {
            index = -1;
        }

        public readonly IGraphNode Current => current!;

        readonly object IEnumerator.Current => current!;
    }

    private const int DefaultCapacity = 16;
    private readonly List<NodePathElement> path;

    private GraphNodePath(IGraphNode rootNode, int defaultCapacity)
    {
        RootNode = rootNode;
        path = new List<NodePathElement>(defaultCapacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphNodePath"/> with the given root node.
    /// </summary>
    /// <param name="rootNode">The root node to represent with this instance of <see cref="GraphNodePath"/>.</param>
    public GraphNodePath(IGraphNode rootNode)
        : this(rootNode, DefaultCapacity)
    {
    }

    /// <summary>
    /// Gets the root node of this path.
    /// </summary>
    public IGraphNode RootNode { get; }

    /// <summary>
    /// Gets whether this path is empty.
    /// </summary>
    /// <remarks>An empty path resolves to <see cref="RootNode"/>.</remarks>
    public bool IsEmpty => path.Count == 0;

    /// <summary>
    /// Gets the number of items in this path.
    /// </summary>
    public int Count => path.Count;

    /// <summary>
    /// Gets the items composing this path.
    /// </summary>
    public IReadOnlyList<NodePathElement> Path => path;

    /// <inheritdoc/>
    public IEnumerator<IGraphNode> GetEnumerator()
    {
        return new GraphNodePathEnumerator(this);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(GraphNodePath? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!Equals(RootNode, other.RootNode) || path.Count != other.path.Count) return false;

        for (var i = 0; i < path.Count; ++i)
        {
            if (!path[i].EqualsInPath(other.path[i]))
                return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is GraphNodePath path && Equals(path);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Note: the only invariant is the root node.
        return RootNode?.GetHashCode() ?? 0;
    }

    public static bool operator ==(GraphNodePath left, GraphNodePath right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(GraphNodePath left, GraphNodePath right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "(root)" + (path.Count > 0 ? path.Select(x => x.ToString()).Aggregate((current, next) => current + next) : "");
    }

    /// <summary>
    /// Retrieves the node targeted by this path.
    /// </summary>
    /// <returns></returns>
    [Pure]
    public IGraphNode GetNode() => this.Last();

    /// <summary>
    /// Retrieves an accessor to the target of the path.
    /// </summary>
    /// <returns></returns>
    [Pure]
    public NodeAccessor GetAccessor()
    {
        if (path.Count == 0)
            return new NodeAccessor(RootNode, NodeIndex.Empty);

        var lastItem = path[^1];
        return lastItem.Type == ElementType.Index ? new NodeAccessor(GetParent()!.GetNode(), lastItem.Index) : new NodeAccessor(GetNode(), NodeIndex.Empty);
    }

    /// <summary>
    /// Retrieve the parent path.
    /// </summary>
    /// <returns>A new <see cref="GraphNodePath"/> instance representing the parent path.</returns>
    [Pure]
    public GraphNodePath? GetParent()
    {
        if (IsEmpty)
            return null;

        var result = new GraphNodePath(RootNode, path.Count - 1);
        for (var i = 0; i < path.Count - 1; ++i)
            result.path.Add(path[i]);
        return result;
    }

    /// <summary>
    /// Clones this instance of <see cref="GraphNodePath"/> and remap the new instance to a new root node.
    /// </summary>
    /// <param name="newRoot">The root node for the cloned path.</param>
    /// <returns>A copy of this path with the given node as root node.</returns>
    [Pure]
    public GraphNodePath Clone(IGraphNode newRoot)
    {
        var clone = new GraphNodePath(newRoot, Math.Max(path.Count, DefaultCapacity));
        clone.path.AddRange(path);
        return clone;
    }

    /// <summary>
    /// Clones this instance of <see cref="GraphNodePath"/>.
    /// </summary>
    /// <returns>A copy of this path with the same root node.</returns>
    [Pure]
    public GraphNodePath Clone() => Clone(RootNode);

    public void PushMember(string memberName) => PushElement(memberName, ElementType.Member);

    public void PushTarget() => PushElement(null, ElementType.Target);

    public void PushIndex(NodeIndex index) => PushElement(index, ElementType.Index);

    public void Pop() => path.RemoveAt(path.Count - 1);

    // TODO: Switch to tuple return as soon as we have C# 7.0
    public static GraphNodePath From(IGraphNode root, MemberPath memberPath, out NodeIndex index)
    {
        ArgumentNullException.ThrowIfNull(memberPath);

        var result = new GraphNodePath(root);
        index = NodeIndex.Empty;
        var memberPathItems = memberPath.Decompose();
        for (int i = 0; i < memberPathItems.Count; i++)
        {
            var memberPathItem = memberPathItems[i];
            bool lastItem = i == memberPathItems.Count - 1;
            if (memberPathItem.MemberDescriptor != null)
            {
                result.PushMember(memberPathItem.MemberDescriptor.Name);
            }
            else if (memberPathItem.GetIndex() != null)
            {
                var localIndex = new NodeIndex(memberPathItem.GetIndex());

                if (lastItem)
                {
                    // If last item, we directly return the index rather than add it to the path
                    index = localIndex;
                }
                else
                {
                    result.PushIndex(localIndex);
                }
            }

            // Don't apply Target on last item
            if (!lastItem)
            {
                // If this is a reference, add a target element to the path
                var node = result.GetNode();
                var objectReference = (node as IMemberNode)?.TargetReference;
                if (objectReference?.TargetNode != null)
                {
                    result.PushTarget();
                }
            }
        }

        return result;
    }

    [Pure]
    public MemberPath ToMemberPath()
    {
        var memberPath = new MemberPath();
        var node = RootNode;
        for (var i = 0; i < path.Count; i++)
        {
            var itemPath = path[i];
            switch (itemPath.Type)
            {
                case ElementType.Member:
                    var name = itemPath.Name!;
                    node = ((IObjectNode)node!)[name];
                    memberPath.Push(((MemberNode)node).MemberDescriptor);
                    break;
                case ElementType.Target:
                    if (i != path.Count - 1)
                    {
                        node = ((IMemberNode)node!).Target;
                    }
                    break;
                case ElementType.Index:
                    var index = itemPath.Index;
                    var enumerableReference = ((IObjectNode)node!).ItemReferences;
                    var descriptor = node.Descriptor;
                    if (descriptor is CollectionDescriptor collectionDescriptor)
                    {
                        memberPath.Push(collectionDescriptor, index.Value);
                    }
                    else if (descriptor is DictionaryDescriptor dictionaryDescriptor)
                    {
                        memberPath.Push(dictionaryDescriptor, index.Value);
                    }

                    if (i != path.Count - 1)
                    {
                        var objectRefererence = enumerableReference!.Single(x => Equals(x.Index, index));
                        node = objectRefererence.TargetNode;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return memberPath;
    }

    private void PushElement(object? elementValue, ElementType type)
    {
        switch (type)
        {
            case ElementType.Member:
                if (elementValue is not string name)
                    throw new ArgumentException("The value must be a non-null string when type is ElementType.Member.");
                path.Add(NodePathElement.CreateMember(name));
                break;
            case ElementType.Target:
                if (elementValue != null)
                    throw new ArgumentException("The value must be null when type is ElementType.Target.");
                path.Add(NodePathElement.CreateTarget());
                break;
            case ElementType.Index:
                if (!(elementValue is NodeIndex))
                    throw new ArgumentException("The value must be an Index when type is ElementType.Index.");
                path.Add(NodePathElement.CreateIndex((NodeIndex)elementValue));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
