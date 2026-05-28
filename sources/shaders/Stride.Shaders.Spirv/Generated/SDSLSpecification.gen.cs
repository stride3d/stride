namespace Stride.Shaders.Spirv;
public static partial class Specification
{
    public static uint MagicNumber { get; } = 0x07230203;
    public static uint MajorVersion { get; } = 1;
    public static uint MinorVersion { get; } = 6;
    public static uint Revision { get; } = 4;

    [Flags]
    public enum ImageOperandsMask
    {
        None = 0,
        Bias = 1,
        Lod = 2,
        Grad = 4,
        ConstOffset = 8,
        Offset = 16,
        ConstOffsets = 32,
        Sample = 64,
        MinLod = 128,
        MakeTexelAvailable = 256,
        MakeTexelVisible = 512,
        NonPrivateTexel = 1024,
        VolatileTexel = 2048,
        SignExtend = 4096,
        ZeroExtend = 8192,
        Nontemporal = 16384,
        Offsets = 65536,
    }

    [Flags]
    public enum FPFastMathModeMask
    {
        None = 0,
        NotNaN = 1,
        NotInf = 2,
        NSZ = 4,
        AllowRecip = 8,
        Fast = 16,
        AllowContract = 65536,
        AllowReassoc = 131072,
        AllowTransform = 262144,
    }

    [Flags]
    public enum SelectionControlMask
    {
        None = 0,
        Flatten = 1,
        DontFlatten = 2,
    }

    [Flags]
    public enum LoopControlMask
    {
        None = 0,
        Unroll = 1,
        DontUnroll = 2,
        DependencyInfinite = 4,
        DependencyLength = 8,
        MinIterations = 16,
        MaxIterations = 32,
        IterationMultiple = 64,
        PeelCount = 128,
        PartialCount = 256,
        InitiationIntervalINTEL = 65536,
        MaxConcurrencyINTEL = 131072,
        DependencyArrayINTEL = 262144,
        PipelineEnableINTEL = 524288,
        LoopCoalesceINTEL = 1048576,
        MaxInterleavingINTEL = 2097152,
        SpeculatedIterationsINTEL = 4194304,
        NoFusionINTEL = 8388608,
        LoopCountINTEL = 16777216,
        MaxReinvocationDelayINTEL = 33554432,
    }

    [Flags]
    public enum FunctionControlMask
    {
        None = 0,
        Inline = 1,
        DontInline = 2,
        Pure = 4,
        Const = 8,
        OptNoneEXT = 65536,
    }

    [Flags]
    public enum MemorySemanticsMask
    {
        Relaxed = 0,
        Acquire = 2,
        Release = 4,
        AcquireRelease = 8,
        SequentiallyConsistent = 16,
        UniformMemory = 64,
        SubgroupMemory = 128,
        WorkgroupMemory = 256,
        CrossWorkgroupMemory = 512,
        AtomicCounterMemory = 1024,
        ImageMemory = 2048,
        OutputMemory = 4096,
        MakeAvailable = 8192,
        MakeVisible = 16384,
        Volatile = 32768,
    }

    [Flags]
    public enum MemoryAccessMask
    {
        None = 0,
        Volatile = 1,
        Aligned = 2,
        Nontemporal = 4,
        MakePointerAvailable = 8,
        MakePointerVisible = 16,
        NonPrivatePointer = 32,
        AliasScopeINTELMask = 65536,
        NoAliasINTELMask = 131072,
    }

    [Flags]
    public enum KernelProfilingInfoMask
    {
        None = 0,
        CmdExecTime = 1,
    }

    [Flags]
    public enum RayFlagsMask
    {
        NoneKHR = 0,
        OpaqueKHR = 1,
        NoOpaqueKHR = 2,
        TerminateOnFirstHitKHR = 4,
        SkipClosestHitShaderKHR = 8,
        CullBackFacingTrianglesKHR = 16,
        CullFrontFacingTrianglesKHR = 32,
        CullOpaqueKHR = 64,
        CullNoOpaqueKHR = 128,
        SkipTrianglesKHR = 256,
        SkipAABBsKHR = 512,
        ForceOpacityMicromap2StateEXT = 1024,
    }

    [Flags]
    public enum FragmentShadingRateMask
    {
        Vertical2Pixels = 1,
        Vertical4Pixels = 2,
        Horizontal2Pixels = 4,
        Horizontal4Pixels = 8,
    }

    [Flags]
    public enum RawAccessChainOperandsMask
    {
        None = 0,
        RobustnessPerComponentNV = 1,
        RobustnessPerElementNV = 2,
    }

    public enum SourceLanguage
    {
        Unknown = 0,
        ESSL = 1,
        GLSL = 2,
        OpenCL_C = 3,
        OpenCL_CPP = 4,
        HLSL = 5,
        CPP_for_OpenCL = 6,
        SYCL = 7,
        HERO_C = 8,
        NZSL = 9,
        WGSL = 10,
        Slang = 11,
        Zig = 12,
    }

    public enum ExecutionModel
    {
        Vertex = 0,
        TessellationControl = 1,
        TessellationEvaluation = 2,
        Geometry = 3,
        Fragment = 4,
        GLCompute = 5,
        Kernel = 6,
        TaskNV = 5267,
        MeshNV = 5268,
        RayGenerationKHR = 5313,
        IntersectionKHR = 5314,
        AnyHitKHR = 5315,
        ClosestHitKHR = 5316,
        MissKHR = 5317,
        CallableKHR = 5318,
        TaskEXT = 5364,
        MeshEXT = 5365,
        Mixin = 5367,
    }

    public enum AddressingModel
    {
        Logical = 0,
        Physical32 = 1,
        Physical64 = 2,
        PhysicalStorageBuffer64 = 5348,
    }

