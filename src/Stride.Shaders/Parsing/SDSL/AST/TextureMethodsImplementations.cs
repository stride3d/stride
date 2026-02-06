using Stride.Shaders.Core;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL;

internal class TextureMethodsImplementations : TextureMethodsDeclarations
{
    public static TextureMethodsImplementations Instance { get; } = new();

    public override SpirvValue CompileLoad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue x, SpirvValue? o = null, SpirvValue? status = null, SpirvValue? s = null)
    {
        if (status != null)
            throw new NotImplementedException();

        var imageCoordType = context.ReverseTypes[x.TypeId];

        // We get all components except last one (LOD)
        var imageCoordSize = imageCoordType.GetElementCount();
        imageCoordType = imageCoordType.GetElementType().GetVectorOrScalar(imageCoordSize - 1);
        Span<int> shuffleIndices = stackalloc int[imageCoordSize - 1];
        for (int i = 0; i < shuffleIndices.Length; ++i)
            shuffleIndices[i] = i;

        // Note: assign LOD first because we truncate imageCoordValue right after
        // Extract LOD (last coordinate) as a separate value
        var lod = new SpirvValue(builder.InsertData(new OpCompositeExtract(context.GetOrRegister(ScalarType.Int), context.Bound++, x.Id, [imageCoordSize - 1])));
        // Remove last component (LOD) from texcoord 
        x = new(builder.InsertData(new OpVectorShuffle(context.GetOrRegister(imageCoordType), context.Bound++, x.Id, x.Id, new(shuffleIndices))));

        TextureGenerateImageOperands(lod, o, s, out var imask, out var imParams);
        var loadResult = builder.Insert(new OpImageFetch(context.GetOrRegister(functionType.ReturnType), context.Bound++, texture.Id, x.Id, imask, imParams));
        return new(loadResult.ResultId, loadResult.ResultType);
    }

    public override SpirvValue CompileSample(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();
        
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(null, o, null, out var imask, out var imParams);
        var sample = builder.Insert(new OpImageSampleImplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleLevel(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue lod, SpirvValue? o = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(lod, o, null, out var imask, out var imParams);
        var sample = builder.Insert(new OpImageSampleExplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleCmp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));
                    
        TextureGenerateImageOperands(null, o, null, out var imask, out var imParams); 
        var sample = builder.Insert(new OpImageSampleDrefImplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleCmpLevelZero(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));
                    
        TextureGenerateImageOperands(context.CompileConstant(0.0f), o, null, out var imask, out var imParams); 
        var sample = builder.Insert(new OpImageSampleDrefExplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }
    
    private void TextureGenerateImageOperands(SpirvValue? lod, SpirvValue? offset, SpirvValue? sampleIndex, out ImageOperandsMask imask, out EnumerantParameters imParams)
    {
        imask = ImageOperandsMask.None;
        // Allocate for worst case (3 operands)
        Span<int> operands = stackalloc int[3];
        int operandCount = 0;
        if (lod != null)
        {
            imask |= ImageOperandsMask.Lod;
            operands[operandCount++] = lod.Value.Id;
        }
        if (offset != null)
        {
            imask |= ImageOperandsMask.Offset;
            operands[operandCount++] = offset.Value.Id;
        }
        if (sampleIndex != null)
        {
            imask |= ImageOperandsMask.Sample;
            operands[operandCount++] = sampleIndex.Value.Id;
        }

        imParams = operandCount > 0 ? new EnumerantParameters(operands.Slice(0, operandCount)) : new EnumerantParameters();
    }
}