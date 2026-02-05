using Stride.Shaders.Core;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL;

internal class Texture2DIntrinsicImplementations : Texture2DMethodsDeclarations
{
    public static Texture2DIntrinsicImplementations Instance { get; } = new();
    
    public override SpirvValue CompileSampleLevel(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue lod, SpirvValue? o = null, SpirvValue? status = null)
    {
        return base.CompileSampleLevel(context, builder, functionType, s, x, lod, o, status);
    }
}