    public enum MemoryModel
    {
        Simple = 0,
        GLSL450 = 1,
        OpenCL = 2,
        Vulkan = 3,
    }

    public enum ExecutionMode
    {
        Invocations = 0,
        SpacingEqual = 1,
        SpacingFractionalEven = 2,
        SpacingFractionalOdd = 3,
        VertexOrderCw = 4,
        VertexOrderCcw = 5,
        PixelCenterInteger = 6,
        OriginUpperLeft = 7,
        OriginLowerLeft = 8,
        EarlyFragmentTests = 9,
        PointMode = 10,
        Xfb = 11,
        DepthReplacing = 12,
        DepthGreater = 14,
        DepthLess = 15,
        DepthUnchanged = 16,
        LocalSize = 17,
        LocalSizeHint = 18,
        InputPoints = 19,
        InputLines = 20,
        InputLinesAdjacency = 21,
        Triangles = 22,
        InputTrianglesAdjacency = 23,
        Quads = 24,
        Isolines = 25,
        OutputVertices = 26,
        OutputPoints = 27,
        OutputLineStrip = 28,
        OutputTriangleStrip = 29,
        VecTypeHint = 30,
        ContractionOff = 31,
        Initializer = 33,
        Finalizer = 34,
        SubgroupSize = 35,
        SubgroupsPerWorkgroup = 36,
        SubgroupsPerWorkgroupId = 37,
        LocalSizeId = 38,
        LocalSizeHintId = 39,
        NonCoherentColorAttachmentReadEXT = 4169,
        NonCoherentDepthAttachmentReadEXT = 4170,
        NonCoherentStencilAttachmentReadEXT = 4171,
        SubgroupUniformControlFlowKHR = 4421,
        PostDepthCoverage = 4446,
        DenormPreserve = 4459,
        DenormFlushToZero = 4460,
        SignedZeroInfNanPreserve = 4461,
        RoundingModeRTE = 4462,
        RoundingModeRTZ = 4463,
        EarlyAndLateFragmentTestsAMD = 5017,
        StencilRefReplacingEXT = 5027,
        CoalescingAMDX = 5069,
        IsApiEntryAMDX = 5070,
        MaxNodeRecursionAMDX = 5071,
        StaticNumWorkgroupsAMDX = 5072,
        ShaderIndexAMDX = 5073,
        MaxNumWorkgroupsAMDX = 5077,
        StencilRefUnchangedFrontAMD = 5079,
        StencilRefGreaterFrontAMD = 5080,
        StencilRefLessFrontAMD = 5081,
        StencilRefUnchangedBackAMD = 5082,
        StencilRefGreaterBackAMD = 5083,
        StencilRefLessBackAMD = 5084,
        QuadDerivativesKHR = 5088,
        RequireFullQuadsKHR = 5089,
        SharesInputWithAMDX = 5102,
        OutputLinesEXT = 5269,
        OutputPrimitivesEXT = 5270,
        DerivativeGroupQuadsKHR = 5289,
        DerivativeGroupLinearKHR = 5290,
        OutputTrianglesEXT = 5298,
        PixelInterlockOrderedEXT = 5366,
        PixelInterlockUnorderedEXT = 5367,
        SampleInterlockOrderedEXT = 5368,
        SampleInterlockUnorderedEXT = 5369,
        ShadingRateInterlockOrderedEXT = 5370,
        ShadingRateInterlockUnorderedEXT = 5371,
        SharedLocalMemorySizeINTEL = 5618,
        RoundingModeRTPINTEL = 5620,
        RoundingModeRTNINTEL = 5621,
        FloatingPointModeALTINTEL = 5622,
        FloatingPointModeIEEEINTEL = 5623,
        MaxWorkgroupSizeINTEL = 5893,
        MaxWorkDimINTEL = 5894,
        NoGlobalOffsetINTEL = 5895,
        NumSIMDWorkitemsINTEL = 5896,
        SchedulerTargetFmaxMhzINTEL = 5903,
        MaximallyReconvergesKHR = 6023,
        FPFastMathDefault = 6028,
        StreamingInterfaceINTEL = 6154,
        RegisterMapInterfaceINTEL = 6160,
        NamedBarrierCountINTEL = 6417,
        MaximumRegistersINTEL = 6461,
        MaximumRegistersIdINTEL = 6462,
        NamedMaximumRegistersINTEL = 6463,
    }

    public enum StorageClass
    {
        UniformConstant = 0,
        Input = 1,
        Uniform = 2,
        Output = 3,
        Workgroup = 4,
        CrossWorkgroup = 5,
        Private = 6,
        Function = 7,
        Generic = 8,
        PushConstant = 9,
        AtomicCounter = 10,
        Image = 11,
        StorageBuffer = 12,
        TileImageEXT = 4172,
        NodePayloadAMDX = 5068,
        CallableDataKHR = 5328,
        IncomingCallableDataKHR = 5329,
        RayPayloadKHR = 5338,
        HitAttributeKHR = 5339,
        IncomingRayPayloadKHR = 5342,
        ShaderRecordBufferKHR = 5343,
        PhysicalStorageBuffer = 5349,
        HitObjectAttributeNV = 5385,
        TaskPayloadWorkgroupEXT = 5402,
        CodeSectionINTEL = 5605,
        DeviceOnlyINTEL = 5936,
        HostOnlyINTEL = 5937,
        Params = 8000,
    }

    public enum Dim
    {
        Dim1D = 0,
        Dim2D = 1,
        Dim3D = 2,
        Cube = 3,
        Rect = 4,
        Buffer = 5,
        SubpassData = 6,
        TileImageDataEXT = 4173,
    }

