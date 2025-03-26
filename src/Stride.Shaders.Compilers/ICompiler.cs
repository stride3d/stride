namespace Stride.Shaders.Compilers;


public interface ICompiler
{
    bool Compile(string code, out byte[] compiled);
}