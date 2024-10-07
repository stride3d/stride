namespace Stride.Shaders.Spirv;


public record struct MixinGraphInstructions(MixinGraph Graph, bool Offsetted = false)
{

    public readonly  int Count
    {
        get
        {
            int count = 0;
            foreach (var e in this)
                count += 1;
            return count;
        }
    }
    public readonly MixinInstructionEnumerator GetEnumerator() => new(Graph, Offsetted);
}

/// <summary>
/// Representation of mixin parents to a graph.
/// </summary>
public class MixinGraph
{
    public ParentList Names { get; private set; }
    internal MixinList DistinctNames { get; private set; }

    public MixinGraphInstructions OffsettedInstructions => new(this,true);
    public MixinGraphInstructions Instructions => new(this);

    public int Count => GetCount();

    public MixinBuffer this[int index]
    {
        get
        {
            if(index >= Count)
                throw new IndexOutOfRangeException();
            var enumerator = GetEnumerator();
            for (int i = 0; enumerator.MoveNext() && i < index; i++){}
            return enumerator.Current;
        }
    }

    public MixinGraph()
    {
        Names = new();
        DistinctNames = new();
    }
    
    public MixinGraph(ParentList names)
    {
        Names = names;
        DistinctNames = new();
        RebuildGraph();
    }

    public MixinEnumerator GetEnumerator() => new(DistinctNames.AsList());

    public void Add(string mixin)
    {
        Names.Add(mixin);
        RebuildGraph();
    }
    
    public void RebuildGraph()
    {
        DistinctNames.Clear();
        foreach (var m in Names)
        {
            FillMixinHashSet(m);
        }
    }

    void FillMixinHashSet(string name)
    {
        if(MixinSourceProvider.TryGetMixinGraph(name, out var graph) && graph != null)
        {
            foreach (var m in graph)
                FillMixinHashSet(m.Name);
            DistinctNames.Add(name);
        }
    }
    int GetCount()
    {
        int count = 0;
        var e = GetEnumerator();
        while (e.MoveNext())
            count += 1;
        return count;
    }
}