    public enum SamplerAddressingMode
    {
        None = 0,
        ClampToEdge = 1,
        Clamp = 2,
        Repeat = 3,
        RepeatMirrored = 4,
    }

    public enum SamplerFilterMode
    {
        Nearest = 0,
        Linear = 1,
    }

    public enum ImageFormat
    {
        Unknown = 0,
        Rgba32f = 1,
        Rgba16f = 2,
        R32f = 3,
        Rgba8 = 4,
        Rgba8Snorm = 5,
        Rg32f = 6,
        Rg16f = 7,
        R11fG11fB10f = 8,
        R16f = 9,
        Rgba16 = 10,
        Rgb10A2 = 11,
        Rg16 = 12,
        Rg8 = 13,
        R16 = 14,
        R8 = 15,
        Rgba16Snorm = 16,
        Rg16Snorm = 17,
        Rg8Snorm = 18,
        R16Snorm = 19,
        R8Snorm = 20,
        Rgba32i = 21,
        Rgba16i = 22,
        Rgba8i = 23,
        R32i = 24,
        Rg32i = 25,
        Rg16i = 26,
        Rg8i = 27,
        R16i = 28,
        R8i = 29,
        Rgba32ui = 30,
        Rgba16ui = 31,
        Rgba8ui = 32,
        R32ui = 33,
        Rgb10a2ui = 34,
        Rg32ui = 35,
        Rg16ui = 36,
        Rg8ui = 37,
        R16ui = 38,
        R8ui = 39,
        R64ui = 40,
        R64i = 41,
    }

    public enum ImageChannelOrder
    {
        R = 0,
        A = 1,
        RG = 2,
        RA = 3,
        RGB = 4,
        RGBA = 5,
        BGRA = 6,
        ARGB = 7,
        Intensity = 8,
        Luminance = 9,
        Rx = 10,
        RGx = 11,
        RGBx = 12,
        Depth = 13,
        DepthStencil = 14,
        sRGB = 15,
        sRGBx = 16,
        sRGBA = 17,
        sBGRA = 18,
        ABGR = 19,
    }

    public enum ImageChannelDataType
    {
        SnormInt8 = 0,
        SnormInt16 = 1,
        UnormInt8 = 2,
        UnormInt16 = 3,
        UnormShort565 = 4,
        UnormShort555 = 5,
        UnormInt101010 = 6,
        SignedInt8 = 7,
        SignedInt16 = 8,
        SignedInt32 = 9,
        UnsignedInt8 = 10,
        UnsignedInt16 = 11,
        UnsignedInt32 = 12,
        HalfFloat = 13,
        Float = 14,
        UnormInt24 = 15,
        UnormInt101010_2 = 16,
        UnsignedIntRaw10EXT = 19,
        UnsignedIntRaw12EXT = 20,
        UnormInt2_101010EXT = 21,
    }

    public enum FPRoundingMode
    {
        RTE = 0,
        RTZ = 1,
        RTP = 2,
        RTN = 3,
    }

    public enum FPDenormMode
    {
        Preserve = 0,
        FlushToZero = 1,
    }

    public enum QuantizationModes
    {
        TRN = 0,
        TRN_ZERO = 1,
        RND = 2,
        RND_ZERO = 3,
        RND_INF = 4,
        RND_MIN_INF = 5,
        RND_CONV = 6,
        RND_CONV_ODD = 7,
    }

    public enum FPOperationMode
    {
        IEEE = 0,
        ALT = 1,
    }

    public enum OverflowModes
    {
        WRAP = 0,
        SAT = 1,
        SAT_ZERO = 2,
        SAT_SYM = 3,
    }

    public enum LinkageType
    {
        Export = 0,
        Import = 1,
        LinkOnceODR = 2,
    }

    public enum AccessQualifier
    {
        ReadOnly = 0,
        WriteOnly = 1,
        ReadWrite = 2,
    }

    public enum HostAccessQualifier
    {
        NoneINTEL = 0,
        ReadINTEL = 1,
        WriteINTEL = 2,
        ReadWriteINTEL = 3,
    }

    public enum FunctionParameterAttribute
    {
        Zext = 0,
        Sext = 1,
        ByVal = 2,
        Sret = 3,
        NoAlias = 4,
        NoCapture = 5,
        NoWrite = 6,
        NoReadWrite = 7,
        RuntimeAlignedINTEL = 5940,
    }

