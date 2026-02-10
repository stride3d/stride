using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDFX.AST;


public partial class ShaderEffect(TypeName name, bool isPartial, TextLocation info) : ShaderDeclaration(info)
{
    public TypeName Name { get; set; } = name;

    public BlockStatement Block { get; set; }
    public bool IsPartial { get; set; } = isPartial;

    public override string ToString() => Block.ToString();

    public void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        compiler.Builder.Insert(new OpSDSLEffect(Name.Name));
        Block.ProcessSymbol(table);
        Block.Compile(table, compiler);
    }

    internal static int[] CompileGenerics(SymbolTable table, SpirvContext context, ShaderExpressionList? generics)
    {
        var genericCount = generics != null ? generics.Values.Count : 0;
        var genericValues = new int[genericCount];
        if (genericCount > 0)
        {
            int genericIndex = 0;
            foreach (var generic in generics)
            {
                if (generic is not Literal literal)
                    throw new InvalidOperationException($"Generic value {generic} is not a literal");
                generic.ProcessSymbol(table);
                var compiledValue = generic.CompileConstantValue(table, context);
                genericValues[genericIndex++] = compiledValue.Id;
            }
        }

        return genericValues;
    }
}

public abstract class EffectStatement(TextLocation info) : Statement(info)
{
}

public partial class ShaderSourceDeclaration(Identifier name, TextLocation info, Expression? value = null) : EffectStatement(info)
{
    public Identifier Name { get; set; } = name;
    public Expression? Value { get; set; } = value;
    public bool IsCollection => Name.Name.Contains("Collection");

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"ShaderSourceCollection {Name} = {Value}";
    }
}

public partial class UsingParams(Identifier name, TextLocation info) : EffectStatement(info)
{
    public Identifier ParamsName { get; set; } = name;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, _) = compiler;
        builder.Insert(new OpSDSLParamsUse(ParamsName.Name));
    }

    public override string ToString()
    {
        return $"using params {ParamsName}";
    }
}

public partial class EffectDiscardStatement(TextLocation info) : EffectStatement(info)
{
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}


