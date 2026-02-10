using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
namespace Stride.Shaders.Parsing.SDFX.AST;

public class EffectFlow(TextLocation info) : EffectStatement(info)
{
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class EffectForEach(TypeName typename, Identifier variable, Expression collection, Statement body, TextLocation info) : EffectFlow(info)
{
    public TypeName Typename { get; set; } = typename;
    public Identifier Variable { get; set; } = variable;
    public Expression Collection { get; set; } = collection;
    public Statement Body { get; set; } = body;

    public override string ToString()
    {
        return $"foreach({Typename} {Variable} in {Collection})\n{Body}";
    }
}