    public enum Decoration
    {
        RelaxedPrecision = 0,
        SpecId = 1,
        Block = 2,
        BufferBlock = 3,
        RowMajor = 4,
        ColMajor = 5,
        ArrayStride = 6,
        MatrixStride = 7,
        GLSLShared = 8,
        GLSLPacked = 9,
        CPacked = 10,
        BuiltIn = 11,
        NoPerspective = 13,
        Flat = 14,
        Patch = 15,
        Centroid = 16,
        Sample = 17,
        Invariant = 18,
        Restrict = 19,
        Aliased = 20,
        Volatile = 21,
        Constant = 22,
        Coherent = 23,
        NonWritable = 24,
        NonReadable = 25,
        Uniform = 26,
        UniformId = 27,
        SaturatedConversion = 28,
        Stream = 29,
        Location = 30,
        Component = 31,
        Index = 32,
        Binding = 33,
        DescriptorSet = 34,
        Offset = 35,
        XfbBuffer = 36,
        XfbStride = 37,
        FuncParamAttr = 38,
        FPRoundingMode = 39,
        FPFastMathMode = 40,
        LinkageAttributes = 41,
        NoContraction = 42,
        InputAttachmentIndex = 43,
        Alignment = 44,
        MaxByteOffset = 45,
        AlignmentId = 46,
        MaxByteOffsetId = 47,
        NoSignedWrap = 4469,
        NoUnsignedWrap = 4470,
        WeightTextureQCOM = 4487,
        BlockMatchTextureQCOM = 4488,
        BlockMatchSamplerQCOM = 4499,
        ExplicitInterpAMD = 4999,
        NodeSharesPayloadLimitsWithAMDX = 5019,
        NodeMaxPayloadsAMDX = 5020,
        TrackFinishWritingAMDX = 5078,
        PayloadNodeNameAMDX = 5091,
        PayloadNodeBaseIndexAMDX = 5098,
        PayloadNodeSparseArrayAMDX = 5099,
        PayloadNodeArraySizeAMDX = 5100,
        PayloadDispatchIndirectAMDX = 5105,
        OverrideCoverageNV = 5248,
        PassthroughNV = 5250,
        ViewportRelativeNV = 5252,
        SecondaryViewportRelativeNV = 5256,
        PerPrimitiveEXT = 5271,
        PerViewNV = 5272,
        PerTaskNV = 5273,
        PerVertexKHR = 5285,
        NonUniform = 5300,
        RestrictPointer = 5355,
        AliasedPointer = 5356,
        HitObjectShaderRecordBufferNV = 5386,
        BindlessSamplerNV = 5398,
        BindlessImageNV = 5399,
        BoundSamplerNV = 5400,
        BoundImageNV = 5401,
        SIMTCallINTEL = 5599,
        ReferencedIndirectlyINTEL = 5602,
        ClobberINTEL = 5607,
        SideEffectsINTEL = 5608,
        VectorComputeVariableINTEL = 5624,
        FuncParamIOKindINTEL = 5625,
        VectorComputeFunctionINTEL = 5626,
        StackCallINTEL = 5627,
        GlobalVariableOffsetINTEL = 5628,
        CounterBuffer = 5634,
        UserSemantic = 5635,
        UserTypeGOOGLE = 5636,
        FunctionRoundingModeINTEL = 5822,
        FunctionDenormModeINTEL = 5823,
        RegisterINTEL = 5825,
        MemoryINTEL = 5826,
        NumbanksINTEL = 5827,
        BankwidthINTEL = 5828,
        MaxPrivateCopiesINTEL = 5829,
        SinglepumpINTEL = 5830,
        DoublepumpINTEL = 5831,
        MaxReplicatesINTEL = 5832,
        SimpleDualPortINTEL = 5833,
        MergeINTEL = 5834,
        BankBitsINTEL = 5835,
        ForcePow2DepthINTEL = 5836,
        StridesizeINTEL = 5883,
        WordsizeINTEL = 5884,
        TrueDualPortINTEL = 5885,
        BurstCoalesceINTEL = 5899,
        CacheSizeINTEL = 5900,
        DontStaticallyCoalesceINTEL = 5901,
        PrefetchINTEL = 5902,
        StallEnableINTEL = 5905,
        FuseLoopsInFunctionINTEL = 5907,
        MathOpDSPModeINTEL = 5909,
        AliasScopeINTEL = 5914,
        NoAliasINTEL = 5915,
        InitiationIntervalINTEL = 5917,
        MaxConcurrencyINTEL = 5918,
        PipelineEnableINTEL = 5919,
        BufferLocationINTEL = 5921,
        IOPipeStorageINTEL = 5944,
        FunctionFloatingPointModeINTEL = 6080,
        SingleElementVectorINTEL = 6085,
        VectorComputeCallableFunctionINTEL = 6087,
        MediaBlockIOINTEL = 6140,
        StallFreeINTEL = 6151,
        FPMaxErrorDecorationINTEL = 6170,
        LatencyControlLabelINTEL = 6172,
        LatencyControlConstraintINTEL = 6173,
        ConduitKernelArgumentINTEL = 6175,
        RegisterMapKernelArgumentINTEL = 6176,
        MMHostInterfaceAddressWidthINTEL = 6177,
        MMHostInterfaceDataWidthINTEL = 6178,
        MMHostInterfaceLatencyINTEL = 6179,
        MMHostInterfaceReadWriteModeINTEL = 6180,
        MMHostInterfaceMaxBurstINTEL = 6181,
        MMHostInterfaceWaitRequestINTEL = 6182,
        StableKernelArgumentINTEL = 6183,
        HostAccessINTEL = 6188,
        InitModeINTEL = 6190,
        ImplementInRegisterMapINTEL = 6191,
        CacheControlLoadINTEL = 6442,
        CacheControlStoreINTEL = 6443,
        LinkSDSL = 8000,
        LinkIdSDSL = 8001,
        ColorSDSL = 8002,
        ResourceGroupSDSL = 8010,
        ResourceGroupIdSDSL = 8011,
        LogicalGroupSDSL = 8004,
        SamplerStateSDSL = 8020,
        FunctionParameterDefaultValueSDSL = 8040,
        ShaderConstantSDSL = 8060,
        PatchConstantFuncSDSL = 8070,
    }

