using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace Stride.Shaders.Compilers;




public record struct DXCompiler(string Code)
{

    static string sampleCode = @"
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
    static DXC dxc = DXC.GetApi();
    
    public readonly void Compile()
    {
        // var content = Encoding.ASCII.GetBytes(Code);
        unsafe
        {
            
            Guid id = IDxcCompiler.Guid;
            Guid libId = IDxcUtils.Guid;
            var guid = IDxcUtils.Guid;
            var utils = dxc.CreateInstance<IDxcUtils>(ref guid);
            SilkMarshal.ThrowHResult(dxc.CreateInstance(&libId, out ComPtr<IDxcLibrary> library));
            // Console.WriteLine($"{(nint)compiler.GetAddressOf()} - {(nint)library.GetAddressOf()}");
            IDxcBlobEncoding* sourceBlob = null;
            fixed (char* ptr = Code.AsSpan())
                SilkMarshal.ThrowHResult(library.Get().CreateBlobWithEncodingFromPinned(ptr, (uint)Code.Length, 0, &sourceBlob));

            // IDxcOperationResult* result = null;
            // SilkMarshal.ThrowHResult(compiler.Get().Compile((IDxcBlob*)sourceBlob, (string)null!, (char*)SilkMarshal.StringToPtr("PSMain"), (char*)SilkMarshal.StringToPtr(""), (char**)SilkMarshal.StringArrayToPtr(["-spirv","-T", "ps_6_0"]), 3, null, 0, null, &result));
            // Console.WriteLine((nint)result);
        }
    }
}