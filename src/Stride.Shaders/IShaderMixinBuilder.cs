using Stride.Shaders.Mixer;

namespace Stride.Shaders;

public interface IShaderMixinBuilder
{
    /// <summary>
    /// Generates a mixin.
    /// </summary>
    /// <param name="mixinTree">The mixin tree.</param>
    /// <param name="context">The context.</param>
    void Generate(ShaderMixinSource mixinTree, ShaderMixinContext context);
}