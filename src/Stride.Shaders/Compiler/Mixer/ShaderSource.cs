namespace Stride.Shaders.Mixer;
public abstract class ShaderSource
{
    public bool Discard { get; set; }

    public abstract void EnumerateMixins(SortedSet<ShaderSource> shaderSources);

    public abstract object Clone();

    public abstract override bool Equals(object against);

    public abstract override int GetHashCode();
}