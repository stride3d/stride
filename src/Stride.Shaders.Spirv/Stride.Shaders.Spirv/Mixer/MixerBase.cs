using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv;

/// <summary>
/// Mixer base class
/// </summary>
public abstract class MixerBase
{
    public MixinGraph Mixins {get; protected set;}
    public MultiBuffer Buffer {get; protected set;}

    protected Action DisposeBuffers;

    public string Name { get; init; }
    

    public MixerBase(string name)
    {
        Name = name;
        Buffer = new();
        Buffer.AddOpSDSLMixinName(Name);
        Mixins = new();
        DisposeBuffers = Buffer.Dispose;
    }

    public virtual MixinBuffer Build()
    {
        Buffer.AddOpSDSLMixinEnd();
        // TODO : do some validation here
        MixinSourceProvider.Register(new(Name, Buffer));
        DisposeBuffers.Invoke();
        return MixinSourceProvider.Get(Name);
    }
}