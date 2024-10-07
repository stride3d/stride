using System.Collections;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv;

/// <summary>
/// Collections of mixins
/// </summary>
internal struct MixinList : IList<string>
{
    List<string> mixins;

    public MixinList()
    {
        mixins = new();
    }

    public string this[int index] { get => mixins[index]; set => throw new Exception();}

    public int Count => mixins.Count;

    public bool IsReadOnly => false;

    public void Add(string mixin)
    {
        if (!mixins.Contains(mixin))
            mixins.Add(mixin);
    }

    public void Clear()
    {
        mixins.Clear();
    }

    public bool Contains(string item)
    {
        return mixins.Contains(item);
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        mixins.CopyTo(array, arrayIndex);
    }

    public IEnumerator<string> GetEnumerator()
    {
        return mixins.GetEnumerator();
    }

    public int IndexOf(string item)
    {
        return mixins.IndexOf(item);
    }

    public void Insert(int index, string item)
    {
        mixins.Insert(index,item);
    }

    public bool Remove(string item)
    {
        return mixins.Remove(item);
    }

    public void RemoveAt(int index)
    {
        mixins.RemoveAt(index);
    }

    public List<string> AsList() => mixins;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public ref struct MixinEnumerator
{
    List<string> mixinNames;
    List<string>.Enumerator enumerator;

    public MixinEnumerator(List<string> names)
    {
        mixinNames = names;
        enumerator = mixinNames.GetEnumerator();
    }

    public MixinBuffer Current => MixinSourceProvider.Get(enumerator.Current);

    public bool MoveNext() => enumerator.MoveNext();
}

