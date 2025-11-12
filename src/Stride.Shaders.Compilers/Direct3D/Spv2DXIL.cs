using System.Runtime.InteropServices;

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
    bool dump_nir;
}


public struct RuntimeConf
{
    struct runtime_data_cbv
    {
        ushort register_space;
        ushort base_shader_register;
    }


    struct PushConstantCBV
    {
        ushort register_space;
        ushort base_shader_register;
    }

    enum  FirstVertexAndBaseInstanceMode;
    enum  WorkgroupIdMode;

    struct YZFlip
    {
        // mode != DXIL_SPIRV_YZ_FLIP_NONE only valid on vertex/geometry stages.
        enum Mode;

        // Y/Z flip masks (one bit per viewport)
        ushort y_mask;
        ushort z_mask;
    }

    // The caller supports read-only images to be turned into SRV accesses,
    // which allows us to run the nir_opt_access() pass
    bool declared_read_only_images_as_srvs;

    // The caller supports read-write images to be turned into SRV accesses,
    // if they are found not to be written
    bool inferred_read_only_images_as_srvs;

    // Force sample rate shading on a fragment shader
    bool force_sample_rate_shading;

    // View index needs to be lowered to a UBO lookup
    bool lower_view_index;
    // View index also needs to be forwarded to RT layer output
    bool lower_view_index_to_rt_layer;

    // Affects which features can be used by the shader
    enum Shader_model_max;
}

public unsafe delegate void MSGCallback(void* priv, string msg);

public unsafe struct DXILSpirvLogger
{
    void* priv;
    MSGCallback log;
}

public unsafe struct DXILSpirvObject {
   struct Metadata;
   struct Binary {
      void *buffer;
      nint size;
   }
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
    [LibraryImport("./native/spirv_to_dxil.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static unsafe partial int spirv_to_dxil(
        uint* words,
        nint word_count,
        Specialization* specializations,
        uint num_specializations,
        ShaderStage stage,
        string entry_point_name,
        ValidatorVersion validator_version_max,
        DebugOptions* debug_options,
        RuntimeConf* conf,
        DXILSpirvLogger* logger,
        DXILSpirvObject* out_dxil
    );


    [LibraryImport("./native/spirv_to_dxil.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial ulong spirv_to_dxil_get_version();
}