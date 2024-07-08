using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace Stride.Shaders.Compilers;




public record struct FXCompiler(string Code)
{
    static D3DCompiler d3d = D3DCompiler.GetApi();
    
    public readonly void Compile()
    {
        var content = Encoding.ASCII.GetBytes(Code);
        unsafe
        {
            // ComPtr<ID3D10Blob> shader;
            // ComPtr<ID3D10Blob> errorMsgs;
            // int res = 0;
            // fixed(byte* pContent = content)
            // res = d3d.Compile(
            //             pSrcData: pContent,
            //             SrcDataSize: (nuint)content.Length,
            //             // pSourceName: "triangle",
            //             pDefines: null,
            //             pInclude: null,
            //             pEntrypoint: "VSMain",
            //             pTarget: "vs_5_0",
            //             Flags1: 0,
            //             Flags2: 0,
            //             ppCode: &shader,
            //             ppErrorMsgs: &errorMsgs);
        
            // Console.WriteLine(Encoding.ASCII.GetString(errorMsgs.Get().Buffer));
            // SilkMarshal.ThrowHResult(res);
        }
    }
}