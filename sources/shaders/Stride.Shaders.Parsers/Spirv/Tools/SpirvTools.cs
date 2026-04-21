using System.Runtime.InteropServices;

namespace Stride.Shaders.Spirv.Tools;

/// <summary>
/// Hand-rolled P/Invoke surface for the SPIRV-Tools validator and optimizer,
/// backed by the Stride.Dependencies.SPIRVTools native payload.
/// <para>
/// Validator bindings are the stable C API from <c>spirv-tools/libspirv.h</c>. Optimizer
/// bindings target the <c>stride_spv*</c> C shim that wraps <c>spvtools::Optimizer</c>
/// — upstream's <c>optimizer.hpp</c> is C++ only. Both live in the single combined
/// <c>stride_spirv_tools</c> library; see <c>build/deps/spirv-tools/</c>.
/// </para>
/// </summary>
public static unsafe class SpirvTools
{
    const string Lib = "stride_spirv_tools";

    public enum TargetEnv
    {
        Universal_1_0 = 0,
        Vulkan_1_0 = 1,
        Universal_1_1 = 2,
        Universal_1_2 = 11,
        Universal_1_3 = 18,
        Vulkan_1_1 = 19,
        Universal_1_4 = 21,
        Vulkan_1_1_SpirV_1_4 = 22,
        Universal_1_5 = 23,
        Vulkan_1_2 = 24,
        Universal_1_6 = 25,
        Vulkan_1_3 = 26,
        Vulkan_1_4 = 27,
    }

    public enum Result
    {
        Success = 0,
        Unsupported = 1,
        EndOfStream = 2,
        Warning = 3,
        FailedMatch = 4,
        RequestedTermination = 5,
        InternalError = -1,
        OutOfMemory = -2,
        InvalidPointer = -3,
        InvalidBinary = -4,
        InvalidText = -5,
        InvalidTable = -6,
        InvalidValue = -7,
        InvalidDiagnostic = -8,
        InvalidLookup = -9,
        InvalidId = -10,
        InvalidCfg = -11,
        InvalidLayout = -12,
        InvalidCapability = -13,
        InvalidData = -14,
        MissingExtension = -15,
        WrongVersion = -16,
    }

    [Flags]
    public enum ValidatorOptions
    {
        None = 0,
        /// <summary>Relax block-layout rules (Vulkan VK_KHR_relaxed_block_layout equivalent).</summary>
        RelaxBlockLayout = 1 << 0,
        /// <summary>Allow UBO standard layout (Vulkan VK_KHR_uniform_buffer_standard_layout equivalent).</summary>
        UniformBufferStandardLayout = 1 << 1,
        /// <summary>Allow scalar block layout (Vulkan VK_EXT_scalar_block_layout equivalent).</summary>
        ScalarBlockLayout = 1 << 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Position { public nuint Line; public nuint Column; public nuint Index; }

    [StructLayout(LayoutKind.Sequential)]
    struct Diagnostic { public Position Position; public byte* Error; public byte IsTextSource; }

    // ---- Core / validator (spirv-tools/libspirv.h) -------------------------
    [DllImport(Lib, EntryPoint = "spvContextCreate")]
    static extern IntPtr ContextCreate(TargetEnv env);

    [DllImport(Lib, EntryPoint = "spvContextDestroy")]
    static extern void ContextDestroy(IntPtr context);

    [DllImport(Lib, EntryPoint = "spvValidateBinary")]
    static extern Result ValidateBinary(IntPtr context, uint* code, nuint wordCount, Diagnostic** diagnostic);

    [DllImport(Lib, EntryPoint = "spvValidateWithOptions")]
    static extern Result ValidateWithOptions(IntPtr context, IntPtr options, ConstBinary* binary, Diagnostic** diagnostic);

    [DllImport(Lib, EntryPoint = "spvDiagnosticDestroy")]
    static extern void DiagnosticDestroy(Diagnostic* diagnostic);

    [DllImport(Lib, EntryPoint = "spvValidatorOptionsCreate")]
    static extern IntPtr ValidatorOptionsCreate();

    [DllImport(Lib, EntryPoint = "spvValidatorOptionsDestroy")]
    static extern void ValidatorOptionsDestroy(IntPtr options);

    [DllImport(Lib, EntryPoint = "spvValidatorOptionsSetRelaxBlockLayout")]
    static extern void ValidatorOptionsSetRelaxBlockLayout(IntPtr options, byte val);

    [DllImport(Lib, EntryPoint = "spvValidatorOptionsSetUniformBufferStandardLayout")]
    static extern void ValidatorOptionsSetUniformBufferStandardLayout(IntPtr options, byte val);

