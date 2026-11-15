// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Direct3D.Compilers;
using Stride.Shaders.Compilers.Direct3D;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The <c>spirv_to_dxil</c> interop (mesa): struct layouts and specialization constants.
/// </summary>
public unsafe class Spv2DxilTests
{
    [Fact]
    public void SpecializationStructMatchesMesa()
    {
        // dxil_spirv_specialization: uint32_t id, 8-byte dxil_spirv_const_value union, bool defined_on_module
        Assert.Equal(8, sizeof(SpecializationValue));
        Assert.Equal(24, sizeof(Specialization));
        Assert.Equal(8, (int)Marshal.OffsetOf<Specialization>(nameof(Specialization.value)));
        Assert.Equal(16, (int)Marshal.OffsetOf<Specialization>(nameof(Specialization.defined_on_module)));
    }

    [Fact]
    public void SpecializationValueChangesDxil()
    {
        SkipUnlessWindows();

        var words = FragmentWithSpecConstant();
        Assert.Null(SpirvTools.Validate(words));

        var byDefault = CompileFragment(words, []);
        var specialized = CompileFragment(words, [new Specialization { id = 0, value = new SpecializationValue { f32 = 0.25f } }]);

        Assert.NotEqual(byDefault, specialized);
        if (DxilValidator.Find() is { } validator)
        {
            Assert.Null(validator.Validate(byDefault));
            Assert.Null(validator.Validate(specialized));
            // The validator does reject a damaged container
            var damaged = byDefault.ToArray();
            damaged[damaged.Length / 2] ^= 0xFF;
            Assert.NotNull(validator.Validate(damaged));
        }
    }

