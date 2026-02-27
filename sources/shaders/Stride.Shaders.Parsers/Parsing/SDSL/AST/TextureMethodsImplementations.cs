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

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var imageCoordType = context.ReverseTypes[x.TypeId];
        var imageCoordSize = imageCoordType.GetElementCount();

        // Determine the texture's natural coordinate dimension
        var textureDim = textureType switch
        {
            Texture1DType => 1,
            Texture2DType => 2,
            Texture3DType or TextureCubeType => 3,
            _ => throw new NotImplementedException($"Unsupported texture type {textureType}")
        };
        if (textureType.Arrayed)
            textureDim++;

        if (imageCoordSize > textureDim)
        {
            // Coord has extra component (LOD): extract it and strip from coord
            var coordType = imageCoordType.GetElementType().GetVectorOrScalar(imageCoordSize - 1);
            Span<int> shuffleIndices = stackalloc int[imageCoordSize - 1];
            for (int i = 0; i < shuffleIndices.Length; ++i)
                shuffleIndices[i] = i;

            var lod = new SpirvValue(builder.InsertData(new OpCompositeExtract(context.GetOrRegister(ScalarType.Int), context.Bound++, x.Id, [imageCoordSize - 1])));
            x = new(builder.InsertData(new OpVectorShuffle(context.GetOrRegister(coordType), context.Bound++, x.Id, x.Id, new(shuffleIndices))));

            TextureGenerateImageOperands(lod, o, s, out var imask, out var imParams);
            var loadResult = builder.Insert(new OpImageFetch(context.GetOrRegister(functionType.ReturnType), context.Bound++, texture.Id, x.Id, imask, imParams));
            return new(loadResult.ResultId, loadResult.ResultType);
        }
        else
        {
            // No LOD component (e.g. RWTexture): use coord directly
            TextureGenerateImageOperands(null, o, s, out var imask, out var imParams);
            var loadResult = builder.Insert(new OpImageFetch(context.GetOrRegister(functionType.ReturnType), context.Bound++, texture.Id, x.Id, imask, imParams));
            return new(loadResult.ResultId, loadResult.ResultType);
        }
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

    public override SpirvValue CompileGetDimensions(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue? x = null, SpirvValue? width = null, SpirvValue? levels = null, SpirvValue? elements = null, SpirvValue? height = null, SpirvValue? samples = null, SpirvValue? depth = null)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var uintType = context.GetOrRegister(ScalarType.UInt);

        // Determine the number of size components returned by image size query
        // SPIR-V returns: scalar for 1D, vec2 for 2D/Cube, vec3 for 3D
        // Add 1 if arrayed (array layers as last component)
        int sizeComponents = textureType switch
        {
            Texture1DType { Arrayed: false } => 1,
            Texture1DType { Arrayed: true } => 2,
            Texture2DType { Arrayed: false } => 2,
            Texture2DType { Arrayed: true } => 3,
            Texture3DType => 3,
            TextureCubeType { Arrayed: false } => 2,
            TextureCubeType { Arrayed: true } => 3,
            _ => throw new NotImplementedException($"GetDimensions not supported for texture type {textureType}")
        };

        var sizeResultType = sizeComponents == 1 ? uintType : context.GetOrRegister(new VectorType(ScalarType.UInt, sizeComponents));

        // Query image size
        int sizeResultId;
        if (x != null)
        {
            // LOD provided — use OpImageQuerySizeLod
            sizeResultId = builder.Insert(new OpImageQuerySizeLod(sizeResultType, context.Bound++, texture.Id, x.Value.Id)).ResultId;
        }
        else if (textureType.Multisampled || textureType.Sampled == 2)
        {
            // Multisampled or RW texture — use OpImageQuerySize (no LOD)
            sizeResultId = builder.Insert(new OpImageQuerySize(sizeResultType, context.Bound++, texture.Id)).ResultId;
        }
        else
        {
            // Regular sampled texture without explicit LOD — query at LOD 0
            var lod0 = context.CompileConstant((uint)0);
            sizeResultId = builder.Insert(new OpImageQuerySizeLod(sizeResultType, context.Bound++, texture.Id, lod0.Id)).ResultId;
        }

        // Map size components to out parameters based on texture type
        // Component order from SPIR-V: [width, height?, depth_or_elements?]
        int componentIdx = 0;

        // Component 0: width (always present)
        if (width != null)
            StoreQueryComponent(context, builder, uintType, sizeResultId, sizeComponents, componentIdx, width.Value);
        componentIdx++;

        // Component 1 varies by texture type
        if (componentIdx < sizeComponents)
        {
            if (textureType is Texture1DType)
            {
                // For 1DArray: component 1 is array elements
                if (elements != null)
                    StoreQueryComponent(context, builder, uintType, sizeResultId, sizeComponents, componentIdx, elements.Value);
            }
            else
            {
                // For 2D/3D/Cube: component 1 is height
                if (height != null)
                    StoreQueryComponent(context, builder, uintType, sizeResultId, sizeComponents, componentIdx, height.Value);
            }
            componentIdx++;
        }

        // Component 2: depth (3D) or elements (arrayed 2D/Cube)
        if (componentIdx < sizeComponents)
        {
            if (textureType is Texture3DType)
            {
                if (depth != null)
                    StoreQueryComponent(context, builder, uintType, sizeResultId, sizeComponents, componentIdx, depth.Value);
            }
            else if (textureType.Arrayed)
            {
                if (elements != null)
                    StoreQueryComponent(context, builder, uintType, sizeResultId, sizeComponents, componentIdx, elements.Value);
            }
        }

        // Query mip levels if requested
        if (levels != null)
        {
            var levelsResult = builder.Insert(new OpImageQueryLevels(uintType, context.Bound++, texture.Id));
            StoreConvertedValue(context, builder, uintType, levelsResult.ResultId, levels.Value);
        }

        // Query sample count if requested
        if (samples != null)
        {
            var samplesResult = builder.Insert(new OpImageQuerySamples(uintType, context.Bound++, texture.Id));
            StoreConvertedValue(context, builder, uintType, samplesResult.ResultId, samples.Value);
        }

        return default;
    }

    private static void StoreQueryComponent(SpirvContext context, SpirvBuilder builder, int uintTypeId, int sizeResultId, int sizeComponents, int componentIndex, SpirvValue outParam)
    {
        int valueId;
        if (sizeComponents == 1)
        {
            valueId = sizeResultId;
        }
        else
        {
            valueId = builder.Insert(new OpCompositeExtract(uintTypeId, context.Bound++, sizeResultId, [componentIndex])).ResultId;
        }

        StoreConvertedValue(context, builder, uintTypeId, valueId, outParam);
    }

    private static void StoreConvertedValue(SpirvContext context, SpirvBuilder builder, int uintTypeId, int valueId, SpirvValue outParam)
    {
        var ptrType = (PointerType)context.ReverseTypes[outParam.TypeId];
        var targetType = ptrType.BaseType;

        if (targetType is ScalarType { Type: Scalar.Float })
        {
            // Convert uint to float
            var floatTypeId = context.GetOrRegister(targetType);
            var converted = builder.Insert(new OpConvertUToF(floatTypeId, context.Bound++, valueId));
            builder.Insert(new OpStore(outParam.Id, converted.ResultId, null, []));
        }
        else
        {
            // uint — store directly
            builder.Insert(new OpStore(outParam.Id, valueId, null, []));
        }
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
