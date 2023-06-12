using SoftTouch.Spirv;

namespace SDSL.Mixer;

public partial struct Mixer
{
    public Mixin Shader { get; private set; }
    public Mixer()
    {
        Shader = new();
    }
    public Mixin Get() => Shader;

    public Mixer With(Mixin mixin)
    {
        Shader.Mixins.Add(mixin);
        return this;
    }
}