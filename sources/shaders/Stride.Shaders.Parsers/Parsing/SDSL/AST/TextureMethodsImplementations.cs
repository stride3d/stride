using Stride.Shaders.Core;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL;

internal class TextureMethodsImplementations : TextureMethodsDeclarations
{
    public static TextureMethodsImplementations Instance { get; } = new();

    /// <summary>
    /// SPIR-V requires OpImageSample*/OpImageFetch result types to be a 4-component vector.
    /// This method returns the vec4 type for sampling, and if the actual return type is smaller,
    /// extracts the needed components from the vec4 result.
    /// </summary>
    private static (int Vec4TypeId, bool NeedsExtract) GetImageSampleResultType(SpirvContext context, FunctionType functionType, TextureType textureType)
    {
        var returnType = functionType.ReturnType;
        var scalarType = returnType.GetElementType();
        var vec4Type = new VectorType((ScalarType)scalarType, 4);
        var vec4TypeId = context.GetOrRegister(vec4Type);
        var needsExtract = returnType.GetElementCount() < 4;
        return (vec4TypeId, needsExtract);
    }

    private static SpirvValue ExtractFromVec4(SpirvContext context, SpirvBuilder builder, FunctionType functionType, int vec4ResultId)
    {
        var returnType = functionType.ReturnType;
        var elementCount = returnType.GetElementCount();
        var returnTypeId = context.GetOrRegister(returnType);

        if (elementCount == 1)
        {
            // Scalar: extract component 0
            var extract = builder.InsertData(new OpCompositeExtract(returnTypeId, context.Bound++, vec4ResultId, [0]));
            return new(extract);
        }
        else
        {
            // vec2 or vec3: shuffle from vec4
            Span<int> indices = stackalloc int[elementCount];
            for (int i = 0; i < elementCount; i++)
                indices[i] = i;
            var shuffle = builder.InsertData(new OpVectorShuffle(returnTypeId, context.Bound++, vec4ResultId, vec4ResultId, new(indices)));
            return new(shuffle);
        }
    }

