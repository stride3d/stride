using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]

namespace Stride.Shaders.Compilers.Direct3D;
public enum ShaderStage
{
    DXIL_SPIRV_SHADER_NONE = -1,
    DXIL_SPIRV_SHADER_VERTEX = 0,
    DXIL_SPIRV_SHADER_TESS_CTRL = 1,
    DXIL_SPIRV_SHADER_TESS_EVAL = 2,
    DXIL_SPIRV_SHADER_GEOMETRY = 3,
    DXIL_SPIRV_SHADER_FRAGMENT = 4,
    DXIL_SPIRV_SHADER_COMPUTE = 5,
    DXIL_SPIRV_SHADER_KERNEL = 14,
}

public struct DebugOptions
{
    public bool dump_nir;
}

public enum FlipMode
{
    YZFlipNone = 0,
    // Y-flip is unconditional: pos.y = -pos.y
    // Z-flip is unconditional: pos.z = -pos.z + 1.0f
    YFlipUnconditional = 1 << 0,
    ZFlipUnconditional = 1 << 1,
    YZFlipUnconditional = YFlipUnconditional | ZFlipUnconditional,
    // Y-flip/Z-flip info are passed through a sysval
    YFlipConditional = 1 << 2,
    ZFlipConditional = 1 << 2,
    YZFlipConditional = YFlipConditional | ZFlipConditional,
}

public enum SysvalType
{
    // The sysval can be inlined in the shader as a constant zero
    Zero,
    // The sysval has a supported DXIL equivalent
    Native,
    // The sysval might be nonzero and has no DXIL equivalent, so it
    // will need to be provided by the runtime_data constant buffer
    RuntimeData,
}

public enum dxil_shader_model
{
    SHADER_MODEL_6_0 = 0x60000,
    SHADER_MODEL_6_1,
    SHADER_MODEL_6_2,
    SHADER_MODEL_6_3,
    SHADER_MODEL_6_4,
    SHADER_MODEL_6_5,
    SHADER_MODEL_6_6,
    SHADER_MODEL_6_7,
    SHADER_MODEL_6_8,
}

public struct RegisterInfo
{
    public uint register_space;
    public uint base_shader_register;
}


public struct RuntimeConf
{
    public RegisterInfo runtime_data_cbv;
    public RegisterInfo push_constant_cbv;

    public SysvalType first_vertex_and_base_instance_mode;
    public SysvalType workgroup_id_mode;

    // mode != DXIL_SPIRV_YZ_FLIP_NONE only valid on vertex/geometry stages.
    public FlipMode yzflip_mode;
    // Y/Z flip masks (one bit per viewport)
    public ushort yzflip_y_mask;
    public ushort yzflip_z_mask;

    // The caller supports read-only images to be turned into SRV accesses,
    // which allows us to run the nir_opt_access() pass
    public bool declared_read_only_images_as_srvs;

    // The caller supports read-write images to be turned into SRV accesses,
    // if they are found not to be written
    public bool inferred_read_only_images_as_srvs;

    // Force sample rate shading on a fragment shader
    public bool force_sample_rate_shading;

    // View index needs to be lowered to a UBO lookup
    public bool lower_view_index;
    // View index also needs to be forwarded to RT layer output
    public bool lower_view_index_to_rt_layer;

    // Affects which features can be used by the shader
    public dxil_shader_model shader_model_max;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void MSGCallback(void* priv, string msg);

public unsafe struct DXILSpirvLogger
{
    public void* priv;
    public nint log;
}

public unsafe struct DXILSpirvObject {
    // Some sysval or other type of data is accessed which needs to be piped
    // from the app/API implementation into the shader via a buffer
    bool metadata_requires_runtime_data;

    // Specifically if a vertex shader needs the first-vertex or base-instance
    // sysval. These are relevant since these can come from an indirect arg
    // buffer, and therefore piping them to the runtime data buffer is extra
    // complex.
    bool metadata_needs_draw_sysvals;

    void *buffer;
    nint size;
}
public unsafe struct Specialization
{
    ushort id;
    void* value;
    bool defined_on_module;
}

public enum ValidatorVersion {
   NO_DXIL_VALIDATION,
   DXIL_VALIDATOR_1_0 = 0x10000,
   DXIL_VALIDATOR_1_1,
   DXIL_VALIDATOR_1_2,
   DXIL_VALIDATOR_1_3,
   DXIL_VALIDATOR_1_4,
   DXIL_VALIDATOR_1_5,
   DXIL_VALIDATOR_1_6,
   DXIL_VALIDATOR_1_7,
   DXIL_VALIDATOR_1_8,
};

public static partial class Spv2DXIL
{

    // Import user32.dll (containing the function we need) and define
    // the method corresponding to the native function.
    [LibraryImport("./native/spirv_to_dxil.dll", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool spirv_to_dxil(
        uint* words,
        nint word_count,
        Specialization* specializations,
        uint num_specializations,
        ShaderStage stage,
        string entry_point_name,
        ValidatorVersion validator_version_max,
        ref DebugOptions debug_options,
        ref RuntimeConf conf,
        ref DXILSpirvLogger logger,
        out DXILSpirvObject out_dxil
    );


    [LibraryImport("./native/spirv_to_dxil.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial ulong spirv_to_dxil_get_version();
}