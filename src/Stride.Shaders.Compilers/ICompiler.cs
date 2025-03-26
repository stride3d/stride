namespace Stride.Shaders.Compilers;


public interface ICompiler
{
    bool Compile(string code, out Memory<byte> compiled);
}