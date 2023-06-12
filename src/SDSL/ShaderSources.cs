using System.Diagnostics.CodeAnalysis;
using SoftTouch.Spirv;

namespace SDSL;

public class ShaderSources
{
    public static ShaderSources Instance { get; } = new();

    Dictionary<string, Mixin> _cache;

    private ShaderSources()
    {
        _cache = new();
    }

    public static void Register(string name, Mixin mixin)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        Instance._cache.Add(name, mixin);
    }
    public static bool TryRegister(string name, Mixin mixin)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        return Instance._cache.TryAdd(name, mixin);
    }
    public static Mixin Get(string name)
    {
        return Instance._cache[name];
    }
    public static bool TryGet(string name, out Mixin? mixin)
    {
        return Instance._cache.TryGetValue(name, out mixin);
    }

}