    public enum BuiltIn
    {
        Position = 0,
        PointSize = 1,
        ClipDistance = 3,
        CullDistance = 4,
        VertexId = 5,
        InstanceId = 6,
        PrimitiveId = 7,
        InvocationId = 8,
        Layer = 9,
        ViewportIndex = 10,
        TessLevelOuter = 11,
        TessLevelInner = 12,
        TessCoord = 13,
        PatchVertices = 14,
        FragCoord = 15,
        PointCoord = 16,
        FrontFacing = 17,
        SampleId = 18,
        SamplePosition = 19,
        SampleMask = 20,
        FragDepth = 22,
        HelperInvocation = 23,
        NumWorkgroups = 24,
        WorkgroupSize = 25,
        WorkgroupId = 26,
        LocalInvocationId = 27,
        GlobalInvocationId = 28,
        LocalInvocationIndex = 29,
        WorkDim = 30,
        GlobalSize = 31,
        EnqueuedWorkgroupSize = 32,
        GlobalOffset = 33,
        GlobalLinearId = 34,
        SubgroupSize = 36,
        SubgroupMaxSize = 37,
        NumSubgroups = 38,
        NumEnqueuedSubgroups = 39,
        SubgroupId = 40,
        SubgroupLocalInvocationId = 41,
        VertexIndex = 42,
        InstanceIndex = 43,
        CoreIDARM = 4160,
        CoreCountARM = 4161,
        CoreMaxIDARM = 4162,
        WarpIDARM = 4163,
        WarpMaxIDARM = 4164,
        SubgroupEqMask = 4416,
        SubgroupGeMask = 4417,
        SubgroupGtMask = 4418,
        SubgroupLeMask = 4419,
        SubgroupLtMask = 4420,
        BaseVertex = 4424,
        BaseInstance = 4425,
        DrawIndex = 4426,
        PrimitiveShadingRateKHR = 4432,
        DeviceIndex = 4438,
        ViewIndex = 4440,
        ShadingRateKHR = 4444,
        BaryCoordNoPerspAMD = 4992,
        BaryCoordNoPerspCentroidAMD = 4993,
        BaryCoordNoPerspSampleAMD = 4994,
        BaryCoordSmoothAMD = 4995,
        BaryCoordSmoothCentroidAMD = 4996,
        BaryCoordSmoothSampleAMD = 4997,
        BaryCoordPullModelAMD = 4998,
        FragStencilRefEXT = 5014,
        RemainingRecursionLevelsAMDX = 5021,
        ShaderIndexAMDX = 5073,
        ViewportMaskNV = 5253,
        SecondaryPositionNV = 5257,
        SecondaryViewportMaskNV = 5258,
        PositionPerViewNV = 5261,
        ViewportMaskPerViewNV = 5262,
        FullyCoveredEXT = 5264,
        TaskCountNV = 5274,
        PrimitiveCountNV = 5275,
        PrimitiveIndicesNV = 5276,
        ClipDistancePerViewNV = 5277,
        CullDistancePerViewNV = 5278,
        LayerPerViewNV = 5279,
        MeshViewCountNV = 5280,
        MeshViewIndicesNV = 5281,
        BaryCoordKHR = 5286,
        BaryCoordNoPerspKHR = 5287,
        FragSizeEXT = 5292,
        FragInvocationCountEXT = 5293,
        PrimitivePointIndicesEXT = 5294,
        PrimitiveLineIndicesEXT = 5295,
        PrimitiveTriangleIndicesEXT = 5296,
        CullPrimitiveEXT = 5299,
        LaunchIdKHR = 5319,
        LaunchSizeKHR = 5320,
        WorldRayOriginKHR = 5321,
        WorldRayDirectionKHR = 5322,
        ObjectRayOriginKHR = 5323,
        ObjectRayDirectionKHR = 5324,
        RayTminKHR = 5325,
        RayTmaxKHR = 5326,
        InstanceCustomIndexKHR = 5327,
        ObjectToWorldKHR = 5330,
        WorldToObjectKHR = 5331,
        HitTNV = 5332,
        HitKindKHR = 5333,
        CurrentRayTimeNV = 5334,
        HitTriangleVertexPositionsKHR = 5335,
        HitMicroTriangleVertexPositionsNV = 5337,
        HitMicroTriangleVertexBarycentricsNV = 5344,
        IncomingRayFlagsKHR = 5351,
        RayGeometryIndexKHR = 5352,
        WarpsPerSMNV = 5374,
        SMCountNV = 5375,
        WarpIDNV = 5376,
        SMIDNV = 5377,
        HitKindFrontFacingMicroTriangleNV = 5405,
        HitKindBackFacingMicroTriangleNV = 5406,
        CullMaskKHR = 6021,
    }

    public enum Scope
    {
        CrossDevice = 0,
        Device = 1,
        Workgroup = 2,
        Subgroup = 3,
        Invocation = 4,
        QueueFamily = 5,
        ShaderCallKHR = 6,
    }

    public enum GroupOperation
    {
        Reduce = 0,
        InclusiveScan = 1,
        ExclusiveScan = 2,
        ClusteredReduce = 3,
        PartitionedReduceNV = 6,
        PartitionedInclusiveScanNV = 7,
        PartitionedExclusiveScanNV = 8,
    }

    public enum KernelEnqueueFlags
    {
        NoWait = 0,
        WaitKernel = 1,
        WaitWorkGroup = 2,
    }