    [DllImport(Lib, EntryPoint = "spvValidatorOptionsSetScalarBlockLayout")]
    static extern void ValidatorOptionsSetScalarBlockLayout(IntPtr options, byte val);

    [StructLayout(LayoutKind.Sequential)]
    struct ConstBinary { public uint* Code; public nuint WordCount; }

    /// <summary>
    /// Validates a SPIR-V binary. Returns null on success; otherwise a diagnostic message.
    /// </summary>
    public static string? Validate(ReadOnlySpan<uint> words, TargetEnv env = TargetEnv.Vulkan_1_3, ValidatorOptions options = ValidatorOptions.None)
    {
        var ctx = ContextCreate(env);
        if (ctx == IntPtr.Zero)
            throw new InvalidOperationException("spvContextCreate failed");
        try
        {
            IntPtr opts = IntPtr.Zero;
            if (options != ValidatorOptions.None)
            {
                opts = ValidatorOptionsCreate();
                if ((options & ValidatorOptions.RelaxBlockLayout) != 0)
                    ValidatorOptionsSetRelaxBlockLayout(opts, 1);
                if ((options & ValidatorOptions.UniformBufferStandardLayout) != 0)
                    ValidatorOptionsSetUniformBufferStandardLayout(opts, 1);
                if ((options & ValidatorOptions.ScalarBlockLayout) != 0)
                    ValidatorOptionsSetScalarBlockLayout(opts, 1);
            }

            try
            {
                Diagnostic* diag = null;
                fixed (uint* code = words)
                {
                    Result r;
                    if (opts == IntPtr.Zero)
                    {
                        r = ValidateBinary(ctx, code, (nuint)words.Length, &diag);
                    }
                    else
                    {
                        var bin = new ConstBinary { Code = code, WordCount = (nuint)words.Length };
                        r = ValidateWithOptions(ctx, opts, &bin, &diag);
                    }
                    if (r == Result.Success)
                        return null;
                    try
                    {
                        var msg = diag != null && diag->Error != null
                            ? Marshal.PtrToStringAnsi((IntPtr)diag->Error)
                            : null;
                        return msg ?? $"SPIR-V validation failed: {r}";
                    }
                    finally
                    {
                        if (diag != null) DiagnosticDestroy(diag);
                    }
                }
            }
            finally
            {
                if (opts != IntPtr.Zero) ValidatorOptionsDestroy(opts);
            }
        }
        finally
        {
            ContextDestroy(ctx);
        }
    }

    /// <inheritdoc cref="Validate(ReadOnlySpan{uint}, TargetEnv, ValidatorOptions)"/>
    public static string? Validate(ReadOnlySpan<byte> bytes, TargetEnv env = TargetEnv.Vulkan_1_3, ValidatorOptions options = ValidatorOptions.None)
        => Validate(MemoryMarshal.Cast<byte, uint>(bytes), env, options);

    // ---- Optimizer (C shim over spvtools::Optimizer) ----------------------
    [DllImport(Lib, EntryPoint = "stride_spvOptimizerCreate")]
    static extern IntPtr OptimizerCreate(TargetEnv env);

    [DllImport(Lib, EntryPoint = "stride_spvOptimizerDestroy")]
    static extern void OptimizerDestroy(IntPtr optimizer);

    [DllImport(Lib, EntryPoint = "stride_spvOptimizerRegisterLegalizationPasses")]
    static extern void OptimizerRegisterLegalizationPasses(IntPtr optimizer);

    [DllImport(Lib, EntryPoint = "stride_spvOptimizerRegisterLegalizationPassesPreserveInterface")]
    static extern void OptimizerRegisterLegalizationPassesPreserveInterface(IntPtr optimizer);

    [DllImport(Lib, EntryPoint = "stride_spvOptimizerRegisterPerformancePasses")]
    static extern void OptimizerRegisterPerformancePasses(IntPtr optimizer);

    [DllImport(Lib, EntryPoint = "stride_spvOptimizerRegisterPassFromFlag")]
    static extern int OptimizerRegisterPassFromFlag(IntPtr optimizer, byte* flag, int preserveInterface);

    [DllImport(Lib, EntryPoint = "stride_spvOptimizerRun")]
    static extern Result OptimizerRun(
        IntPtr optimizer,
        uint* inputBinary,
        nuint inputWordCount,
        uint** outputBinary,
        nuint* outputWordCount);

    [DllImport(Lib, EntryPoint = "stride_spvOptimizerFreeBinary")]
    static extern void OptimizerFreeBinary(uint* binary);

    enum PassMode { Legalize, LegalizePreserveInterface, Performance }

