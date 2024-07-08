using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

using Silk.NET.Direct3D.Compilers;
using Silk.NET.SPIRV.Cross;
using Silk.NET.Core.Native;
using System.Text;
using Stride.Shaders.Compilers;

Console.WriteLine("Hello world");
Console.WriteLine(Directory.GetCurrentDirectory());

uint[] words = [
	// Offset 0x00000000 to 0x0000016F
	0x03022307, 0x00050100, 0x00000E00,
	0x0C000000, 0x00000000, 0x11000200,
	0x01000000, 0x0E000300, 0x00000000,
	0x01000000, 0x0F000700, 0x04000000,
	0x01000000, 0x50534D61, 0x696E0000,
	0x02000000, 0x03000000, 0x10000300,
	0x01000000, 0x07000000, 0x03000300,
	0x05000000, 0x58020000, 0x05000600,
	0x02000000, 0x696E2E76, 0x61722E43,
	0x4F4C4F52, 0x00000000, 0x05000700,
	0x03000000, 0x6F75742E, 0x7661722E,
	0x53565F54, 0x41524745, 0x54000000,
	0x05000400, 0x01000000, 0x50534D61,
	0x696E0000, 0x47000400, 0x02000000,
	0x1E000000, 0x00000000, 0x47000400,
	0x03000000, 0x1E000000, 0x00000000,
	0x16000300, 0x04000000, 0x20000000,
	0x17000400, 0x05000000, 0x04000000,
	0x04000000, 0x20000400, 0x06000000,
	0x01000000, 0x05000000, 0x20000400,
	0x07000000, 0x03000000, 0x05000000,
	0x13000200, 0x08000000, 0x21000300,
	0x09000000, 0x08000000, 0x3B000400,
	0x06000000, 0x02000000, 0x01000000,
	0x3B000400, 0x07000000, 0x03000000,
	0x03000000, 0x36000500, 0x08000000,
	0x01000000, 0x00000000, 0x09000000,
	0xF8000200, 0x0A000000, 0x3D000400,
	0x05000000, 0x0B000000, 0x02000000,
	0x3E000300, 0x03000000, 0x0B000000,
	0xFD000100, 0x38000100
];

unsafe
{
    var code = new SpirvTranslator(words.AsMemory());
    Console.WriteLine(code.Translate(Backend.Glsl));
    // var cross = Cross.GetApi();
    // Context* context = null;
    // cross.ContextCreate(&context);
    // ParsedIr* ir = null;
    // Result res = Result.Success;
    // fixed (uint* w = words)
    //     res = cross.ContextParseSpirv(context, w, (nuint)words.Length, &ir);

    // Compiler* compiler = null;
    // Resources* resources = null;
    // ReflectedResource* resourceList = null;
    // nuint size = 0;
    // res = cross.ContextCreateCompiler(context, Backend.Glsl, ir, CaptureMode.Copy, &compiler);
    // res = cross.CompilerCreateShaderResources(compiler, &resources);
    // res = cross.ResourcesGetResourceListForType(resources, ResourceType.UniformBuffer, &resourceList, &size);
    // for (int i = 0; i < (int)size; i++)
    // {
    //     var name = new Span<char>(resourceList[i].Name, (int)size);
    //     Console.WriteLine("ID: {0}, BaseTypeID: {1}, TypeID: {2}, Name: {3}", resourceList[i].Id, resourceList[i].BaseTypeId, resourceList[i].TypeId, name.ToString());

    //     uint set = cross.CompilerGetDecoration(compiler, resourceList[i].Id, Silk.NET.SPIRV.Decoration.DescriptorSet);
    //     Console.WriteLine($"Set: {set}");

    //     uint binding = cross.CompilerGetDecoration(compiler, resourceList[i].Id, Silk.NET.SPIRV.Decoration.Binding);
    //     Console.WriteLine($"Binding: {binding}");

    //     Console.WriteLine("=========");
    // }
    // string GetString(byte* ptr)
    // {
    //     int length = 0;
    //     while (length < 4096 && ptr[length] != 0)
    //         length++;
    //     // Decode UTF-8 bytes to string.
    //     return Encoding.UTF8.GetString(ptr, length);
    // }
    // byte* translated = null;
    // cross.CompilerCompile(compiler, &translated);
    // Console.WriteLine(GetString(translated));
    var x = 0;
    
}

// var d3d = D3DCompiler.GetApi();

// var dxc = DXC.GetApi();
// unsafe
// {
//     IDxcCompilerArgs* a = null;
//     IDxcOperationResult* operationResult = null;
//     var cid = IDxcCompiler.Guid;
//     SilkMarshal.ThrowHResult(
//         dxc.CreateInstance(ref cid, out ComPtr<IDxcCompiler> compiler)
//     );
//     var x = 0;
// }

var utf_content = @"
struct PSInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

PSInput VSMain(float4 position : POSITION, float4 color : COLOR)
{
    PSInput result;

    result.position = position;
    result.color = color;

    return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return input.color;
}

";



var content = Encoding.ASCII.GetBytes(utf_content);
unsafe
{
    D3DCompiler d3d = D3DCompiler.GetApi();
    ID3D10Blob* shader;
    ID3D10Blob* errorMsgs;
    int res = 0;
    fixed (byte* pContent = content)
    {
        res = d3d.Compile(
                pSrcData: pContent,
                SrcDataSize: (nuint)content.Length,
                pSourceName: "triangle",
                pDefines: null,
                pInclude: null,
                pEntrypoint: "VSMain",
                pTarget: "vs_6_0",
                Flags1: 0,
                Flags2: 0,
                ppCode: &shader,
                ppErrorMsgs: &errorMsgs);
    }
    Console.WriteLine(Encoding.ASCII.GetString(errorMsgs->Buffer));
    SilkMarshal.ThrowHResult(res);
}

