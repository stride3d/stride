using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
namespace Stride.Shaders.Parsing.SDFX.AST;

public class EffectFlow(TextLocation info) : EffectStatement(info)
{
    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class EffectControl(If first, TextLocation info) : EffectFlow(info)
{
    public If If { get; set; } = first;
    public List<ElseIf> ElseIfs { get; set; } = [];
    public Else? Else { get; set; }
    public override string ToString()
    {
        return $"{If}{string.Join("\n", ElseIfs.Select(x => x.ToString()))}{Else}";
    }
}
public class If(SDSL.AST.Expression condition, EffectStatement body, TextLocation info) : EffectFlow(info)
{
    public SDSL.AST.Expression Condition { get; set; } = condition;
    public EffectStatement Body { get; set; } = body;

    public override string ToString()
    {
        return $"if({Condition})\n{Body}";
    }
}

public class ElseIf(SDSL.AST.Expression condition, EffectStatement body, TextLocation info) : If(condition, body, info)
{
    public override string ToString()
    {
        return $"else if({Condition}){Body}";
    }
}

public class Else(EffectStatement body, TextLocation info) : EffectFlow(info)
{
    public EffectStatement Body { get; set; } = body;
    public override string ToString()
    {
        return $"else {Body}";
    }
}



public class EffectForEach(SDSL.AST.TypeName typename, SDSL.AST.Identifier variable, SDSL.AST.Expression collection, EffectStatement body, TextLocation info) : EffectFlow(info)
{
    public SDSL.AST.TypeName Typename { get; set; } = typename;
    public SDSL.AST.Identifier Variable { get; set; } = variable;
    public SDSL.AST.Expression Collection { get; set; } = collection;
    public EffectStatement Body { get; set; } = body;

    public override string ToString()
    {
        return $"foreach({Typename} {Variable} in {Collection})\n{Body}";
    }
}
