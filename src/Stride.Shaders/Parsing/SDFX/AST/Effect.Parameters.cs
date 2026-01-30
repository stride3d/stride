using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDFX.AST;


public class EffectParameters(TypeName name, TextLocation info) : ShaderDeclaration(info)
{
    public TypeName Name { get; set; } = name;
    public List<EffectParameter> Parameters { get; set; } = [];

    public void Compile(SymbolTable table, CompilerUnit compiler)
    {
        compiler.Builder.Insert(new OpSDSLParams(Name.Name));
        foreach(var parameter in Parameters)
            parameter.Compile(table, compiler);
    }
}


public class EffectParameter(TypeName type, Identifier identifier, TextLocation info, Expression? value = null) : Node(info)
{
    public TypeName Type { get; set; } = type;
    public Identifier Identifier { get; set;} = identifier;
    public Expression? DefaultValue { get; set; } = value;

    public void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (_, context) = compiler;
        context.Add(new OpSDSLParamsField(Identifier, Type));
    }
}