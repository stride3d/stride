using Stride.Shaders.Spirv;
using Stride.Shaders.ThreeAddress;

namespace Stride.Shaders.Parsing.AST.Shader;

public static class Lowering
{
    public static IEnumerable<Register> LowerToken(ShaderToken token, bool isHead = true)
    {

        return token switch
        {
            // BlockStatement t => Lower(t),
            // AssignChain t => Lower(t),
            // DeclareAssign t => Lower(t),
            // ConditionalExpression t => Lower(t),
            // Operation t => Lower(t),
            // ArrayAccessor t => Lower(t, isHead),
            // ChainAccessor t => Lower(t, isHead),
            ShaderLiteral t => Lower(t),
            _ => throw new NotImplementedException()
        };
    }

    public IEnumerable<Register> Lower(ShaderLiteral l)
    {
        
    }

    

}