    public enum Capability
    {
        Matrix = 0,
        Shader = 1,
        Geometry = 2,
        Tessellation = 3,
        Addresses = 4,
        Linkage = 5,
        Kernel = 6,
        Vector16 = 7,
        Float16Buffer = 8,
        Float16 = 9,
        Float64 = 10,
        Int64 = 11,
        Int64Atomics = 12,
        ImageBasic = 13,
        ImageReadWrite = 14,
        ImageMipmap = 15,
        Pipes = 17,
        Groups = 18,
        DeviceEnqueue = 19,
        LiteralSampler = 20,
        AtomicStorage = 21,
        Int16 = 22,
        TessellationPointSize = 23,
        GeometryPointSize = 24,
        ImageGatherExtended = 25,
        StorageImageMultisample = 27,
        UniformBufferArrayDynamicIndexing = 28,
        SampledImageArrayDynamicIndexing = 29,
        StorageBufferArrayDynamicIndexing = 30,
        StorageImageArrayDynamicIndexing = 31,
        ClipDistance = 32,
        CullDistance = 33,
        ImageCubeArray = 34,
        SampleRateShading = 35,
        ImageRect = 36,
        SampledRect = 37,
        GenericPointer = 38,
        Int8 = 39,
        InputAttachment = 40,
        SparseResidency = 41,
        MinLod = 42,
        Sampled1D = 43,
        Image1D = 44,
        SampledCubeArray = 45,
        SampledBuffer = 46,
        ImageBuffer = 47,
        ImageMSArray = 48,
        StorageImageExtendedFormats = 49,
        ImageQuery = 50,
        DerivativeControl = 51,
        InterpolationFunction = 52,
        TransformFeedback = 53,
        GeometryStreams = 54,
        StorageImageReadWithoutFormat = 55,
        StorageImageWriteWithoutFormat = 56,
        MultiViewport = 57,
        SubgroupDispatch = 58,
        NamedBarrier = 59,
        PipeStorage = 60,
        GroupNonUniform = 61,
        GroupNonUniformVote = 62,
        GroupNonUniformArithmetic = 63,
        GroupNonUniformBallot = 64,
        GroupNonUniformShuffle = 65,
        GroupNonUniformShuffleRelative = 66,
        GroupNonUniformClustered = 67,
        GroupNonUniformQuad = 68,
        ShaderLayer = 69,
        ShaderViewportIndex = 70,
        UniformDecoration = 71,
        CoreBuiltinsARM = 4165,
        TileImageColorReadAccessEXT = 4166,
        TileImageDepthReadAccessEXT = 4167,
        TileImageStencilReadAccessEXT = 4168,
        CooperativeMatrixLayoutsARM = 4201,
        FragmentShadingRateKHR = 4422,
        SubgroupBallotKHR = 4423,
        DrawParameters = 4427,
        WorkgroupMemoryExplicitLayoutKHR = 4428,
        WorkgroupMemoryExplicitLayout8BitAccessKHR = 4429,
        WorkgroupMemoryExplicitLayout16BitAccessKHR = 4430,
        SubgroupVoteKHR = 4431,
        StorageBuffer16BitAccess = 4433,
        UniformAndStorageBuffer16BitAccess = 4434,
        StoragePushConstant16 = 4435,
        StorageInputOutput16 = 4436,
        DeviceGroup = 4437,
        MultiView = 4439,
        VariablePointersStorageBuffer = 4441,
        VariablePointers = 4442,
        AtomicStorageOps = 4445,
        SampleMaskPostDepthCoverage = 4447,
        StorageBuffer8BitAccess = 4448,
        UniformAndStorageBuffer8BitAccess = 4449,
        StoragePushConstant8 = 4450,
        DenormPreserve = 4464,
        DenormFlushToZero = 4465,
        SignedZeroInfNanPreserve = 4466,
        RoundingModeRTE = 4467,
        RoundingModeRTZ = 4468,
        RayQueryProvisionalKHR = 4471,
        RayQueryKHR = 4472,
        UntypedPointersKHR = 4473,
        RayTraversalPrimitiveCullingKHR = 4478,
        RayTracingKHR = 4479,
        TextureSampleWeightedQCOM = 4484,
        TextureBoxFilterQCOM = 4485,
        TextureBlockMatchQCOM = 4486,
        TextureBlockMatch2QCOM = 4498,
        Float16ImageAMD = 5008,
        ImageGatherBiasLodAMD = 5009,
        FragmentMaskAMD = 5010,
        StencilExportEXT = 5013,
        ImageReadWriteLodAMD = 5015,
        Int64ImageEXT = 5016,
        ShaderClockKHR = 5055,
        ShaderEnqueueAMDX = 5067,
        QuadControlKHR = 5087,
        SampleMaskOverrideCoverageNV = 5249,
        GeometryShaderPassthroughNV = 5251,
        ShaderViewportIndexLayerEXT = 5254,
        ShaderViewportMaskNV = 5255,
        ShaderStereoViewNV = 5259,
        PerViewAttributesNV = 5260,
        FragmentFullyCoveredEXT = 5265,
        MeshShadingNV = 5266,
        ImageFootprintNV = 5282,
        MeshShadingEXT = 5283,
        FragmentBarycentricKHR = 5284,
        ComputeDerivativeGroupQuadsKHR = 5288,
        FragmentDensityEXT = 5291,
        GroupNonUniformPartitionedNV = 5297,
        ShaderNonUniform = 5301,
        RuntimeDescriptorArray = 5302,
        InputAttachmentArrayDynamicIndexing = 5303,
        UniformTexelBufferArrayDynamicIndexing = 5304,
        StorageTexelBufferArrayDynamicIndexing = 5305,
        UniformBufferArrayNonUniformIndexing = 5306,
        SampledImageArrayNonUniformIndexing = 5307,
        StorageBufferArrayNonUniformIndexing = 5308,
        StorageImageArrayNonUniformIndexing = 5309,
        InputAttachmentArrayNonUniformIndexing = 5310,
        UniformTexelBufferArrayNonUniformIndexing = 5311,
        StorageTexelBufferArrayNonUniformIndexing = 5312,
        RayTracingPositionFetchKHR = 5336,
        RayTracingNV = 5340,
        RayTracingMotionBlurNV = 5341,
        VulkanMemoryModel = 5345,
        VulkanMemoryModelDeviceScope = 5346,
        PhysicalStorageBufferAddresses = 5347,
        ComputeDerivativeGroupLinearKHR = 5350,
        RayTracingProvisionalKHR = 5353,
        CooperativeMatrixNV = 5357,
        FragmentShaderSampleInterlockEXT = 5363,
        FragmentShaderShadingRateInterlockEXT = 5372,
        ShaderSMBuiltinsNV = 5373,
        FragmentShaderPixelInterlockEXT = 5378,
        DemoteToHelperInvocation = 5379,
        DisplacementMicromapNV = 5380,
        RayTracingOpacityMicromapEXT = 5381,
        ShaderInvocationReorderNV = 5383,
        BindlessTextureNV = 5390,
        RayQueryPositionFetchKHR = 5391,
        AtomicFloat16VectorNV = 5404,
        RayTracingDisplacementMicromapNV = 5409,
        RawAccessChainsNV = 5414,
        CooperativeMatrixReductionsNV = 5430,
        CooperativeMatrixConversionsNV = 5431,
        CooperativeMatrixPerElementOperationsNV = 5432,
        CooperativeMatrixTensorAddressingNV = 5433,
        CooperativeMatrixBlockLoadsNV = 5434,
        TensorAddressingNV = 5439,
        SubgroupShuffleINTEL = 5568,
        SubgroupBufferBlockIOINTEL = 5569,
        SubgroupImageBlockIOINTEL = 5570,
        SubgroupImageMediaBlockIOINTEL = 5579,
        RoundToInfinityINTEL = 5582,
        FloatingPointModeINTEL = 5583,
        IntegerFunctions2INTEL = 5584,
        FunctionPointersINTEL = 5603,
        IndirectReferencesINTEL = 5604,
        AsmINTEL = 5606,
        AtomicFloat32MinMaxEXT = 5612,
        AtomicFloat64MinMaxEXT = 5613,
        AtomicFloat16MinMaxEXT = 5616,
        VectorComputeINTEL = 5617,
        VectorAnyINTEL = 5619,
        ExpectAssumeKHR = 5629,
        SubgroupAvcMotionEstimationINTEL = 5696,
        SubgroupAvcMotionEstimationIntraINTEL = 5697,
        SubgroupAvcMotionEstimationChromaINTEL = 5698,
        VariableLengthArrayINTEL = 5817,
        FunctionFloatControlINTEL = 5821,
        FPGAMemoryAttributesINTEL = 5824,
        FPFastMathModeINTEL = 5837,
        ArbitraryPrecisionIntegersINTEL = 5844,
        ArbitraryPrecisionFloatingPointINTEL = 5845,
        UnstructuredLoopControlsINTEL = 5886,
        FPGALoopControlsINTEL = 5888,
        KernelAttributesINTEL = 5892,
        FPGAKernelAttributesINTEL = 5897,
        FPGAMemoryAccessesINTEL = 5898,
        FPGAClusterAttributesINTEL = 5904,
        LoopFuseINTEL = 5906,
        FPGADSPControlINTEL = 5908,
        MemoryAccessAliasingINTEL = 5910,
        FPGAInvocationPipeliningAttributesINTEL = 5916,
        FPGABufferLocationINTEL = 5920,
        ArbitraryPrecisionFixedPointINTEL = 5922,
        USMStorageClassesINTEL = 5935,
        RuntimeAlignedAttributeINTEL = 5939,
        IOPipesINTEL = 5943,
        BlockingPipesINTEL = 5945,
        FPGARegINTEL = 5948,
        DotProductInputAll = 6016,
        DotProductInput4x8Bit = 6017,
        DotProductInput4x8BitPacked = 6018,
        DotProduct = 6019,
        RayCullMaskKHR = 6020,
        CooperativeMatrixKHR = 6022,
        ReplicatedCompositesEXT = 6024,
        BitInstructions = 6025,
        GroupNonUniformRotateKHR = 6026,
        FloatControls2 = 6029,
        AtomicFloat32AddEXT = 6033,
        AtomicFloat64AddEXT = 6034,
        LongCompositesINTEL = 6089,
        OptNoneEXT = 6094,
        AtomicFloat16AddEXT = 6095,
        DebugInfoModuleINTEL = 6114,
        BFloat16ConversionINTEL = 6115,
        SplitBarrierINTEL = 6141,
        ArithmeticFenceEXT = 6144,
        FPGAClusterAttributesV2INTEL = 6150,
        FPGAKernelAttributesv2INTEL = 6161,
        FPMaxErrorINTEL = 6169,
        FPGALatencyControlINTEL = 6171,
        FPGAArgumentInterfacesINTEL = 6174,
        GlobalVariableHostAccessINTEL = 6187,
        GlobalVariableFPGADecorationsINTEL = 6189,
        SubgroupBufferPrefetchINTEL = 6220,
        GroupUniformArithmeticKHR = 6400,
        MaskedGatherScatterINTEL = 6427,
        CacheControlsINTEL = 6441,
        RegisterLimitsINTEL = 6460,
    }

