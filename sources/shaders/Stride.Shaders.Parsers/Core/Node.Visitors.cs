using Stride.Shaders.Parsing;

namespace Stride.Shaders.Core;

public abstract partial class NodeVisitor
{
    protected virtual void VisitItemList<T>(List<T> list) where T : INodeItem
    {
        foreach (var item in list)
            VisitItem(item);
    }

    protected virtual void VisitNodeList<T>(List<T> list) where T : Node
    {
        foreach (var item in list)
            VisitNode(item);
    }

    public virtual void DefaultVisit(Node node)
    {
    }

    public void DefaultVisit<T>(T node) where T : struct, INodeItem
    {
    }

    public virtual void VisitNode(Node node)
    {
        node?.Accept(this);
    }

    public virtual void VisitItem<T>(T node) where T : INodeItem
    {
        node?.Accept(this);
    }
}

public partial class NodeWalker : NodeVisitor
{
}
