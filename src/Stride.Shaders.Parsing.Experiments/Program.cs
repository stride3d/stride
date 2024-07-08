using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

using Silk.NET.Direct3D.Compilers;
using Silk.NET.Core.Native;
using System.Text;

Console.WriteLine("Hello world");
Console.WriteLine(Directory.GetCurrentDirectory());


var d3d = D3DCompiler.GetApi();
var utf_content = @"
struct PSInput
{
    float4 position : SV_POSITION;
    float4 color;
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
                pTarget: "vs_5_0",
                Flags1: 0,
                Flags2: 0,
                ppCode: &shader,
                ppErrorMsgs: &errorMsgs);
    }
    Console.WriteLine(Encoding.ASCII.GetString(errorMsgs->Buffer));
    SilkMarshal.ThrowHResult(res);
}