    public enum RayQueryIntersection
    {
        RayQueryCandidateIntersectionKHR = 0,
        RayQueryCommittedIntersectionKHR = 1,
    }

    public enum RayQueryCommittedIntersectionType
    {
        RayQueryCommittedIntersectionNoneKHR = 0,
        RayQueryCommittedIntersectionTriangleKHR = 1,
        RayQueryCommittedIntersectionGeneratedKHR = 2,
    }

    public enum RayQueryCandidateIntersectionType
    {
        RayQueryCandidateIntersectionTriangleKHR = 0,
        RayQueryCandidateIntersectionAABBKHR = 1,
    }

    public enum PackedVectorFormat
    {
        PackedVectorFormat4x8Bit = 0,
    }

    [Flags]
    public enum CooperativeMatrixOperandsMask
    {
        NoneKHR = 0,
        MatrixASignedComponentsKHR = 1,
        MatrixBSignedComponentsKHR = 2,
        MatrixCSignedComponentsKHR = 4,
        MatrixResultSignedComponentsKHR = 8,
        SaturatingAccumulationKHR = 16,
    }

    public enum CooperativeMatrixLayout
    {
        RowMajorKHR = 0,
        ColumnMajorKHR = 1,
        RowBlockedInterleavedARM = 4202,
        ColumnBlockedInterleavedARM = 4203,
    }