    static void SkipUnlessWindows()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("spirv_to_dxil is only shipped for Windows");
    }

    /// <summary>
    /// A fragment shader that writes a float spec constant (<c>SpecId</c> 0, default 1.0) to its color output.
    /// </summary>
    static uint[] FragmentWithSpecConstant()
    {
        const uint main = 1, tVoid = 2, tFunction = 3, tFloat = 4, tVec4 = 5, tOutputVec4 = 6, output = 7, spec = 8, label = 9, color = 10;
        var words = new List<uint> { 0x07230203, 0x00010000, 0, 11, 0 };
        void Op(ushort opcode, params uint[] operands)
        {
            words.Add((uint)(operands.Length + 1) << 16 | opcode);
            words.AddRange(operands);
        }

        Op(17, 1); // OpCapability Shader
        Op(14, 0, 1); // OpMemoryModel Logical GLSL450
        Op(15, 4, main, 0x6E69616D, 0, output); // OpEntryPoint Fragment %main "main" %output
        Op(16, main, 7); // OpExecutionMode %main OriginUpperLeft
        Op(71, output, 30, 0); // OpDecorate %output Location 0
        Op(71, spec, 1, 0); // OpDecorate %spec SpecId 0
        Op(19, tVoid); // OpTypeVoid
        Op(33, tFunction, tVoid); // OpTypeFunction %void
        Op(22, tFloat, 32); // OpTypeFloat 32
        Op(23, tVec4, tFloat, 4); // OpTypeVector %float 4
        Op(32, tOutputVec4, 3, tVec4); // OpTypePointer Output %v4float
        Op(59, tOutputVec4, output, 3); // OpVariable Output
        Op(50, tFloat, spec, 0x3F800000); // OpSpecConstant %float 1.0
        Op(54, tVoid, main, 0, tFunction); // OpFunction %void None
        Op(248, label); // OpLabel
        Op(80, tVec4, color, spec, spec, spec, spec); // OpCompositeConstruct %v4float
        Op(62, output, color); // OpStore
        Op(253); // OpReturn
        Op(56); // OpFunctionEnd
        return [.. words];
    }

    static byte[] CompileFragment(uint[] words, Specialization[] specializations)
    {
        var conf = new RuntimeConf
        {
            runtime_data_cbv = { base_shader_register = 0, register_space = 31 },
            push_constant_cbv = { base_shader_register = 1, register_space = 31 },
            yzflip_mode = FlipMode.YZFlipNone,
            shader_model_max = dxil_shader_model.SHADER_MODEL_6_2,
        };
        var debug = new DebugOptions();
        var logger = new DXILSpirvLogger { log = &IgnoreLog };
        fixed (uint* w = words)
        fixed (Specialization* s = specializations)
        {
            Assert.True(Spv2DXIL.spirv_to_dxil(w, words.Length, s, (uint)specializations.Length, Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_FRAGMENT, "main",
                ValidatorVersion.DXIL_VALIDATOR_1_4, ref debug, ref conf, ref logger, out var dxil));
            try
            {
                return new Span<byte>(dxil.buffer, (int)dxil.size).ToArray();
            }
            finally
            {
                Spv2DXIL.spirv_to_dxil_free(ref dxil);
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void IgnoreLog(void* priv, byte* message) { }

    /// <summary>
    /// The DXIL validator of the Windows SDK (<c>dxcompiler.dll</c> + <c>dxil.dll</c>), when installed.
    /// </summary>
    sealed class DxilValidator
    {
        static readonly Guid ClsidDxcUtils = new("6245d6af-66e0-48fd-80b4-4d271796748c");
        static readonly Guid ClsidDxcValidator = new("8ca3e215-f728-4cf3-8cdd-88af917587a1");

        readonly delegate* unmanaged<Guid*, Guid*, void**, int> createInstance;

        DxilValidator(nint dxcompiler) => createInstance = (delegate* unmanaged<Guid*, Guid*, void**, int>)NativeLibrary.GetExport(dxcompiler, "DxcCreateInstance");

        public static DxilValidator? Find()
        {
            var kits = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits", "10", "bin");
            var directory = Directory.Exists(kits)
                ? Directory.GetDirectories(kits, "10.*").OrderDescending().Select(d => Path.Combine(d, "x64")).FirstOrDefault(d => File.Exists(Path.Combine(d, "dxil.dll")) && File.Exists(Path.Combine(d, "dxcompiler.dll")))
                : null;
            if (directory is null)
                return null;
            // dxcompiler.dll loads dxil.dll by name: load it first from the same directory
            NativeLibrary.Load(Path.Combine(directory, "dxil.dll"));
            return new DxilValidator(NativeLibrary.Load(Path.Combine(directory, "dxcompiler.dll")));
        }

        /// <summary>
        /// Returns the validator errors, or null when <paramref name="dxil"/> is valid.
        /// </summary>
        public string? Validate(byte[] dxil)
        {
            IDxcUtils* utils = null;
            IDxcValidator* validator = null;
            IDxcBlobEncoding* blob = null;
            IDxcOperationResult* result = null;
            IDxcBlobEncoding* errors = null;
            try
            {
                var clsid = ClsidDxcUtils;
                var iid = IDxcUtils.Guid;
                Marshal.ThrowExceptionForHR(createInstance(&clsid, &iid, (void**)&utils));
                clsid = ClsidDxcValidator;
                iid = IDxcValidator.Guid;
                Marshal.ThrowExceptionForHR(createInstance(&clsid, &iid, (void**)&validator));
                fixed (byte* data = dxil)
                    Marshal.ThrowExceptionForHR(utils->CreateBlob(data, (uint)dxil.Length, 0, &blob));
                // A container it can't read fails the call itself
                var hr = validator->Validate((IDxcBlob*)blob, 0, &result);
                if (hr < 0)
                    return $"DXIL validator rejected the container: 0x{hr:X8}";
                int status;
                Marshal.ThrowExceptionForHR(result->GetStatus(&status));
                if (status >= 0)
                    return null;
                result->GetErrorBuffer(&errors);
                return errors != null ? Marshal.PtrToStringUTF8((nint)errors->GetBufferPointer(), (int)errors->GetBufferSize()) : $"DXIL validation failed: 0x{status:X8}";
            }
            finally
            {
                if (errors != null) errors->Release();
                if (result != null) result->Release();
                if (blob != null) blob->Release();
                if (validator != null) validator->Release();
                if (utils != null) utils->Release();
            }
        }
    }
}
