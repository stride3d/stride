using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
namespace Stride.Shaders.Parsing.SDFX.AST;

public class EffectFlow(TextLocation info) : EffectStatement(info)
{
    public override void Compile(Analysis.SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class EffectControl(If first, TextLocation info) : EffectFlow(info)
{
    public If If { get; set; } = first;
    public List<ElseIf> ElseIfs { get; set; } = [];
    public Else? Else { get; set; }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        compiler.Builder.Insert(new OpSDSLConditionalStart());
        If.Compile(table, compiler);
        foreach(var elseIf in ElseIfs)
            elseIf.Compile(table, compiler);
        Else?.Compile(table, compiler);
        compiler.Builder.Insert(new OpSDSLConditionalEnd());
    }
    public override string ToString()
    {
        return $"{If}{string.Join("\n", ElseIfs.Select(x => x.ToString()))}{Else}";
    }
}
public class If(Expression condition, EffectStatement body, TextLocation info) : EffectFlow(info)
{
    public Expression Condition { get; set; } = condition;
    public EffectStatement Body { get; set; } = body;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        _ = Condition switch
        {
            AccessorChainExpression { Source : Identifier, Accessors : [Identifier]} ace => 
                compiler.Builder.Insert(new OpSDSLParamsTrue(ace.ToString())),
            BinaryExpression {Left : AccessorChainExpression { Source : Identifier, Accessors : [Identifier]} ace, Op : Core.Operator.NotEquals, Right : Identifier {Name : "null"}} =>
                compiler.Builder.Insert(new OpSDSLParamsTrue(ace.ToString())),
            _ => throw new NotImplementedException()
        };

        _ = Body switch
        {
            EffectExpressionStatement {Statement : ExpressionStatement { Expression : MethodCall {Name.Name : "mixin", Parameters.Values : [Identifier m]}}} 
                => compiler.Builder.Insert(new OpSDSLMixinUse(m.Name)),
            EffectExpressionStatement {Statement : ExpressionStatement { Expression : MethodCall {Name.Name : "mixin", Parameters.Values : [AccessorChainExpression {Source : Identifier, Accessors : [Identifier]} ace]}}} 
                => compiler.Builder.Insert(new OpSDSLMixinUse(ace.ToString())),
            _ => throw new NotImplementedException()
        };
    }

    public override string ToString()
    {
        return $"if({Condition})\n{Body}";
    }
}

public class ElseIf(Expression condition, EffectStatement body, TextLocation info) : If(condition, body, info)
{
    public override string ToString()
    {
        return $"else if({Condition}){Body}";
    }
}

public class Else(EffectStatement body, TextLocation info) : EffectFlow(info)
{
    public EffectStatement Body { get; set; } = body;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        compiler.Builder.Insert(new OpSDSLElse());
        _ = Body switch
        {
            EffectExpressionStatement {Statement : ExpressionStatement { Expression : MethodCall {Name.Name : "mixin", Parameters.Values : [Identifier m]}}} 
                => compiler.Builder.Insert(new OpSDSLMixinUse(m.Name)),
            EffectExpressionStatement {Statement : ExpressionStatement { Expression : MethodCall {Name.Name : "mixin", Parameters.Values : [AccessorChainExpression {Source : Identifier, Accessors : [Identifier]} ace]}}} 
                => compiler.Builder.Insert(new OpSDSLMixinUse(ace.ToString())),
            _ => throw new NotImplementedException()
        };
    }
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
