using System.Text;
using SPIRVCross;
using static SPIRVCross.SPIRV;

namespace SDSLParserExample;

public static class CrossExtensions
{

    public static void ToGlsl(this Span<byte> byteCode) => ToGlsl(byteCode.ToArray());
    public static void ToHlsl(this Span<byte> byteCode) => ToHlsl(byteCode.ToArray());
    public static void ToGlsl(this byte[] bytecode)
    {
        unsafe
        {
            string GetString(byte* ptr)
            {
                int length = 0;
                while (length < 4096 && ptr[length] != 0)
                    length++;
                // Decode UTF-8 bytes to string.
                return Encoding.UTF8.GetString(ptr, length);
            }

            SpvId* spirv;

            fixed (byte* ptr = bytecode)
                spirv = (SpvId*)ptr;

            uint word_count = (uint)bytecode.Length / 4;

            spvc_context context = default;
            spvc_parsed_ir ir;
            spvc_compiler compiler_glsl;
            spvc_compiler_options options;
            spvc_resources resources;
            spvc_reflected_resource* list = default;
            nuint count = default;
            spvc_error_callback error_callback = default;

            // Create context.
            if (spvc_context_create(&context) != spvc_result.SPVC_SUCCESS) throw new Exception();

            // Set debug callback.
            spvc_context_set_error_callback(context, error_callback, null);

            // Parse the SPIR-V.
            if (spvc_context_parse_spirv(context, spirv, word_count, &ir) != spvc_result.SPVC_SUCCESS)
                throw new Exception();

            // Hand it off to a compiler instance and give it ownership of the IR.

            if (spvc_context_create_compiler(context, spvc_backend.Glsl, ir, spvc_capture_mode.TakeOwnership, &compiler_glsl) != spvc_result.SPVC_SUCCESS)
                throw new Exception();
            // Do some basic reflection.

            if (spvc_compiler_create_shader_resources(compiler_glsl, &resources) != spvc_result.SPVC_SUCCESS)
                throw new Exception();
            if (spvc_resources_get_resource_list_for_type(resources, spvc_resource_type.UniformBuffer, (spvc_reflected_resource*)&list, &count) != spvc_result.SPVC_SUCCESS)
                throw new Exception();

            for (uint i = 0; i < count; i++)
            {
                Console.WriteLine("ID: {0}, BaseTypeID: {1}, TypeID: {2}, Name: {3}", list[i].id, list[i].base_type_id, list[i].type_id, GetString(list[i].name));

                uint set = spvc_compiler_get_decoration(compiler_glsl, (SpvId)list[i].id, SpvDecoration.SpvDecorationDescriptorSet);
                Console.WriteLine($"Set: {set}");

                uint binding = spvc_compiler_get_decoration(compiler_glsl, (SpvId)list[i].id, SpvDecoration.SpvDecorationBinding);
                Console.WriteLine($"Binding: {binding}");

                Console.WriteLine("=========");
            }
            Console.WriteLine("\n \n");

            // Modify options.
            if (spvc_compiler_create_compiler_options(compiler_glsl, &options) != spvc_result.SPVC_SUCCESS) throw new Exception();
            if (spvc_compiler_options_set_uint(options, spvc_compiler_option.GlslVersion, 450) != spvc_result.SPVC_SUCCESS) throw new Exception();
            if (spvc_compiler_options_set_bool(options, spvc_compiler_option.GlslEs, false) != spvc_result.SPVC_SUCCESS) throw new Exception();
            if (spvc_compiler_install_compiler_options(compiler_glsl, options) != spvc_result.SPVC_SUCCESS) throw new Exception();

            byte* result = default;
            var res = spvc_compiler_compile(compiler_glsl, (byte*)&result);
            if (res != spvc_result.SPVC_SUCCESS) throw new Exception(res.ToString());
            Console.WriteLine("Cross-compiled source: \n{0}", GetString(result));

            // Frees all memory we allocated so far.
            spvc_context_destroy(context);
        }
    }
    public static void ToHlsl(this byte[] bytecode)
    {
        unsafe
        {
            string GetString(byte* ptr)
            {
                int length = 0;
                while (length < 4096 && ptr[length] != 0)
                    length++;
                // Decode UTF-8 bytes to string.
                return Encoding.UTF8.GetString(ptr, length);
            }

            SpvId* spirv;

            fixed (byte* ptr = bytecode)
                spirv = (SpvId*)ptr;

            uint word_count = (uint)bytecode.Length / 4;

            spvc_context context = default;
            spvc_parsed_ir ir;
            spvc_compiler compiler_hlsl;
            spvc_compiler_options options;
            spvc_resources resources;
            spvc_reflected_resource* list = default;
            nuint count = default;
            spvc_error_callback error_callback = default;

            // Create context.
            spvc_context_create(&context);

            // Set debug callback.
            spvc_context_set_error_callback(context, error_callback, null);

            // Parse the SPIR-V.
            spvc_context_parse_spirv(context, spirv, word_count, &ir);

            // Hand it off to a compiler instance and give it ownership of the IR.
            spvc_context_create_compiler(context, spvc_backend.Hlsl, ir, spvc_capture_mode.TakeOwnership, &compiler_hlsl);

            // Do some basic reflection.
            spvc_compiler_create_shader_resources(compiler_hlsl, &resources);
            spvc_resources_get_resource_list_for_type(resources, spvc_resource_type.UniformBuffer, (spvc_reflected_resource*)&list, &count);

            for (uint i = 0; i < count; i++)
            {
                Console.WriteLine("ID: {0}, BaseTypeID: {1}, TypeID: {2}, Name: {3}", list[i].id, list[i].base_type_id, list[i].type_id, GetString(list[i].name));

                uint set = spvc_compiler_get_decoration(compiler_hlsl, (SpvId)list[i].id, SpvDecoration.SpvDecorationDescriptorSet);
                Console.WriteLine($"Set: {set}");

                uint binding = spvc_compiler_get_decoration(compiler_hlsl, (SpvId)list[i].id, SpvDecoration.SpvDecorationBinding);
                Console.WriteLine($"Binding: {binding}");

                Console.WriteLine("=========");
            }
            Console.WriteLine("\n \n");

            // Modify options.
            spvc_compiler_create_compiler_options(compiler_hlsl, &options);
            spvc_compiler_options_set_uint(options, spvc_compiler_option.HlslShaderModel, 50);
            spvc_compiler_install_compiler_options(compiler_hlsl, options);

            byte* result = default;
            spvc_compiler_compile(compiler_hlsl, (byte*)&result);
            Console.WriteLine("Cross-compiled source: \n{0}", GetString(result));

            // Frees all memory we allocated so far.
            spvc_context_destroy(context);
        }
    }
}