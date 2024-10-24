namespace Stride.Shaders.Parsing.SDFX.AST;

public class EffectFlow(TextLocation info) : EffectStatement(info);

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