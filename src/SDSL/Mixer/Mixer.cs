using SoftTouch.Spirv;

namespace SDSL.Mixer;

public class Mixer
{
    public Mixin Shader { get; private set; }
    public Mixer()
    {
        Shader = new();
    }
    public Mixin Build() => Shader;

    public Mixer With(Mixin mixin)
    {
        Shader.Mixins.Add(mixin);
        return this;
    }
}