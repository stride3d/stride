using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core.Buffers;


namespace Stride.Shaders.Spirv;




public record struct Mixin(string Name, SpirvBuffer Buffer);


public class MixinStorage
{
    public static MixinStorage Instance { get; } = new();
    Dictionary<string, Mixin> Storage { get; } = [];

    private MixinStorage(){}

    public static void RegisterOrUpdate(string name, SpirvBuffer buffer)
    {
        Instance.Storage[name] = new(name, buffer);
    }

    public static bool TryRegister(string name, SpirvBuffer buffer)
    {
        return Instance.Storage.TryAdd(name, new(name, buffer));
    }

    public static Mixin Get(string name)
    {
        return Instance.Storage[name];
    }
    
    public static bool TryGet(string name, out Mixin mixin)
    {
        return Instance.Storage.TryGetValue(name, out mixin);
    }
}