    public override SpirvValue CompileLoad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue x, SpirvValue? o = null, SpirvValue? status = null, SpirvValue? s = null)
    {
        if (status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var imageCoordType = context.ReverseTypes[x.TypeId];
        var imageCoordSize = imageCoordType.GetElementCount();

        var textureDim = textureType.CoordinateDimension;

        var (vec4TypeId, needsExtract) = GetImageSampleResultType(context, functionType, textureType);

        // Extract LOD from coords if present (sampled images only, not storage)
        SpirvValue? lod = null;
        if (textureType.Sampled != 2 && imageCoordSize > textureDim)
        {
            var coordType = imageCoordType.GetElementType().GetVectorOrScalar(imageCoordSize - 1);
            Span<int> shuffleIndices = stackalloc int[imageCoordSize - 1];
            for (int i = 0; i < shuffleIndices.Length; ++i)
                shuffleIndices[i] = i;

            lod = new SpirvValue(builder.InsertData(new OpCompositeExtract(context.GetOrRegister(ScalarType.Int), context.Bound++, x.Id, [imageCoordSize - 1])));
            x = new(builder.InsertData(new OpVectorShuffle(context.GetOrRegister(coordType), context.Bound++, x.Id, x.Id, new(shuffleIndices))));
        }

        TextureGenerateImageOperands(context, builder, lod, o, s, out var imask, out var imParams);

        // Storage images (RWTexture, Sampled=2) use OpImageRead; sampled images use OpImageFetch
        int loadResultId;
        if (textureType.Sampled == 2)
            loadResultId = builder.Insert(new OpImageRead(vec4TypeId, context.Bound++, texture.Id, x.Id, imask, imParams)).ResultId;
        else
            loadResultId = builder.Insert(new OpImageFetch(vec4TypeId, context.Bound++, texture.Id, x.Id, imask, imParams)).ResultId;

        if (needsExtract) return ExtractFromVec4(context, builder, functionType, loadResultId);
        return new(loadResultId, vec4TypeId);
    }

    public override SpirvValue CompileSample(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var (vec4TypeId, needsExtract) = GetImageSampleResultType(context, functionType, textureType);

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams);
        var sample = builder.Insert(new OpImageSampleImplicitLod(vec4TypeId, context.Bound++, sampledImage.ResultId, x.Id, imask, imParams));

        if (needsExtract) return ExtractFromVec4(context, builder, functionType, sample.ResultId);
        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleBias(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue bias, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var (vec4TypeId, needsExtract) = GetImageSampleResultType(context, functionType, textureType);

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams, bias: bias);
        var sample = builder.Insert(new OpImageSampleImplicitLod(vec4TypeId, context.Bound++, sampledImage.ResultId, x.Id, imask, imParams));

        if (needsExtract) return ExtractFromVec4(context, builder, functionType, sample.ResultId);
        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleLevel(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue lod, SpirvValue? o = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var (vec4TypeId, needsExtract) = GetImageSampleResultType(context, functionType, textureType);

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, lod, o, null, out var imask, out var imParams);
        var sample = builder.Insert(new OpImageSampleExplicitLod(vec4TypeId, context.Bound++, sampledImage.ResultId, x.Id, imask, imParams));

        if (needsExtract) return ExtractFromVec4(context, builder, functionType, sample.ResultId);
        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleGrad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var (vec4TypeId, needsExtract) = GetImageSampleResultType(context, functionType, textureType);

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams, ddx, ddy);
        var sample = builder.Insert(new OpImageSampleExplicitLod(vec4TypeId, context.Bound++, sampledImage.ResultId, x.Id, imask, imParams));

        if (needsExtract) return ExtractFromVec4(context, builder, functionType, sample.ResultId);
        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleCmp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams);
        var sample = builder.Insert(new OpImageSampleDrefImplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleCmpBias(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue bias, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams, bias: bias);
        var sample = builder.Insert(new OpImageSampleDrefImplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleCmpGrad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue ddx, SpirvValue ddy, SpirvValue? o = null, SpirvValue? clamp = null, SpirvValue? status = null)
    {
        if (clamp != null || status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams, ddx, ddy);
        var sample = builder.Insert(new OpImageSampleDrefExplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleCmpLevel(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? compareValue = null, SpirvValue? lod = null, SpirvValue? o = null, SpirvValue? status = null, SpirvValue? c = null)
    {
        if (status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, lod, o, null, out var imask, out var imParams);
        var sample = builder.Insert(new OpImageSampleDrefExplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue!.Value.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileSampleCmpLevelZero(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();

        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, context.CompileConstant(0.0f), o, null, out var imask, out var imParams);
        var sample = builder.Insert(new OpImageSampleDrefExplicitLod(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, imask, imParams));

        return new(sample.ResultId, sample.ResultType);
    }

    public override SpirvValue CompileCalculateLevelOfDetail(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        var float2Type = context.GetOrRegister(new VectorType(ScalarType.Float, 2));
        var queryResult = builder.Insert(new OpImageQueryLod(float2Type, context.Bound++, sampledImage.ResultId, x.Id));

        // Component 0 = selected (clamped) mip level
        var floatType = context.GetOrRegister(ScalarType.Float);
        var result = builder.Insert(new OpCompositeExtract(floatType, context.Bound++, queryResult.ResultId, [0]));
        return new(result.ResultId, result.ResultType);
    }

    public override SpirvValue CompileCalculateLevelOfDetailUnclamped(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];

        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        var float2Type = context.GetOrRegister(new VectorType(ScalarType.Float, 2));
        var queryResult = builder.Insert(new OpImageQueryLod(float2Type, context.Bound++, sampledImage.ResultId, x.Id));

        // Component 1 = unclamped LOD
        var floatType = context.GetOrRegister(ScalarType.Float);
        var result = builder.Insert(new OpCompositeExtract(floatType, context.Bound++, queryResult.ResultId, [1]));
        return new(result.ResultId, result.ResultType);
    }

    // Gather: component 0 (same as GatherRed)
    public override SpirvValue CompileGather(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? o = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        return CompileGatherComponent(context, builder, functionType, texture, s, x, 0, o);
    }

    public override SpirvValue CompileGatherRed(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherComponentConstOffsets(context, builder, functionType, texture, s, x, 0, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherComponent(context, builder, functionType, texture, s, x, 0, o);
    }

    public override SpirvValue CompileGatherGreen(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherComponentConstOffsets(context, builder, functionType, texture, s, x, 1, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherComponent(context, builder, functionType, texture, s, x, 1, o);
    }

    public override SpirvValue CompileGatherBlue(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherComponentConstOffsets(context, builder, functionType, texture, s, x, 2, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherComponent(context, builder, functionType, texture, s, x, 2, o);
    }

    public override SpirvValue CompileGatherAlpha(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherComponentConstOffsets(context, builder, functionType, texture, s, x, 3, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherComponent(context, builder, functionType, texture, s, x, 3, o);
    }

    public override SpirvValue CompileGatherCmp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        return CompileGatherDref(context, builder, functionType, texture, s, x, compareValue, o);
    }

    public override SpirvValue CompileGatherCmpRed(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherDrefConstOffsets(context, builder, functionType, texture, s, x, compareValue, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherDref(context, builder, functionType, texture, s, x, compareValue, o);
    }

    public override SpirvValue CompileGatherCmpGreen(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherDrefConstOffsets(context, builder, functionType, texture, s, x, compareValue, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherDref(context, builder, functionType, texture, s, x, compareValue, o);
    }

    public override SpirvValue CompileGatherCmpBlue(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherDrefConstOffsets(context, builder, functionType, texture, s, x, compareValue, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherDref(context, builder, functionType, texture, s, x, compareValue, o);
    }

    public override SpirvValue CompileGatherCmpAlpha(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o = null, SpirvValue? o1 = null, SpirvValue? o2 = null, SpirvValue? o3 = null, SpirvValue? o4 = null, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException();
        if (o1 != null)
            return CompileGatherDrefConstOffsets(context, builder, functionType, texture, s, x, compareValue, o1.Value, o2!.Value, o3!.Value, o4!.Value);
        return CompileGatherDref(context, builder, functionType, texture, s, x, compareValue, o);
    }

    public override SpirvValue CompileGetDimensions(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue? x = null, SpirvValue? width = null, SpirvValue? levels = null, SpirvValue? elements = null, SpirvValue? height = null, SpirvValue? samples = null, SpirvValue? depth = null)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var uintType = context.GetOrRegister(ScalarType.UInt);

        int sizeComponents = textureType.SizeQueryDimension;

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

    private SpirvValue CompileGatherComponent(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, uint component, SpirvValue? o)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        var componentConstant = context.CompileConstant(component);
        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams);
        var gather = builder.Insert(new OpImageGather(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, componentConstant.Id, imask, imParams));
        return new(gather.ResultId, gather.ResultType);
    }

    private SpirvValue CompileGatherComponentConstOffsets(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, uint component, SpirvValue o1, SpirvValue o2, SpirvValue o3, SpirvValue o4)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var componentConstant = context.CompileConstant(component);
        var resultTypeId = context.GetOrRegister(functionType.ReturnType);

        // Try to promote all 4 offsets to constants (handles inline int2(x,y) constructors)
        var co1 = TryPromoteToConstant(context, builder, o1.Id);
        var co2 = TryPromoteToConstant(context, builder, o2.Id);
        var co3 = TryPromoteToConstant(context, builder, o3.Id);
        var co4 = TryPromoteToConstant(context, builder, o4.Id);
        if (co1 >= 0 && co2 >= 0 && co3 >= 0 && co4 >= 0)
        {
            var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));
            var int2Type = new VectorType(ScalarType.Int, 2);
            var arrayType = context.GetOrRegister(new ArrayType(int2Type, 4));
            var constOffsetsId = context.Bound++;
            context.AddData(new OpConstantComposite(arrayType, constOffsetsId, [co1, co2, co3, co4]));

            Span<int> operands = [constOffsetsId];
            var gather = builder.Insert(new OpImageGather(resultTypeId, context.Bound++, sampledImage.ResultId, x.Id, componentConstant.Id, ImageOperandsMask.ConstOffsets, new EnumerantParameters(operands)));
            return new(gather.ResultId, gather.ResultType);
        }

        // Fallback: 4 separate gathers with Offset, extract component 3 from each
        var float4TypeId = resultTypeId;
        var floatTypeId = context.GetOrRegister(ScalarType.Float);
        var int3 = context.CompileConstant(3).Id;
        Span<int> components = stackalloc int[4];
        foreach (var (offset, i) in new[] { (o1, 0), (o2, 1), (o3, 2), (o4, 3) })
        {
            var si = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));
            Span<int> ops = [offset.Id];
            var gather = builder.Insert(new OpImageGather(float4TypeId, context.Bound++, si.ResultId, x.Id, componentConstant.Id, ImageOperandsMask.Offset, new EnumerantParameters(ops)));
            // Extract component 3: the texel at the exact offset location
            components[i] = builder.Insert(new OpCompositeExtract(floatTypeId, context.Bound++, gather.ResultId, [3])).ResultId;
        }
        var result = builder.Insert(new OpCompositeConstruct(float4TypeId, context.Bound++, [components[0], components[1], components[2], components[3]]));
        return new(result.ResultId, result.ResultType);
    }

    private SpirvValue CompileGatherDref(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue? o)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));

        TextureGenerateImageOperands(context, builder, null, o, null, out var imask, out var imParams);
        var gather = builder.Insert(new OpImageDrefGather(context.GetOrRegister(functionType.ReturnType), context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, imask, imParams));
        return new(gather.ResultId, gather.ResultType);
    }

    private SpirvValue CompileGatherDrefConstOffsets(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue texture, SpirvValue s, SpirvValue x, SpirvValue compareValue, SpirvValue o1, SpirvValue o2, SpirvValue o3, SpirvValue o4)
    {
        var textureType = (TextureType)context.ReverseTypes[texture.TypeId];
        var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
        var resultTypeId = context.GetOrRegister(functionType.ReturnType);

        // Try to promote all 4 offsets to constants (handles inline int2(x,y) constructors)
        var co1 = TryPromoteToConstant(context, builder, o1.Id);
        var co2 = TryPromoteToConstant(context, builder, o2.Id);
        var co3 = TryPromoteToConstant(context, builder, o3.Id);
        var co4 = TryPromoteToConstant(context, builder, o4.Id);
        if (co1 >= 0 && co2 >= 0 && co3 >= 0 && co4 >= 0)
        {
            var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));
            var int2Type = new VectorType(ScalarType.Int, 2);
            var arrayType = context.GetOrRegister(new ArrayType(int2Type, 4));
            var constOffsetsId = context.Bound++;
            context.AddData(new OpConstantComposite(arrayType, constOffsetsId, [co1, co2, co3, co4]));

            Span<int> operands = [constOffsetsId];
            var gather = builder.Insert(new OpImageDrefGather(resultTypeId, context.Bound++, sampledImage.ResultId, x.Id, compareValue.Id, ImageOperandsMask.ConstOffsets, new EnumerantParameters(operands)));
            return new(gather.ResultId, gather.ResultType);
        }

        // Fallback: 4 separate gathers with Offset, extract component 3 from each
        var float4TypeId = resultTypeId;
        var floatTypeId = context.GetOrRegister(ScalarType.Float);
        Span<int> components = stackalloc int[4];
        foreach (var (offset, i) in new[] { (o1, 0), (o2, 1), (o3, 2), (o4, 3) })
        {
            var si = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, texture.Id, s.Id));
            Span<int> ops = [offset.Id];
            var gather = builder.Insert(new OpImageDrefGather(float4TypeId, context.Bound++, si.ResultId, x.Id, compareValue.Id, ImageOperandsMask.Offset, new EnumerantParameters(ops)));
            components[i] = builder.Insert(new OpCompositeExtract(floatTypeId, context.Bound++, gather.ResultId, [3])).ResultId;
        }
        var result = builder.Insert(new OpCompositeConstruct(float4TypeId, context.Bound++, [components[0], components[1], components[2], components[3]]));
        return new(result.ResultId, result.ResultType);
    }

    /// <summary>
    /// Returns true if the given ID refers to a constant instruction (OpConstant, OpConstantComposite, etc.) in context.
    /// </summary>
    private static bool IsConstantInContext(SpirvContext context, int id)
    {
        if (!context.GetBuffer().TryGetInstructionById(id, out var inst))
            return false;
        return inst.Op is Op.OpConstant or Op.OpConstantTrue or Op.OpConstantFalse
            or Op.OpConstantComposite or Op.OpConstantNull
            or Op.OpSpecConstantComposite;
    }

    /// <summary>
    /// Tries to promote a value to a constant in context for use as ConstOffset.
    /// Handles the simple case: OpCompositeConstruct in builder where all constituents are already constants in context.
    /// Also handles scalar constants already in context.
    /// Returns the constant ID if successful, or -1 if the value cannot be promoted.
    /// </summary>
    private static int TryPromoteToConstant(SpirvContext context, SpirvBuilder builder, int id)
    {
        // Already a constant in context
        if (IsConstantInContext(context, id))
            return id;

        // Check builder buffer for OpCompositeConstruct with all-constant constituents
        var buf = builder.GetBuffer();
        if (!buf.TryGetInstructionById(id, out var inst) || inst.Op != Op.OpCompositeConstruct)
            return -1;

        var span = inst.Data.Memory.Span;
        for (int j = 3; j < span.Length; j++)
        {
            if (!IsConstantInContext(context, span[j]))
                return -1;
        }

        // All constituents are constants — emit OpConstantComposite in context
        var resultType = span[1];
        var constId = context.Bound++;
        Span<int> constituents = stackalloc int[span.Length - 3];
        span[3..].CopyTo(constituents);
        context.AddData(new OpConstantComposite(resultType, constId, new(constituents)));
        return constId;
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

    private void TextureGenerateImageOperands(SpirvContext context, SpirvBuilder builder, SpirvValue? lod, SpirvValue? offset, SpirvValue? sampleIndex, out ImageOperandsMask imask, out EnumerantParameters imParams, SpirvValue? ddx = null, SpirvValue? ddy = null, SpirvValue? bias = null)
    {
        imask = ImageOperandsMask.None;
        // Allocate for worst case (6 operands: bias + grad(2) + lod + offset + sample)
        Span<int> operands = stackalloc int[6];
        int operandCount = 0;
        // Operands must appear in bit-order: Bias(0x1) < Lod(0x2) < Grad(0x4) < ConstOffset(0x8) < Offset(0x10) < Sample(0x40)
        if (bias != null)
        {
            imask |= ImageOperandsMask.Bias;
            operands[operandCount++] = bias.Value.Id;
        }
        if (lod != null)
        {
            imask |= ImageOperandsMask.Lod;
            operands[operandCount++] = lod.Value.Id;
        }
        if (ddx != null && ddy != null)
        {
            imask |= ImageOperandsMask.Grad;
            operands[operandCount++] = ddx.Value.Id;
            operands[operandCount++] = ddy.Value.Id;
        }
        if (offset != null)
        {
            // Try to promote to constant (handles int2(1,3) style inline constructors)
            var constId = TryPromoteToConstant(context, builder, offset.Value.Id);
            if (constId >= 0)
            {
                imask |= ImageOperandsMask.ConstOffset;
                operands[operandCount++] = constId;
            }
            else
            {
                imask |= ImageOperandsMask.Offset;
                operands[operandCount++] = offset.Value.Id;
            }
        }
        if (sampleIndex != null)
        {
            imask |= ImageOperandsMask.Sample;
            operands[operandCount++] = sampleIndex.Value.Id;
        }

        imParams = operandCount > 0 ? new EnumerantParameters(operands.Slice(0, operandCount)) : new EnumerantParameters();
    }
}
