using System.Text;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace Stride.Shaders.Compilers.Direct3D;

using DXCBuffer = Silk.NET.Direct3D.Compilers.Buffer;




public record struct DXCompiler() : ICompiler
{

    public static string sampleCode = @"
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
    static Guid blobGuid = Guid.Parse("3DA636C9-BA71-4024-A301-30CBF125305B");
    static Guid utilsGuid = Guid.Parse("6245D6AF-66E0-48FD-80B4-4D271796748C");
    static Guid compilerGuid = Guid.Parse("73e22d93-e6ce-47f3-b5bf-f0664f39c1b0");
    static Guid compilerArgsGuid = Guid.Parse("3e56ae82-224d-470f-a1a1-fe3016ee9f9d");
    static Guid resultGuid = Guid.Parse("58346CDA-DDE7-4497-9461-6F87AF5E0659");
    static readonly DXC dxc = DXC.GetApi();
    
    public bool Compile(string code, out Memory<byte> compiled)
    {
        throw new NotImplementedException();
        // var content = Encoding.ASCII.GetBytes(Code);
        // unsafe
        // {
        //     var compiler = dxc.CreateInstance<IDxcCompiler3>(ref compilerGuid);
        //     var utils = dxc.CreateInstance<IDxcUtils>(ref utilsGuid);
        //     var args = dxc.CreateInstance<IDxcCompilerArgs>(ref compilerArgsGuid);
            
        //     // Console.WriteLine($"{(nint)compiler.GetAddressOf()} - {(nint)library.GetAddressOf()}");
        //     IDxcBlobEncoding* sourceBlob = null;
            
        //     SilkMarshal.ThrowHResult(
        //         utils.Get().CreateBlobFromPinned((void*)SilkMarshal.StringToPtr(Code), (uint)Code.Length, 1200, ref sourceBlob)
        //     );
        //     // utils.Get().BuildArguments("mycode", "PSMain", "ps_6_0", (char**)SilkMarshal.StringArrayToPtr(["-spirv", "-T", "ps_6_0"]), 3, null, 0, ref args);
        //     var buff = new DXCBuffer(sourceBlob, (nuint)Code.Length);
        //     IDxcOperationResult* result = null;
        //     string[] parms = ["-spirv", "-T", "ps_6_0", "-E", "PSMain"];
        //     SilkMarshal.ThrowHResult(
        //         compiler.Get().Compile(&buff, parms, (uint)parms.Length, null, ref resultGuid,(void**)result)
        //     );
            
        //     // Console.WriteLine((nint)result);
        // }
        // compiled = Memory<byte>.Empty;
        // return true;
    }
}