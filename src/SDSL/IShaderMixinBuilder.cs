using SDSL.Mixer;

namespace SDSL;

public interface IShaderMixinBuilder
{
    /// <summary>
    /// Generates a mixin.
    /// </summary>
    /// <param name="mixinTree">The mixin tree.</param>
    /// <param name="context">The context.</param>
    void Generate(ShaderMixinSource mixinTree, ShaderMixinContext context);
}