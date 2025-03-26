using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace Stride.Shaders.Compilers.Direct3D;




public record struct FXCompiler() : ICompiler
{
    static D3DCompiler d3d = D3DCompiler.GetApi();
    
    public bool Compile(string code, out byte[] compiled)
    {
        throw new NotImplementedException();
    }
}