    /// <summary>
    /// Runs the SPIRV-Cross-tuned legalization pass list with <c>preserve_interface=true</c>
    /// — constant folding, DCE, CCP, structured-CFG cleanup, but no pruning of unused
    /// <c>Input</c>/<c>Output</c> variables. Use this before handing SPIR-V to SPIRV-Cross
    /// for HLSL/MSL/GLSL emission.
    /// <para>
    /// Interface preservation is a stopgap: Stride feeds a single merged module
    /// containing every stage to the optimizer, and spirv-opt has no cross-stage
    /// awareness. Letting DCE strip outputs independently per stage leaves downstream
    /// stages holding inputs whose producers vanished, which FXC then maps to
    /// mismatched hardware registers. The proper long-term fix is cross-stage DCE
    /// driven back-to-front (PS → DS/GS → VS).
    /// </para>
    /// </summary>
    public static uint[] LegalizeForHlsl(ReadOnlySpan<uint> words, TargetEnv env = TargetEnv.Vulkan_1_3)
        => RunOptimizer(words, env, PassMode.LegalizePreserveInterface);

    /// <summary>
    /// Runs the performance pass list (equivalent to <c>spirv-opt -O</c>). Produces
    /// smaller, faster SPIR-V with no semantic change. Don't use before SPIRV-Cross
    /// — the aggressive inlining and reordering hurts HLSL output quality.
    /// </summary>
    public static uint[] OptimizeForPerformance(ReadOnlySpan<uint> words, TargetEnv env = TargetEnv.Vulkan_1_3)
        => RunOptimizer(words, env, PassMode.Performance);

    /// <summary>
    /// Runs a caller-supplied pass pipeline, specified as <c>spirv-opt</c> CLI flags
    /// (e.g. <c>"--eliminate-dead-code-aggressive"</c>, <c>"--scalar-replacement=0"</c>).
    /// <paramref name="preserveInterface"/> maps to the <c>preserve_interface</c> flag
    /// threaded into passes that can strip unused <c>Input</c>/<c>Output</c> variables.
    /// </summary>
    public static uint[] Optimize(ReadOnlySpan<uint> words, IEnumerable<string> flags, bool preserveInterface = false, TargetEnv env = TargetEnv.Vulkan_1_3)
    {
        var opt = OptimizerCreate(env);
        if (opt == IntPtr.Zero)
            throw new InvalidOperationException("spvOptimizerCreate failed");
        try
        {
            foreach (var flag in flags)
                RegisterFlag(opt, flag, preserveInterface);
            return RunAndCopy(opt, words);
        }
        finally
        {
            OptimizerDestroy(opt);
        }
    }

    static uint[] RunOptimizer(ReadOnlySpan<uint> words, TargetEnv env, PassMode mode)
    {
        var opt = OptimizerCreate(env);
        if (opt == IntPtr.Zero)
            throw new InvalidOperationException("spvOptimizerCreate failed");
        try
        {
            switch (mode)
            {
                case PassMode.Legalize:                  OptimizerRegisterLegalizationPasses(opt); break;
                case PassMode.LegalizePreserveInterface: OptimizerRegisterLegalizationPassesPreserveInterface(opt); break;
                case PassMode.Performance:               OptimizerRegisterPerformancePasses(opt); break;
            }
            return RunAndCopy(opt, words);
        }
        finally
        {
            OptimizerDestroy(opt);
        }
    }

    static uint[] RunAndCopy(IntPtr opt, ReadOnlySpan<uint> words)
    {
        uint* outPtr = null;
        nuint outCount = 0;
        fixed (uint* inPtr = words)
        {
            var r = OptimizerRun(opt, inPtr, (nuint)words.Length, &outPtr, &outCount);
            if (r != Result.Success)
                throw new InvalidOperationException($"spvOptimizerRun failed: {r}");
        }
        try
        {
            var result = new uint[(int)outCount];
            new ReadOnlySpan<uint>(outPtr, (int)outCount).CopyTo(result);
            return result;
        }
        finally
        {
            if (outPtr != null) OptimizerFreeBinary(outPtr);
        }
    }

    static void RegisterFlag(IntPtr opt, string flag, bool preserveInterface)
    {
        var buf = stackalloc byte[Encoding.ASCII.GetMaxByteCount(flag.Length) + 1];
        int n = Encoding.ASCII.GetBytes(flag, new Span<byte>(buf, flag.Length * 2));
        buf[n] = 0;
        if (OptimizerRegisterPassFromFlag(opt, buf, preserveInterface ? 1 : 0) == 0)
            throw new InvalidOperationException($"spvOptimizerRegisterPassFromFlag rejected '{flag}'");
    }
}
