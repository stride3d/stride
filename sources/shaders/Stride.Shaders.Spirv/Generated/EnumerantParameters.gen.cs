using static Stride.Shaders.Spirv.Specification;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Core;
public static class ImageOperandsParams
{
    public ref struct Bias(int idRef0) : IEnumerantParameter<Bias>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static Bias Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Bias
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Lod(int idRef0) : IEnumerantParameter<Lod>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static Lod Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Lod
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Grad(int idRef0, int idRef1) : IEnumerantParameter<Grad>
    {
        public int IdRef0 { get; set; } = idRef0;
        public int IdRef1 { get; set; } = idRef1;

        public static Grad Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Grad
            {
                IdRef0 = reader.ReadInt(),
                IdRef1 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ConstOffset(int idRef0) : IEnumerantParameter<ConstOffset>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static ConstOffset Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ConstOffset
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Offset(int idRef0) : IEnumerantParameter<Offset>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static Offset Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Offset
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ConstOffsets(int idRef0) : IEnumerantParameter<ConstOffsets>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static ConstOffsets Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ConstOffsets
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Sample(int idRef0) : IEnumerantParameter<Sample>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static Sample Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Sample
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MinLod(int idRef0) : IEnumerantParameter<MinLod>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static MinLod Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MinLod
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MakeTexelAvailable(int idscope0) : IEnumerantParameter<MakeTexelAvailable>
    {
        public int Idscope0 { get; set; } = idscope0;

        public static MakeTexelAvailable Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MakeTexelAvailable
            {
                Idscope0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MakeTexelVisible(int idscope0) : IEnumerantParameter<MakeTexelVisible>
    {
        public int Idscope0 { get; set; } = idscope0;

        public static MakeTexelVisible Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MakeTexelVisible
            {
                Idscope0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Offsets(int idRef0) : IEnumerantParameter<Offsets>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static Offsets Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Offsets
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }
}

public static class LoopControlParams
{
    public ref struct DependencyLength(int literalinteger0) : IEnumerantParameter<DependencyLength>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static DependencyLength Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new DependencyLength
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MinIterations(int literalinteger0) : IEnumerantParameter<MinIterations>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static MinIterations Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MinIterations
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxIterations(int literalinteger0) : IEnumerantParameter<MaxIterations>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static MaxIterations Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxIterations
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct IterationMultiple(int literalinteger0) : IEnumerantParameter<IterationMultiple>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static IterationMultiple Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new IterationMultiple
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PeelCount(int literalinteger0) : IEnumerantParameter<PeelCount>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static PeelCount Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PeelCount
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PartialCount(int literalinteger0) : IEnumerantParameter<PartialCount>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static PartialCount Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PartialCount
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct InitiationIntervalINTEL(int literalinteger0) : IEnumerantParameter<InitiationIntervalINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static InitiationIntervalINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new InitiationIntervalINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxConcurrencyINTEL(int literalinteger0) : IEnumerantParameter<MaxConcurrencyINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static MaxConcurrencyINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxConcurrencyINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct DependencyArrayINTEL(int literalinteger0) : IEnumerantParameter<DependencyArrayINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static DependencyArrayINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new DependencyArrayINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PipelineEnableINTEL(int literalinteger0) : IEnumerantParameter<PipelineEnableINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static PipelineEnableINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PipelineEnableINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LoopCoalesceINTEL(int literalinteger0) : IEnumerantParameter<LoopCoalesceINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static LoopCoalesceINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LoopCoalesceINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxInterleavingINTEL(int literalinteger0) : IEnumerantParameter<MaxInterleavingINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static MaxInterleavingINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxInterleavingINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SpeculatedIterationsINTEL(int literalinteger0) : IEnumerantParameter<SpeculatedIterationsINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static SpeculatedIterationsINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SpeculatedIterationsINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LoopCountINTEL(int literalinteger0) : IEnumerantParameter<LoopCountINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static LoopCountINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LoopCountINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxReinvocationDelayINTEL(int literalinteger0) : IEnumerantParameter<MaxReinvocationDelayINTEL>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static MaxReinvocationDelayINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxReinvocationDelayINTEL
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }
}

public static class MemoryAccessParams
{
    public ref struct Aligned(int literalinteger0) : IEnumerantParameter<Aligned>
    {
        public int Literalinteger0 { get; set; } = literalinteger0;

        public static Aligned Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Aligned
            {
                Literalinteger0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MakePointerAvailable(int idscope0) : IEnumerantParameter<MakePointerAvailable>
    {
        public int Idscope0 { get; set; } = idscope0;

        public static MakePointerAvailable Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MakePointerAvailable
            {
                Idscope0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MakePointerVisible(int idscope0) : IEnumerantParameter<MakePointerVisible>
    {
        public int Idscope0 { get; set; } = idscope0;

        public static MakePointerVisible Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MakePointerVisible
            {
                Idscope0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct AliasScopeINTELMask(int idRef0) : IEnumerantParameter<AliasScopeINTELMask>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static AliasScopeINTELMask Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new AliasScopeINTELMask
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct NoAliasINTELMask(int idRef0) : IEnumerantParameter<NoAliasINTELMask>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static NoAliasINTELMask Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NoAliasINTELMask
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }
}

public static class ExecutionModeParams
{
    public ref struct Invocations(int numberofInvocationinvocations) : IEnumerantParameter<Invocations>
    {
        public int NumberofInvocationinvocations { get; set; } = numberofInvocationinvocations;

        public static Invocations Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Invocations
            {
                NumberofInvocationinvocations = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LocalSize(int xsize, int ysize, int zsize) : IEnumerantParameter<LocalSize>
    {
        public int Xsize { get; set; } = xsize;
        public int Ysize { get; set; } = ysize;
        public int Zsize { get; set; } = zsize;

        public static LocalSize Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LocalSize
            {
                Xsize = reader.ReadInt(),
                Ysize = reader.ReadInt(),
                Zsize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LocalSizeHint(int xsize, int ysize, int zsize) : IEnumerantParameter<LocalSizeHint>
    {
        public int Xsize { get; set; } = xsize;
        public int Ysize { get; set; } = ysize;
        public int Zsize { get; set; } = zsize;

        public static LocalSizeHint Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LocalSizeHint
            {
                Xsize = reader.ReadInt(),
                Ysize = reader.ReadInt(),
                Zsize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct OutputVertices(int vertexcount) : IEnumerantParameter<OutputVertices>
    {
        public int Vertexcount { get; set; } = vertexcount;

        public static OutputVertices Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new OutputVertices
            {
                Vertexcount = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct VecTypeHint(int vectortype) : IEnumerantParameter<VecTypeHint>
    {
        public int Vectortype { get; set; } = vectortype;

        public static VecTypeHint Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new VecTypeHint
            {
                Vectortype = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SubgroupSize(int value) : IEnumerantParameter<SubgroupSize>
    {
        public int Value { get; set; } = value;

        public static SubgroupSize Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SubgroupSize
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SubgroupsPerWorkgroup(int value) : IEnumerantParameter<SubgroupsPerWorkgroup>
    {
        public int Value { get; set; } = value;

        public static SubgroupsPerWorkgroup Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SubgroupsPerWorkgroup
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SubgroupsPerWorkgroupId(int subgroupsPerWorkgroup) : IEnumerantParameter<SubgroupsPerWorkgroupId>
    {
        public int SubgroupsPerWorkgroup { get; set; } = subgroupsPerWorkgroup;

        public static SubgroupsPerWorkgroupId Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SubgroupsPerWorkgroupId
            {
                SubgroupsPerWorkgroup = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LocalSizeId(int xsize, int ysize, int zsize) : IEnumerantParameter<LocalSizeId>
    {
        public int Xsize { get; set; } = xsize;
        public int Ysize { get; set; } = ysize;
        public int Zsize { get; set; } = zsize;

        public static LocalSizeId Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LocalSizeId
            {
                Xsize = reader.ReadInt(),
                Ysize = reader.ReadInt(),
                Zsize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LocalSizeHintId(int xsizehint, int ysizehint, int zsizehint) : IEnumerantParameter<LocalSizeHintId>
    {
        public int Xsizehint { get; set; } = xsizehint;
        public int Ysizehint { get; set; } = ysizehint;
        public int Zsizehint { get; set; } = zsizehint;

        public static LocalSizeHintId Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LocalSizeHintId
            {
                Xsizehint = reader.ReadInt(),
                Ysizehint = reader.ReadInt(),
                Zsizehint = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct DenormPreserve(int targetWidth) : IEnumerantParameter<DenormPreserve>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static DenormPreserve Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new DenormPreserve
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct DenormFlushToZero(int targetWidth) : IEnumerantParameter<DenormFlushToZero>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static DenormFlushToZero Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new DenormFlushToZero
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SignedZeroInfNanPreserve(int targetWidth) : IEnumerantParameter<SignedZeroInfNanPreserve>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static SignedZeroInfNanPreserve Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SignedZeroInfNanPreserve
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct RoundingModeRTE(int targetWidth) : IEnumerantParameter<RoundingModeRTE>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static RoundingModeRTE Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new RoundingModeRTE
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct RoundingModeRTZ(int targetWidth) : IEnumerantParameter<RoundingModeRTZ>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static RoundingModeRTZ Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new RoundingModeRTZ
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct IsApiEntryAMDX(int isEntry) : IEnumerantParameter<IsApiEntryAMDX>
    {
        public int IsEntry { get; set; } = isEntry;

        public static IsApiEntryAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new IsApiEntryAMDX
            {
                IsEntry = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxNodeRecursionAMDX(int numberofrecursions) : IEnumerantParameter<MaxNodeRecursionAMDX>
    {
        public int Numberofrecursions { get; set; } = numberofrecursions;

        public static MaxNodeRecursionAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxNodeRecursionAMDX
            {
                Numberofrecursions = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct StaticNumWorkgroupsAMDX(int xsize, int ysize, int zsize) : IEnumerantParameter<StaticNumWorkgroupsAMDX>
    {
        public int Xsize { get; set; } = xsize;
        public int Ysize { get; set; } = ysize;
        public int Zsize { get; set; } = zsize;

        public static StaticNumWorkgroupsAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new StaticNumWorkgroupsAMDX
            {
                Xsize = reader.ReadInt(),
                Ysize = reader.ReadInt(),
                Zsize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ShaderIndexAMDX(int shaderIndex) : IEnumerantParameter<ShaderIndexAMDX>
    {
        public int ShaderIndex { get; set; } = shaderIndex;

        public static ShaderIndexAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ShaderIndexAMDX
            {
                ShaderIndex = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxNumWorkgroupsAMDX(int xsize, int ysize, int zsize) : IEnumerantParameter<MaxNumWorkgroupsAMDX>
    {
        public int Xsize { get; set; } = xsize;
        public int Ysize { get; set; } = ysize;
        public int Zsize { get; set; } = zsize;

        public static MaxNumWorkgroupsAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxNumWorkgroupsAMDX
            {
                Xsize = reader.ReadInt(),
                Ysize = reader.ReadInt(),
                Zsize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SharesInputWithAMDX(int nodeName, int shaderIndex) : IEnumerantParameter<SharesInputWithAMDX>
    {
        public int NodeName { get; set; } = nodeName;
        public int ShaderIndex { get; set; } = shaderIndex;

        public static SharesInputWithAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SharesInputWithAMDX
            {
                NodeName = reader.ReadInt(),
                ShaderIndex = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct OutputPrimitivesEXT(int primitivecount) : IEnumerantParameter<OutputPrimitivesEXT>
    {
        public int Primitivecount { get; set; } = primitivecount;

        public static OutputPrimitivesEXT Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new OutputPrimitivesEXT
            {
                Primitivecount = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SharedLocalMemorySizeINTEL(int size) : IEnumerantParameter<SharedLocalMemorySizeINTEL>
    {
        public int Size { get; set; } = size;

        public static SharedLocalMemorySizeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SharedLocalMemorySizeINTEL
            {
                Size = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct RoundingModeRTPINTEL(int targetWidth) : IEnumerantParameter<RoundingModeRTPINTEL>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static RoundingModeRTPINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new RoundingModeRTPINTEL
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct RoundingModeRTNINTEL(int targetWidth) : IEnumerantParameter<RoundingModeRTNINTEL>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static RoundingModeRTNINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new RoundingModeRTNINTEL
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct FloatingPointModeALTINTEL(int targetWidth) : IEnumerantParameter<FloatingPointModeALTINTEL>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static FloatingPointModeALTINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FloatingPointModeALTINTEL
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct FloatingPointModeIEEEINTEL(int targetWidth) : IEnumerantParameter<FloatingPointModeIEEEINTEL>
    {
        public int TargetWidth { get; set; } = targetWidth;

        public static FloatingPointModeIEEEINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FloatingPointModeIEEEINTEL
            {
                TargetWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxWorkgroupSizeINTEL(int maxxsize, int maxysize, int maxzsize) : IEnumerantParameter<MaxWorkgroupSizeINTEL>
    {
        public int Maxxsize { get; set; } = maxxsize;
        public int Maxysize { get; set; } = maxysize;
        public int Maxzsize { get; set; } = maxzsize;

        public static MaxWorkgroupSizeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxWorkgroupSizeINTEL
            {
                Maxxsize = reader.ReadInt(),
                Maxysize = reader.ReadInt(),
                Maxzsize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxWorkDimINTEL(int maxdimensions) : IEnumerantParameter<MaxWorkDimINTEL>
    {
        public int Maxdimensions { get; set; } = maxdimensions;

        public static MaxWorkDimINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxWorkDimINTEL
            {
                Maxdimensions = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct NumSIMDWorkitemsINTEL(int vectorwidth) : IEnumerantParameter<NumSIMDWorkitemsINTEL>
    {
        public int Vectorwidth { get; set; } = vectorwidth;

        public static NumSIMDWorkitemsINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NumSIMDWorkitemsINTEL
            {
                Vectorwidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SchedulerTargetFmaxMhzINTEL(int targetfmax) : IEnumerantParameter<SchedulerTargetFmaxMhzINTEL>
    {
        public int Targetfmax { get; set; } = targetfmax;

        public static SchedulerTargetFmaxMhzINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SchedulerTargetFmaxMhzINTEL
            {
                Targetfmax = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct FPFastMathDefault(int targetType, int fastMathMode) : IEnumerantParameter<FPFastMathDefault>
    {
        public int TargetType { get; set; } = targetType;
        public int FastMathMode { get; set; } = fastMathMode;

        public static FPFastMathDefault Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FPFastMathDefault
            {
                TargetType = reader.ReadInt(),
                FastMathMode = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct StreamingInterfaceINTEL(int stallFreeReturn) : IEnumerantParameter<StreamingInterfaceINTEL>
    {
        public int StallFreeReturn { get; set; } = stallFreeReturn;

        public static StreamingInterfaceINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new StreamingInterfaceINTEL
            {
                StallFreeReturn = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct RegisterMapInterfaceINTEL(int waitForDoneWrite) : IEnumerantParameter<RegisterMapInterfaceINTEL>
    {
        public int WaitForDoneWrite { get; set; } = waitForDoneWrite;

        public static RegisterMapInterfaceINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new RegisterMapInterfaceINTEL
            {
                WaitForDoneWrite = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct NamedBarrierCountINTEL(int barrierCount) : IEnumerantParameter<NamedBarrierCountINTEL>
    {
        public int BarrierCount { get; set; } = barrierCount;

        public static NamedBarrierCountINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NamedBarrierCountINTEL
            {
                BarrierCount = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaximumRegistersINTEL(int numberofRegisters) : IEnumerantParameter<MaximumRegistersINTEL>
    {
        public int NumberofRegisters { get; set; } = numberofRegisters;

        public static MaximumRegistersINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaximumRegistersINTEL
            {
                NumberofRegisters = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaximumRegistersIdINTEL(int numberofRegisters) : IEnumerantParameter<MaximumRegistersIdINTEL>
    {
        public int NumberofRegisters { get; set; } = numberofRegisters;

        public static MaximumRegistersIdINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaximumRegistersIdINTEL
            {
                NumberofRegisters = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct NamedMaximumRegistersINTEL(NamedMaximumNumberOfRegisters namedMaximumNumberofRegisters) : IEnumerantParameter<NamedMaximumRegistersINTEL>
    {
        public NamedMaximumNumberOfRegisters NamedMaximumNumberofRegisters { get; set; } = namedMaximumNumberofRegisters;

        public static NamedMaximumRegistersINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NamedMaximumRegistersINTEL
            {
                NamedMaximumNumberofRegisters = reader.ReadEnum<NamedMaximumNumberOfRegisters>(),
            };
            return parameter;
        }
    }
}

public static class DecorationParams
{
    public ref struct SpecId(int specializationConstantID) : IEnumerantParameter<SpecId>
    {
        public int SpecializationConstantID { get; set; } = specializationConstantID;

        public static SpecId Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SpecId
            {
                SpecializationConstantID = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ArrayStride(int value) : IEnumerantParameter<ArrayStride>
    {
        public int Value { get; set; } = value;

        public static ArrayStride Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ArrayStride
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MatrixStride(int value) : IEnumerantParameter<MatrixStride>
    {
        public int Value { get; set; } = value;

        public static MatrixStride Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MatrixStride
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct BuiltInParameter(BuiltIn value) : IEnumerantParameter<BuiltInParameter>
    {
        public BuiltIn Value { get; set; } = value;

        public static BuiltInParameter Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new BuiltInParameter
            {
                Value = reader.ReadEnum<BuiltIn>(),
            };
            return parameter;
        }
    }

    public ref struct UniformId(int execution) : IEnumerantParameter<UniformId>
    {
        public int Execution { get; set; } = execution;

        public static UniformId Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new UniformId
            {
                Execution = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Stream(int streamNumber) : IEnumerantParameter<Stream>
    {
        public int StreamNumber { get; set; } = streamNumber;

        public static Stream Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Stream
            {
                StreamNumber = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Location(int value) : IEnumerantParameter<Location>
    {
        public int Value { get; set; } = value;

        public static Location Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Location
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Component(int value) : IEnumerantParameter<Component>
    {
        public int Value { get; set; } = value;

        public static Component Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Component
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Index(int value) : IEnumerantParameter<Index>
    {
        public int Value { get; set; } = value;

        public static Index Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Index
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Binding(int bindingPoint) : IEnumerantParameter<Binding>
    {
        public int BindingPoint { get; set; } = bindingPoint;

        public static Binding Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Binding
            {
                BindingPoint = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct DescriptorSet(int value) : IEnumerantParameter<DescriptorSet>
    {
        public int Value { get; set; } = value;

        public static DescriptorSet Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new DescriptorSet
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Offset(int byteOffset) : IEnumerantParameter<Offset>
    {
        public int ByteOffset { get; set; } = byteOffset;

        public static Offset Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Offset
            {
                ByteOffset = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct XfbBuffer(int xFBBufferNumber) : IEnumerantParameter<XfbBuffer>
    {
        public int XFBBufferNumber { get; set; } = xFBBufferNumber;

        public static XfbBuffer Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new XfbBuffer
            {
                XFBBufferNumber = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct XfbStride(int xFBStride) : IEnumerantParameter<XfbStride>
    {
        public int XFBStride { get; set; } = xFBStride;

        public static XfbStride Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new XfbStride
            {
                XFBStride = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct FuncParamAttr(FunctionParameterAttribute functionParameterAttribute) : IEnumerantParameter<FuncParamAttr>
    {
        public FunctionParameterAttribute FunctionParameterAttribute { get; set; } = functionParameterAttribute;

        public static FuncParamAttr Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FuncParamAttr
            {
                FunctionParameterAttribute = reader.ReadEnum<FunctionParameterAttribute>(),
            };
            return parameter;
        }
    }

    public ref struct FPRoundingModeParameter(FPRoundingMode value) : IEnumerantParameter<FPRoundingModeParameter>
    {
        public FPRoundingMode Value { get; set; } = value;

        public static FPRoundingModeParameter Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FPRoundingModeParameter
            {
                Value = reader.ReadEnum<FPRoundingMode>(),
            };
            return parameter;
        }
    }

    public ref struct FPFastMathModeParameter(FPFastMathModeMask value) : IEnumerantParameter<FPFastMathModeParameter>
    {
        public FPFastMathModeMask Value { get; set; } = value;

        public static FPFastMathModeParameter Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FPFastMathModeParameter
            {
                Value = reader.ReadEnum<FPFastMathModeMask>(),
            };
            return parameter;
        }
    }

    public ref struct LinkageAttributes(string name, LinkageType linkageType) : IEnumerantParameter<LinkageAttributes>
    {
        public string Name { get; set; } = name;
        public LinkageType LinkageType { get; set; } = linkageType;

        public static LinkageAttributes Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LinkageAttributes
            {
                Name = reader.ReadString(),
                LinkageType = reader.ReadEnum<LinkageType>(),
            };
            return parameter;
        }
    }

    public ref struct InputAttachmentIndex(int attachmentIndex) : IEnumerantParameter<InputAttachmentIndex>
    {
        public int AttachmentIndex { get; set; } = attachmentIndex;

        public static InputAttachmentIndex Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new InputAttachmentIndex
            {
                AttachmentIndex = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct Alignment(int value) : IEnumerantParameter<Alignment>
    {
        public int Value { get; set; } = value;

        public static Alignment Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new Alignment
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxByteOffset(int value) : IEnumerantParameter<MaxByteOffset>
    {
        public int Value { get; set; } = value;

        public static MaxByteOffset Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxByteOffset
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct AlignmentId(int alignment) : IEnumerantParameter<AlignmentId>
    {
        public int Alignment { get; set; } = alignment;

        public static AlignmentId Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new AlignmentId
            {
                Alignment = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxByteOffsetId(int maxByteOffset) : IEnumerantParameter<MaxByteOffsetId>
    {
        public int MaxByteOffset { get; set; } = maxByteOffset;

        public static MaxByteOffsetId Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxByteOffsetId
            {
                MaxByteOffset = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct NodeSharesPayloadLimitsWithAMDX(int payloadType) : IEnumerantParameter<NodeSharesPayloadLimitsWithAMDX>
    {
        public int PayloadType { get; set; } = payloadType;

        public static NodeSharesPayloadLimitsWithAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NodeSharesPayloadLimitsWithAMDX
            {
                PayloadType = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct NodeMaxPayloadsAMDX(int maxnumberofpayloads) : IEnumerantParameter<NodeMaxPayloadsAMDX>
    {
        public int Maxnumberofpayloads { get; set; } = maxnumberofpayloads;

        public static NodeMaxPayloadsAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NodeMaxPayloadsAMDX
            {
                Maxnumberofpayloads = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PayloadNodeNameAMDX(int nodeName) : IEnumerantParameter<PayloadNodeNameAMDX>
    {
        public int NodeName { get; set; } = nodeName;

        public static PayloadNodeNameAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PayloadNodeNameAMDX
            {
                NodeName = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PayloadNodeBaseIndexAMDX(int baseIndex) : IEnumerantParameter<PayloadNodeBaseIndexAMDX>
    {
        public int BaseIndex { get; set; } = baseIndex;

        public static PayloadNodeBaseIndexAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PayloadNodeBaseIndexAMDX
            {
                BaseIndex = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PayloadNodeArraySizeAMDX(int arraySize) : IEnumerantParameter<PayloadNodeArraySizeAMDX>
    {
        public int ArraySize { get; set; } = arraySize;

        public static PayloadNodeArraySizeAMDX Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PayloadNodeArraySizeAMDX
            {
                ArraySize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SecondaryViewportRelativeNV(int offset) : IEnumerantParameter<SecondaryViewportRelativeNV>
    {
        public int Offset { get; set; } = offset;

        public static SecondaryViewportRelativeNV Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SecondaryViewportRelativeNV
            {
                Offset = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct SIMTCallINTEL(int n) : IEnumerantParameter<SIMTCallINTEL>
    {
        public int N { get; set; } = n;

        public static SIMTCallINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SIMTCallINTEL
            {
                N = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ClobberINTEL(string register) : IEnumerantParameter<ClobberINTEL>
    {
        public string Register { get; set; } = register;

        public static ClobberINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ClobberINTEL
            {
                Register = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct FuncParamIOKindINTEL(int kind) : IEnumerantParameter<FuncParamIOKindINTEL>
    {
        public int Kind { get; set; } = kind;

        public static FuncParamIOKindINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FuncParamIOKindINTEL
            {
                Kind = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct GlobalVariableOffsetINTEL(int offset) : IEnumerantParameter<GlobalVariableOffsetINTEL>
    {
        public int Offset { get; set; } = offset;

        public static GlobalVariableOffsetINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new GlobalVariableOffsetINTEL
            {
                Offset = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct CounterBuffer(int value) : IEnumerantParameter<CounterBuffer>
    {
        public int Value { get; set; } = value;

        public static CounterBuffer Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new CounterBuffer
            {
                Value = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct UserSemantic(string semantic) : IEnumerantParameter<UserSemantic>
    {
        public string Semantic { get; set; } = semantic;

        public static UserSemantic Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new UserSemantic
            {
                Semantic = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct UserTypeGOOGLE(string userType) : IEnumerantParameter<UserTypeGOOGLE>
    {
        public string UserType { get; set; } = userType;

        public static UserTypeGOOGLE Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new UserTypeGOOGLE
            {
                UserType = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct FunctionRoundingModeINTEL(int targetWidth, FPRoundingMode fPRoundingMode) : IEnumerantParameter<FunctionRoundingModeINTEL>
    {
        public int TargetWidth { get; set; } = targetWidth;
        public FPRoundingMode FPRoundingMode { get; set; } = fPRoundingMode;

        public static FunctionRoundingModeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FunctionRoundingModeINTEL
            {
                TargetWidth = reader.ReadInt(),
                FPRoundingMode = reader.ReadEnum<FPRoundingMode>(),
            };
            return parameter;
        }
    }

    public ref struct FunctionDenormModeINTEL(int targetWidth, FPDenormMode fPDenormMode) : IEnumerantParameter<FunctionDenormModeINTEL>
    {
        public int TargetWidth { get; set; } = targetWidth;
        public FPDenormMode FPDenormMode { get; set; } = fPDenormMode;

        public static FunctionDenormModeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FunctionDenormModeINTEL
            {
                TargetWidth = reader.ReadInt(),
                FPDenormMode = reader.ReadEnum<FPDenormMode>(),
            };
            return parameter;
        }
    }

    public ref struct MemoryINTEL(string memoryType) : IEnumerantParameter<MemoryINTEL>
    {
        public string MemoryType { get; set; } = memoryType;

        public static MemoryINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MemoryINTEL
            {
                MemoryType = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct NumbanksINTEL(int banks) : IEnumerantParameter<NumbanksINTEL>
    {
        public int Banks { get; set; } = banks;

        public static NumbanksINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NumbanksINTEL
            {
                Banks = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct BankwidthINTEL(int bankWidth) : IEnumerantParameter<BankwidthINTEL>
    {
        public int BankWidth { get; set; } = bankWidth;

        public static BankwidthINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new BankwidthINTEL
            {
                BankWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxPrivateCopiesINTEL(int maximumCopies) : IEnumerantParameter<MaxPrivateCopiesINTEL>
    {
        public int MaximumCopies { get; set; } = maximumCopies;

        public static MaxPrivateCopiesINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxPrivateCopiesINTEL
            {
                MaximumCopies = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxReplicatesINTEL(int maximumReplicates) : IEnumerantParameter<MaxReplicatesINTEL>
    {
        public int MaximumReplicates { get; set; } = maximumReplicates;

        public static MaxReplicatesINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxReplicatesINTEL
            {
                MaximumReplicates = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MergeINTEL(string mergeKey, string mergeType) : IEnumerantParameter<MergeINTEL>
    {
        public string MergeKey { get; set; } = mergeKey;
        public string MergeType { get; set; } = mergeType;

        public static MergeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MergeINTEL
            {
                MergeKey = reader.ReadString(),
                MergeType = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct BankBitsINTEL(int bankBits) : IEnumerantParameter<BankBitsINTEL>
    {
        public int BankBits { get; set; } = bankBits;

        public static BankBitsINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new BankBitsINTEL
            {
                BankBits = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ForcePow2DepthINTEL(int forceKey) : IEnumerantParameter<ForcePow2DepthINTEL>
    {
        public int ForceKey { get; set; } = forceKey;

        public static ForcePow2DepthINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ForcePow2DepthINTEL
            {
                ForceKey = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct StridesizeINTEL(int strideSize) : IEnumerantParameter<StridesizeINTEL>
    {
        public int StrideSize { get; set; } = strideSize;

        public static StridesizeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new StridesizeINTEL
            {
                StrideSize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct WordsizeINTEL(int wordSize) : IEnumerantParameter<WordsizeINTEL>
    {
        public int WordSize { get; set; } = wordSize;

        public static WordsizeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new WordsizeINTEL
            {
                WordSize = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct CacheSizeINTEL(int cacheSizeinbytes) : IEnumerantParameter<CacheSizeINTEL>
    {
        public int CacheSizeinbytes { get; set; } = cacheSizeinbytes;

        public static CacheSizeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new CacheSizeINTEL
            {
                CacheSizeinbytes = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PrefetchINTEL(int prefetcherSizeinbytes) : IEnumerantParameter<PrefetchINTEL>
    {
        public int PrefetcherSizeinbytes { get; set; } = prefetcherSizeinbytes;

        public static PrefetchINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PrefetchINTEL
            {
                PrefetcherSizeinbytes = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MathOpDSPModeINTEL(int mode, int propagate) : IEnumerantParameter<MathOpDSPModeINTEL>
    {
        public int Mode { get; set; } = mode;
        public int Propagate { get; set; } = propagate;

        public static MathOpDSPModeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MathOpDSPModeINTEL
            {
                Mode = reader.ReadInt(),
                Propagate = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct AliasScopeINTEL(int aliasingScopesList) : IEnumerantParameter<AliasScopeINTEL>
    {
        public int AliasingScopesList { get; set; } = aliasingScopesList;

        public static AliasScopeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new AliasScopeINTEL
            {
                AliasingScopesList = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct NoAliasINTEL(int aliasingScopesList) : IEnumerantParameter<NoAliasINTEL>
    {
        public int AliasingScopesList { get; set; } = aliasingScopesList;

        public static NoAliasINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new NoAliasINTEL
            {
                AliasingScopesList = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct InitiationIntervalINTEL(int cycles) : IEnumerantParameter<InitiationIntervalINTEL>
    {
        public int Cycles { get; set; } = cycles;

        public static InitiationIntervalINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new InitiationIntervalINTEL
            {
                Cycles = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MaxConcurrencyINTEL(int invocations) : IEnumerantParameter<MaxConcurrencyINTEL>
    {
        public int Invocations { get; set; } = invocations;

        public static MaxConcurrencyINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MaxConcurrencyINTEL
            {
                Invocations = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct PipelineEnableINTEL(int enable) : IEnumerantParameter<PipelineEnableINTEL>
    {
        public int Enable { get; set; } = enable;

        public static PipelineEnableINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new PipelineEnableINTEL
            {
                Enable = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct BufferLocationINTEL(int bufferLocationID) : IEnumerantParameter<BufferLocationINTEL>
    {
        public int BufferLocationID { get; set; } = bufferLocationID;

        public static BufferLocationINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new BufferLocationINTEL
            {
                BufferLocationID = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct IOPipeStorageINTEL(int iOPipeID) : IEnumerantParameter<IOPipeStorageINTEL>
    {
        public int IOPipeID { get; set; } = iOPipeID;

        public static IOPipeStorageINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new IOPipeStorageINTEL
            {
                IOPipeID = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct FunctionFloatingPointModeINTEL(int targetWidth, FPOperationMode fPOperationMode) : IEnumerantParameter<FunctionFloatingPointModeINTEL>
    {
        public int TargetWidth { get; set; } = targetWidth;
        public FPOperationMode FPOperationMode { get; set; } = fPOperationMode;

        public static FunctionFloatingPointModeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FunctionFloatingPointModeINTEL
            {
                TargetWidth = reader.ReadInt(),
                FPOperationMode = reader.ReadEnum<FPOperationMode>(),
            };
            return parameter;
        }
    }

    public ref struct FPMaxErrorDecorationINTEL(float maxError) : IEnumerantParameter<FPMaxErrorDecorationINTEL>
    {
        public float MaxError { get; set; } = maxError;

        public static FPMaxErrorDecorationINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FPMaxErrorDecorationINTEL
            {
                MaxError = reader.ReadFloat(),
            };
            return parameter;
        }
    }

    public ref struct LatencyControlLabelINTEL(int latencyLabel) : IEnumerantParameter<LatencyControlLabelINTEL>
    {
        public int LatencyLabel { get; set; } = latencyLabel;

        public static LatencyControlLabelINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LatencyControlLabelINTEL
            {
                LatencyLabel = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LatencyControlConstraintINTEL(int relativeTo, int controlType, int relativeCycle) : IEnumerantParameter<LatencyControlConstraintINTEL>
    {
        public int RelativeTo { get; set; } = relativeTo;
        public int ControlType { get; set; } = controlType;
        public int RelativeCycle { get; set; } = relativeCycle;

        public static LatencyControlConstraintINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LatencyControlConstraintINTEL
            {
                RelativeTo = reader.ReadInt(),
                ControlType = reader.ReadInt(),
                RelativeCycle = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MMHostInterfaceAddressWidthINTEL(int addressWidth) : IEnumerantParameter<MMHostInterfaceAddressWidthINTEL>
    {
        public int AddressWidth { get; set; } = addressWidth;

        public static MMHostInterfaceAddressWidthINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MMHostInterfaceAddressWidthINTEL
            {
                AddressWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MMHostInterfaceDataWidthINTEL(int dataWidth) : IEnumerantParameter<MMHostInterfaceDataWidthINTEL>
    {
        public int DataWidth { get; set; } = dataWidth;

        public static MMHostInterfaceDataWidthINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MMHostInterfaceDataWidthINTEL
            {
                DataWidth = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MMHostInterfaceLatencyINTEL(int latency) : IEnumerantParameter<MMHostInterfaceLatencyINTEL>
    {
        public int Latency { get; set; } = latency;

        public static MMHostInterfaceLatencyINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MMHostInterfaceLatencyINTEL
            {
                Latency = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MMHostInterfaceReadWriteModeINTEL(AccessQualifier readWriteMode) : IEnumerantParameter<MMHostInterfaceReadWriteModeINTEL>
    {
        public AccessQualifier ReadWriteMode { get; set; } = readWriteMode;

        public static MMHostInterfaceReadWriteModeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MMHostInterfaceReadWriteModeINTEL
            {
                ReadWriteMode = reader.ReadEnum<AccessQualifier>(),
            };
            return parameter;
        }
    }

    public ref struct MMHostInterfaceMaxBurstINTEL(int maxBurstCount) : IEnumerantParameter<MMHostInterfaceMaxBurstINTEL>
    {
        public int MaxBurstCount { get; set; } = maxBurstCount;

        public static MMHostInterfaceMaxBurstINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MMHostInterfaceMaxBurstINTEL
            {
                MaxBurstCount = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct MMHostInterfaceWaitRequestINTEL(int waitrequest) : IEnumerantParameter<MMHostInterfaceWaitRequestINTEL>
    {
        public int Waitrequest { get; set; } = waitrequest;

        public static MMHostInterfaceWaitRequestINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new MMHostInterfaceWaitRequestINTEL
            {
                Waitrequest = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct HostAccessINTEL(HostAccessQualifier access, string name) : IEnumerantParameter<HostAccessINTEL>
    {
        public HostAccessQualifier Access { get; set; } = access;
        public string Name { get; set; } = name;

        public static HostAccessINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new HostAccessINTEL
            {
                Access = reader.ReadEnum<HostAccessQualifier>(),
                Name = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct InitModeINTEL(InitializationModeQualifier trigger) : IEnumerantParameter<InitModeINTEL>
    {
        public InitializationModeQualifier Trigger { get; set; } = trigger;

        public static InitModeINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new InitModeINTEL
            {
                Trigger = reader.ReadEnum<InitializationModeQualifier>(),
            };
            return parameter;
        }
    }

    public ref struct ImplementInRegisterMapINTEL(int parameter0) : IEnumerantParameter<ImplementInRegisterMapINTEL>
    {
        public int Parameter0 { get; set; } = parameter0;

        public static ImplementInRegisterMapINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ImplementInRegisterMapINTEL
            {
                Parameter0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct CacheControlLoadINTEL(int cacheLevel, LoadCacheControl cacheControl) : IEnumerantParameter<CacheControlLoadINTEL>
    {
        public int CacheLevel { get; set; } = cacheLevel;
        public LoadCacheControl CacheControl { get; set; } = cacheControl;

        public static CacheControlLoadINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new CacheControlLoadINTEL
            {
                CacheLevel = reader.ReadInt(),
                CacheControl = reader.ReadEnum<LoadCacheControl>(),
            };
            return parameter;
        }
    }

    public ref struct CacheControlStoreINTEL(int cacheLevel, StoreCacheControl cacheControl) : IEnumerantParameter<CacheControlStoreINTEL>
    {
        public int CacheLevel { get; set; } = cacheLevel;
        public StoreCacheControl CacheControl { get; set; } = cacheControl;

        public static CacheControlStoreINTEL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new CacheControlStoreINTEL
            {
                CacheLevel = reader.ReadInt(),
                CacheControl = reader.ReadEnum<StoreCacheControl>(),
            };
            return parameter;
        }
    }

    public ref struct LinkSDSL(string name) : IEnumerantParameter<LinkSDSL>
    {
        public string Name { get; set; } = name;

        public static LinkSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LinkSDSL
            {
                Name = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct LinkIdSDSL(int idRef0) : IEnumerantParameter<LinkIdSDSL>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static LinkIdSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LinkIdSDSL
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ColorSDSL(int idRef0) : IEnumerantParameter<ColorSDSL>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static ColorSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ColorSDSL
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct ResourceGroupSDSL(string resourceGroup) : IEnumerantParameter<ResourceGroupSDSL>
    {
        public string ResourceGroup { get; set; } = resourceGroup;

        public static ResourceGroupSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ResourceGroupSDSL
            {
                ResourceGroup = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct ResourceGroupIdSDSL(int resourceGroup) : IEnumerantParameter<ResourceGroupIdSDSL>
    {
        public int ResourceGroup { get; set; } = resourceGroup;

        public static ResourceGroupIdSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new ResourceGroupIdSDSL
            {
                ResourceGroup = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct LogicalGroupSDSL(string logicalGroup) : IEnumerantParameter<LogicalGroupSDSL>
    {
        public string LogicalGroup { get; set; } = logicalGroup;

        public static LogicalGroupSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new LogicalGroupSDSL
            {
                LogicalGroup = reader.ReadString(),
            };
            return parameter;
        }
    }

    public ref struct SamplerStateSDSL(int parameter0, int parameter1, int parameter2, int parameter3, int parameter4, int parameter5, int parameter6, int parameter7, int parameter8, int parameter9, int parameter10, int parameter11, int parameter12) : IEnumerantParameter<SamplerStateSDSL>
    {
        public int Parameter0 { get; set; } = parameter0;
        public int Parameter1 { get; set; } = parameter1;
        public int Parameter2 { get; set; } = parameter2;
        public int Parameter3 { get; set; } = parameter3;
        public int Parameter4 { get; set; } = parameter4;
        public int Parameter5 { get; set; } = parameter5;
        public int Parameter6 { get; set; } = parameter6;
        public int Parameter7 { get; set; } = parameter7;
        public int Parameter8 { get; set; } = parameter8;
        public int Parameter9 { get; set; } = parameter9;
        public int Parameter10 { get; set; } = parameter10;
        public int Parameter11 { get; set; } = parameter11;
        public int Parameter12 { get; set; } = parameter12;

        public static SamplerStateSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new SamplerStateSDSL
            {
                Parameter0 = reader.ReadInt(),
                Parameter1 = reader.ReadInt(),
                Parameter2 = reader.ReadInt(),
                Parameter3 = reader.ReadInt(),
                Parameter4 = reader.ReadInt(),
                Parameter5 = reader.ReadInt(),
                Parameter6 = reader.ReadInt(),
                Parameter7 = reader.ReadInt(),
                Parameter8 = reader.ReadInt(),
                Parameter9 = reader.ReadInt(),
                Parameter10 = reader.ReadInt(),
                Parameter11 = reader.ReadInt(),
                Parameter12 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct FunctionParameterDefaultValueSDSL(int idRef0) : IEnumerantParameter<FunctionParameterDefaultValueSDSL>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static FunctionParameterDefaultValueSDSL Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new FunctionParameterDefaultValueSDSL
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }
}

public static class TensorAddressingOperandsParams
{
    public ref struct TensorView(int idRef0) : IEnumerantParameter<TensorView>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static TensorView Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new TensorView
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }

    public ref struct DecodeFunc(int idRef0) : IEnumerantParameter<DecodeFunc>
    {
        public int IdRef0 { get; set; } = idRef0;

        public static DecodeFunc Create(Span<int> words)
        {
            var reader = new EnumerantParametersReader(words);
            var parameter = new DecodeFunc
            {
                IdRef0 = reader.ReadInt(),
            };
            return parameter;
        }
    }
}

public ref partial struct EnumerantParameters
{
    public static implicit operator EnumerantParameters(ImageOperandsParams.Bias parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.Lod parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.Grad parameter)
    {
        Span<int> span = [parameter.IdRef0, parameter.IdRef1];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.ConstOffset parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.Offset parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.ConstOffsets parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.Sample parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.MinLod parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.MakeTexelAvailable parameter)
    {
        Span<int> span = [(int)parameter.Idscope0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.MakeTexelVisible parameter)
    {
        Span<int> span = [(int)parameter.Idscope0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ImageOperandsParams.Offsets parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.DependencyLength parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.MinIterations parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.MaxIterations parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.IterationMultiple parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.PeelCount parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.PartialCount parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.InitiationIntervalINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.MaxConcurrencyINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.DependencyArrayINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.PipelineEnableINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.LoopCoalesceINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.MaxInterleavingINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.SpeculatedIterationsINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.LoopCountINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(LoopControlParams.MaxReinvocationDelayINTEL parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(MemoryAccessParams.Aligned parameter)
    {
        Span<int> span = [parameter.Literalinteger0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(MemoryAccessParams.MakePointerAvailable parameter)
    {
        Span<int> span = [(int)parameter.Idscope0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(MemoryAccessParams.MakePointerVisible parameter)
    {
        Span<int> span = [(int)parameter.Idscope0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(MemoryAccessParams.AliasScopeINTELMask parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(MemoryAccessParams.NoAliasINTELMask parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.Invocations parameter)
    {
        Span<int> span = [parameter.NumberofInvocationinvocations];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.LocalSize parameter)
    {
        Span<int> span = [parameter.Xsize, parameter.Ysize, parameter.Zsize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.LocalSizeHint parameter)
    {
        Span<int> span = [parameter.Xsize, parameter.Ysize, parameter.Zsize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.OutputVertices parameter)
    {
        Span<int> span = [parameter.Vertexcount];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.VecTypeHint parameter)
    {
        Span<int> span = [parameter.Vectortype];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.SubgroupSize parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.SubgroupsPerWorkgroup parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.SubgroupsPerWorkgroupId parameter)
    {
        Span<int> span = [parameter.SubgroupsPerWorkgroup];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.LocalSizeId parameter)
    {
        Span<int> span = [parameter.Xsize, parameter.Ysize, parameter.Zsize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.LocalSizeHintId parameter)
    {
        Span<int> span = [parameter.Xsizehint, parameter.Ysizehint, parameter.Zsizehint];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.DenormPreserve parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.DenormFlushToZero parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.SignedZeroInfNanPreserve parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.RoundingModeRTE parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.RoundingModeRTZ parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.IsApiEntryAMDX parameter)
    {
        Span<int> span = [parameter.IsEntry];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.MaxNodeRecursionAMDX parameter)
    {
        Span<int> span = [parameter.Numberofrecursions];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.StaticNumWorkgroupsAMDX parameter)
    {
        Span<int> span = [parameter.Xsize, parameter.Ysize, parameter.Zsize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.ShaderIndexAMDX parameter)
    {
        Span<int> span = [parameter.ShaderIndex];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.MaxNumWorkgroupsAMDX parameter)
    {
        Span<int> span = [parameter.Xsize, parameter.Ysize, parameter.Zsize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.SharesInputWithAMDX parameter)
    {
        Span<int> span = [parameter.NodeName, parameter.ShaderIndex];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.OutputPrimitivesEXT parameter)
    {
        Span<int> span = [parameter.Primitivecount];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.SharedLocalMemorySizeINTEL parameter)
    {
        Span<int> span = [parameter.Size];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.RoundingModeRTPINTEL parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.RoundingModeRTNINTEL parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.FloatingPointModeALTINTEL parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.FloatingPointModeIEEEINTEL parameter)
    {
        Span<int> span = [parameter.TargetWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.MaxWorkgroupSizeINTEL parameter)
    {
        Span<int> span = [parameter.Maxxsize, parameter.Maxysize, parameter.Maxzsize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.MaxWorkDimINTEL parameter)
    {
        Span<int> span = [parameter.Maxdimensions];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.NumSIMDWorkitemsINTEL parameter)
    {
        Span<int> span = [parameter.Vectorwidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.SchedulerTargetFmaxMhzINTEL parameter)
    {
        Span<int> span = [parameter.Targetfmax];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.FPFastMathDefault parameter)
    {
        Span<int> span = [parameter.TargetType, parameter.FastMathMode];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.StreamingInterfaceINTEL parameter)
    {
        Span<int> span = [parameter.StallFreeReturn];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.RegisterMapInterfaceINTEL parameter)
    {
        Span<int> span = [parameter.WaitForDoneWrite];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.NamedBarrierCountINTEL parameter)
    {
        Span<int> span = [parameter.BarrierCount];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.MaximumRegistersINTEL parameter)
    {
        Span<int> span = [parameter.NumberofRegisters];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.MaximumRegistersIdINTEL parameter)
    {
        Span<int> span = [parameter.NumberofRegisters];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(ExecutionModeParams.NamedMaximumRegistersINTEL parameter)
    {
        Span<int> span = [(int)parameter.NamedMaximumNumberofRegisters];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.SpecId parameter)
    {
        Span<int> span = [parameter.SpecializationConstantID];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.ArrayStride parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MatrixStride parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.BuiltInParameter parameter)
    {
        Span<int> span = [(int)parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.UniformId parameter)
    {
        Span<int> span = [(int)parameter.Execution];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.Stream parameter)
    {
        Span<int> span = [parameter.StreamNumber];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.Location parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.Component parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.Index parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.Binding parameter)
    {
        Span<int> span = [parameter.BindingPoint];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.DescriptorSet parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.Offset parameter)
    {
        Span<int> span = [parameter.ByteOffset];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.XfbBuffer parameter)
    {
        Span<int> span = [parameter.XFBBufferNumber];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.XfbStride parameter)
    {
        Span<int> span = [parameter.XFBStride];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FuncParamAttr parameter)
    {
        Span<int> span = [(int)parameter.FunctionParameterAttribute];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FPRoundingModeParameter parameter)
    {
        Span<int> span = [(int)parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FPFastMathModeParameter parameter)
    {
        Span<int> span = [(int)parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.LinkageAttributes parameter)
    {
        Span<int> span = [..parameter.Name.AsDisposableLiteralValue().Words, (int)parameter.LinkageType];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.InputAttachmentIndex parameter)
    {
        Span<int> span = [parameter.AttachmentIndex];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.Alignment parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MaxByteOffset parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.AlignmentId parameter)
    {
        Span<int> span = [parameter.Alignment];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MaxByteOffsetId parameter)
    {
        Span<int> span = [parameter.MaxByteOffset];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.NodeSharesPayloadLimitsWithAMDX parameter)
    {
        Span<int> span = [parameter.PayloadType];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.NodeMaxPayloadsAMDX parameter)
    {
        Span<int> span = [parameter.Maxnumberofpayloads];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.PayloadNodeNameAMDX parameter)
    {
        Span<int> span = [parameter.NodeName];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.PayloadNodeBaseIndexAMDX parameter)
    {
        Span<int> span = [parameter.BaseIndex];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.PayloadNodeArraySizeAMDX parameter)
    {
        Span<int> span = [parameter.ArraySize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.SecondaryViewportRelativeNV parameter)
    {
        Span<int> span = [parameter.Offset];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.SIMTCallINTEL parameter)
    {
        Span<int> span = [parameter.N];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.ClobberINTEL parameter)
    {
        Span<int> span = [..parameter.Register.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FuncParamIOKindINTEL parameter)
    {
        Span<int> span = [parameter.Kind];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.GlobalVariableOffsetINTEL parameter)
    {
        Span<int> span = [parameter.Offset];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.CounterBuffer parameter)
    {
        Span<int> span = [parameter.Value];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.UserSemantic parameter)
    {
        Span<int> span = [..parameter.Semantic.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.UserTypeGOOGLE parameter)
    {
        Span<int> span = [..parameter.UserType.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FunctionRoundingModeINTEL parameter)
    {
        Span<int> span = [parameter.TargetWidth, (int)parameter.FPRoundingMode];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FunctionDenormModeINTEL parameter)
    {
        Span<int> span = [parameter.TargetWidth, (int)parameter.FPDenormMode];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MemoryINTEL parameter)
    {
        Span<int> span = [..parameter.MemoryType.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.NumbanksINTEL parameter)
    {
        Span<int> span = [parameter.Banks];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.BankwidthINTEL parameter)
    {
        Span<int> span = [parameter.BankWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MaxPrivateCopiesINTEL parameter)
    {
        Span<int> span = [parameter.MaximumCopies];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MaxReplicatesINTEL parameter)
    {
        Span<int> span = [parameter.MaximumReplicates];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MergeINTEL parameter)
    {
        Span<int> span = [..parameter.MergeKey.AsDisposableLiteralValue().Words, ..parameter.MergeType.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.BankBitsINTEL parameter)
    {
        Span<int> span = [parameter.BankBits];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.ForcePow2DepthINTEL parameter)
    {
        Span<int> span = [parameter.ForceKey];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.StridesizeINTEL parameter)
    {
        Span<int> span = [parameter.StrideSize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.WordsizeINTEL parameter)
    {
        Span<int> span = [parameter.WordSize];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.CacheSizeINTEL parameter)
    {
        Span<int> span = [parameter.CacheSizeinbytes];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.PrefetchINTEL parameter)
    {
        Span<int> span = [parameter.PrefetcherSizeinbytes];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MathOpDSPModeINTEL parameter)
    {
        Span<int> span = [parameter.Mode, parameter.Propagate];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.AliasScopeINTEL parameter)
    {
        Span<int> span = [parameter.AliasingScopesList];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.NoAliasINTEL parameter)
    {
        Span<int> span = [parameter.AliasingScopesList];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.InitiationIntervalINTEL parameter)
    {
        Span<int> span = [parameter.Cycles];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MaxConcurrencyINTEL parameter)
    {
        Span<int> span = [parameter.Invocations];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.PipelineEnableINTEL parameter)
    {
        Span<int> span = [parameter.Enable];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.BufferLocationINTEL parameter)
    {
        Span<int> span = [parameter.BufferLocationID];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.IOPipeStorageINTEL parameter)
    {
        Span<int> span = [parameter.IOPipeID];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FunctionFloatingPointModeINTEL parameter)
    {
        Span<int> span = [parameter.TargetWidth, (int)parameter.FPOperationMode];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FPMaxErrorDecorationINTEL parameter)
    {
        Span<int> span = [BitConverter.SingleToInt32Bits(parameter.MaxError)];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.LatencyControlLabelINTEL parameter)
    {
        Span<int> span = [parameter.LatencyLabel];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.LatencyControlConstraintINTEL parameter)
    {
        Span<int> span = [parameter.RelativeTo, parameter.ControlType, parameter.RelativeCycle];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MMHostInterfaceAddressWidthINTEL parameter)
    {
        Span<int> span = [parameter.AddressWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MMHostInterfaceDataWidthINTEL parameter)
    {
        Span<int> span = [parameter.DataWidth];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MMHostInterfaceLatencyINTEL parameter)
    {
        Span<int> span = [parameter.Latency];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MMHostInterfaceReadWriteModeINTEL parameter)
    {
        Span<int> span = [(int)parameter.ReadWriteMode];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MMHostInterfaceMaxBurstINTEL parameter)
    {
        Span<int> span = [parameter.MaxBurstCount];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.MMHostInterfaceWaitRequestINTEL parameter)
    {
        Span<int> span = [parameter.Waitrequest];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.HostAccessINTEL parameter)
    {
        Span<int> span = [(int)parameter.Access, ..parameter.Name.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.InitModeINTEL parameter)
    {
        Span<int> span = [(int)parameter.Trigger];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.ImplementInRegisterMapINTEL parameter)
    {
        Span<int> span = [parameter.Parameter0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.CacheControlLoadINTEL parameter)
    {
        Span<int> span = [parameter.CacheLevel, (int)parameter.CacheControl];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.CacheControlStoreINTEL parameter)
    {
        Span<int> span = [parameter.CacheLevel, (int)parameter.CacheControl];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.LinkSDSL parameter)
    {
        Span<int> span = [..parameter.Name.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.LinkIdSDSL parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.ColorSDSL parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.ResourceGroupSDSL parameter)
    {
        Span<int> span = [..parameter.ResourceGroup.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.ResourceGroupIdSDSL parameter)
    {
        Span<int> span = [parameter.ResourceGroup];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.LogicalGroupSDSL parameter)
    {
        Span<int> span = [..parameter.LogicalGroup.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.SamplerStateSDSL parameter)
    {
        Span<int> span = [parameter.Parameter0, parameter.Parameter1, parameter.Parameter2, parameter.Parameter3, parameter.Parameter4, parameter.Parameter5, parameter.Parameter6, parameter.Parameter7, parameter.Parameter8, parameter.Parameter9, parameter.Parameter10, parameter.Parameter11, parameter.Parameter12];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(DecorationParams.FunctionParameterDefaultValueSDSL parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(TensorAddressingOperandsParams.TensorView parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters(TensorAddressingOperandsParams.DecodeFunc parameter)
    {
        Span<int> span = [parameter.IdRef0];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, int) tuple)
    {
        Span<int> span = [tuple.Item1, tuple.Item2];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, int, int) tuple)
    {
        Span<int> span = [tuple.Item1, tuple.Item2, tuple.Item3];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((string, LinkageType) tuple)
    {
        Span<int> span = [..tuple.Item1.AsDisposableLiteralValue().Words, (int)tuple.Item2];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, FPRoundingMode) tuple)
    {
        Span<int> span = [tuple.Item1, (int)tuple.Item2];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, FPDenormMode) tuple)
    {
        Span<int> span = [tuple.Item1, (int)tuple.Item2];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((string, string) tuple)
    {
        Span<int> span = [..tuple.Item1.AsDisposableLiteralValue().Words, ..tuple.Item2.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, FPOperationMode) tuple)
    {
        Span<int> span = [tuple.Item1, (int)tuple.Item2];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((HostAccessQualifier, string) tuple)
    {
        Span<int> span = [(int)tuple.Item1, ..tuple.Item2.AsDisposableLiteralValue().Words];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, LoadCacheControl) tuple)
    {
        Span<int> span = [tuple.Item1, (int)tuple.Item2];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, StoreCacheControl) tuple)
    {
        Span<int> span = [tuple.Item1, (int)tuple.Item2];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }

    public static implicit operator EnumerantParameters((int, int, int, int, int, int, int, int, int, int, int, int, int) tuple)
    {
        Span<int> span = [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Item8, tuple.Item9, tuple.Item10, tuple.Item11, tuple.Item12, tuple.Item13];
        MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
        span.CopyTo(buffer.Span);
        var result = new EnumerantParameters(buffer);
        return result;
    }
}