namespace Stride.Shaders.Core;

public abstract partial class TypeVisitor
{
    protected virtual void VisitNodeList<T>(List<T> list) where T : ISymbolTypeNode
    {
        foreach (var item in list)
            VisitNode(item);
    }

    protected virtual void VisitTypeList<T>(List<T> list) where T : ShaderSymbol
    {
        foreach (var item in list)
            VisitType(item);
    }

    public virtual void DefaultVisit(SymbolType type)
    {
    }

    public void DefaultVisit<T>(T node) where T : struct, ISymbolTypeNode
    {
    }

    public virtual void VisitType(SymbolType type)
    {
        type?.Accept(this);
    }

    public virtual void VisitNode<T>(T node) where T : ISymbolTypeNode
    {
        node?.Accept(this);
    }
}

public partial class TypeWalker : TypeVisitor
{
}

public abstract partial class TypeVisitor<TResult>
{
    public virtual TResult DefaultVisit(SymbolType node)
    {
        return default;
    }

    public virtual bool DefaultVisit<T>(ref T node) where T : struct, ISymbolTypeNode
    {
        return true;
    }

    public virtual TResult VisitType(SymbolType type)
    {
        return type.Accept(this);
    }

    public virtual bool VisitNode<T>(ref T node) where T : struct, ISymbolTypeNode
    {
        return node.Accept(this);
    }

}

public abstract partial class TypeRewriter : TypeVisitor<SymbolType?>
{
    protected TypeRewriter()
    {
    }

    public override SymbolType DefaultVisit(SymbolType node)
    {
        return node;
    }

    protected List<T> VisitTypeList<T>(List<T> list) where T : SymbolType
    {
        List<T>? newList = null;
        for (int i = 0; i < list.Count; ++i)
        {
            var previousValue = list[i];
            var temp = VisitType(previousValue);

            // First time change?
            if (!ReferenceEquals(previousValue, temp) && newList == null)
                newList = [.. list.Slice(0, i)];

            if (newList != null)
            {
                if (temp != null)
                    newList.Add((T)temp);
            }
        }

        return newList ?? list;
    }

    protected List<T> VisitNodeList<T>(List<T> list) where T : struct, ISymbolTypeNode
    {
        var equalityComparer = EqualityComparer<T>.Default;

        List<T>? newList = null;
        for (int i = 0; i < list.Count; ++i)
        {
            var value = list[i];
            var keep = VisitNode(ref value);

            // First time change?
            if ((!keep || !equalityComparer.Equals(value, list[i])) && newList == null)
                newList = [.. list.Slice(0, i)];

            if (newList != null)
            {
                if (keep)
                    newList.Add(value);
            }
        }

        return newList ?? list;
    }
}