    public enum CooperativeMatrixUse
    {
        MatrixAKHR = 0,
        MatrixBKHR = 1,
        MatrixAccumulatorKHR = 2,
    }

    [Flags]
    public enum CooperativeMatrixReduceMask
    {
        Row = 1,
        Column = 2,
        CooperativeMatrixReduce2x2 = 4,
    }

    public enum TensorClampMode
    {
        Undefined = 0,
        Constant = 1,
        ClampToEdge = 2,
        Repeat = 3,
        RepeatMirrored = 4,
    }

    [Flags]
    public enum TensorAddressingOperandsMask
    {
        None = 0,
        TensorView = 1,
        DecodeFunc = 2,
    }

    public enum InitializationModeQualifier
    {
        InitOnDeviceReprogramINTEL = 0,
        InitOnDeviceResetINTEL = 1,
    }

    public enum LoadCacheControl
    {
        UncachedINTEL = 0,
        CachedINTEL = 1,
        StreamingINTEL = 2,
        InvalidateAfterReadINTEL = 3,
        ConstCachedINTEL = 4,
    }

    public enum StoreCacheControl
    {
        UncachedINTEL = 0,
        WriteThroughINTEL = 1,
        WriteBackINTEL = 2,
        StreamingINTEL = 3,
    }

    public enum NamedMaximumNumberOfRegisters
    {
        AutoINTEL = 0,
    }

    public enum FPEncoding
    {
    }

    [Flags]
    public enum MixinInheritFlagsMask
    {
        None = 0,
        NeedsFullImport = 1,
    }

    [Flags]
    public enum FunctionFlagsMask
    {
        None = 0,
        Stage = 1,
        Abstract = 16,
        Virtual = 32,
        Override = 64,
        ReferencesNonStage = 128,
    }

    [Flags]
    public enum VariableFlagsMask
    {
        None = 0,
        Stage = 1,
        Stream = 2,
    }

    public enum GenericParameterKindSDSL
    {
        LinkType = 1,
        Semantic = 2,
        MemberName = 3,
        MemberNameResolved = 4,
    }

    public enum StreamsKindSDSL
    {
        Input = 1,
        Streams = 2,
        Output = 3,
        Constants = 4,
    }

    public enum GeometryStreamOutputKindSDSL
    {
        Point = 1,
        Line = 2,
        Triangle = 3,
    }

    public enum PatchTypeKindSDSL
    {
        Input = 1,
        Output = 2,
    }

    public enum SamplerTextureAddressModeSDSL
    {
        Wrap = 1,
        Mirror = 2,
        Clamp = 3,
        Border = 4,
        MirrorOnce = 5,
    }

    public enum SamplerFilterSDSL
    {
        MIN_MAG_MIP_POINT = 0,
        MIN_MAG_POINT_MIP_LINEAR = 1,
        MIN_POINT_MAG_LINEAR_MIP_POINT = 4,
        MIN_POINT_MAG_MIP_LINEAR = 5,
        MIN_LINEAR_MAG_MIP_POINT = 16,
        MIN_LINEAR_MAG_POINT_MIP_LINEAR = 17,
        MIN_MAG_LINEAR_MIP_POINT = 20,
        MIN_MAG_MIP_LINEAR = 21,
        ANISOTROPIC = 85,
        COMPARISON_MIN_MAG_MIP_POINT = 128,
        COMPARISON_MIN_MAG_POINT_MIP_LINEAR = 129,
        COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT = 132,
        COMPARISON_MIN_POINT_MAG_MIP_LINEAR = 133,
        COMPARISON_MIN_LINEAR_MAG_MIP_POINT = 144,
        COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 145,
        COMPARISON_MIN_MAG_LINEAR_MIP_POINT = 148,
        COMPARISON_MIN_MAG_MIP_LINEAR = 149,
        COMPARISON_ANISOTROPIC = 213,
        MINIMUM_MIN_MAG_MIP_POINT = 256,
        MINIMUM_MIN_MAG_POINT_MIP_LINEAR = 257,
        MINIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 260,
        MINIMUM_MIN_POINT_MAG_MIP_LINEAR = 261,
        MINIMUM_MIN_LINEAR_MAG_MIP_POINT = 272,
        MINIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 273,
        MINIMUM_MIN_MAG_LINEAR_MIP_POINT = 276,
        MINIMUM_MIN_MAG_MIP_LINEAR = 277,
        MINIMUM_ANISOTROPIC = 341,
        MAXIMUM_MIN_MAG_MIP_POINT = 384,
        MAXIMUM_MIN_MAG_POINT_MIP_LINEAR = 385,
        MAXIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 388,
        MAXIMUM_MIN_POINT_MAG_MIP_LINEAR = 389,
        MAXIMUM_MIN_LINEAR_MAG_MIP_POINT = 400,
        MAXIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 401,
        MAXIMUM_MIN_MAG_LINEAR_MIP_POINT = 404,
        MAXIMUM_MIN_MAG_MIP_LINEAR = 405,
        MAXIMUM_ANISOTROPIC = 469,
    }

    public enum SamplerComparisonFuncSDSL
    {
        Never = 1,
        Less = 2,
        Equal = 3,
        LessEqual = 4,
        Greater = 5,
        NotEqual = 6,
        GreaterEqual = 7,
        Always = 8,
    }

    public enum MixinKindSDFX
    {
        Default = 0,
        ComposeSet = 1,
        ComposeAdd = 2,
        Child = 3,
        Clone = 4,
        Remove = 5,
        Macro = 6,
    }
}