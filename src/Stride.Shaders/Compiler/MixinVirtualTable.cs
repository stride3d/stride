using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Compiling;


public class MixinVirtualTable
{
    public List<ShaderMethod>? Methods {get;set;}
    public List<ShaderVariableDeclaration>? Variables {get;set;}
}