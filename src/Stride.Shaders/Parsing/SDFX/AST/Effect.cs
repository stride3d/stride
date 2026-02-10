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

public partial class MixinUse(List<Mixin> mixin, TextLocation info) : EffectStatement(info)
{
    public List<Mixin> MixinName { get; set; } = mixin;

    public override void ProcessSymbol(SymbolTable table)
    {
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        foreach (var mixinName in MixinName)
        {
            if (mixinName.Path.Count > 0)
                throw new NotImplementedException();

            int[] genericValues = ShaderEffect.CompileGenerics(table, compiler.Context, mixinName.Generics);

            compiler.Builder.Insert(new OpSDSLMixin(mixinName.Name, [.. genericValues]));
        }
    }

    public override string ToString()
    {
        return $"mixin {MixinName}";
    }
}
public partial class MixinChild(Mixin mixin, TextLocation info) : EffectStatement(info)
{
    public Mixin MixinName { get; set; } = mixin;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"mixin child {MixinName}";
    }
}

public partial class MixinClone(Mixin mixin, TextLocation info) : EffectStatement(info)
{
    public Mixin MixinName { get; set; } = mixin;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"mixin clone {MixinName}";
    }
}

public partial class MixinConst(string identifier, TextLocation info) : EffectStatement(info)
{
    public string Identifier { get; set; } = identifier;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

}

public abstract class Composable();


public abstract class ComposeValue(TextLocation info) : Node(info)
{
    public abstract void Compile(SymbolTable table, CompilerUnit compiler, Identifier identifier, AssignOperator @operator);
}

public partial class ComposePathValue(string path, TextLocation info) : ComposeValue(info)
{
    public string Path { get; set; } = path;

    public override void Compile(SymbolTable table, CompilerUnit compiler, Identifier identifier, AssignOperator @operator)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return Path.ToString();
    }
}
public partial class ComposeMixinValue(Mixin mixin, TextLocation info) : ComposeValue(info)
{
    public Mixin Mixin { get; set; } = mixin;

    public override void Compile(SymbolTable table, CompilerUnit compiler, Identifier identifier, AssignOperator @operator)
    {
        var (builder, context) = compiler;

        if (Mixin.Path.Count > 0)
            throw new NotImplementedException();

        var generics = ShaderEffect.CompileGenerics(table, context, Mixin.Generics);

        switch (@operator)
        {
            case AssignOperator.Simple:
                compiler.Builder.Insert(new OpSDSLMixinCompose(identifier.Name, Mixin.Name.Name, new(generics)));
                break;
            case AssignOperator.Plus:
                compiler.Builder.Insert(new OpSDSLMixinComposeArray(identifier.Name, Mixin.Name.Name, new(generics)));
                break;
            default:
                throw new ArgumentException(null, nameof(@operator));
        }
    }


    public override string ToString()
    {
        return Mixin.ToString();
    }
}

public partial class MixinCompose(Identifier identifier, AssignOperator op, ComposeValue value, TextLocation info) : EffectStatement(info)
{
    public Identifier Identifier { get; set; } = identifier;
    AssignOperator Operator { get; set; } = op;
    public ComposeValue ComposeValue { get; set; } = value;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        ComposeValue.Compile(table, compiler, Identifier, Operator);
    }


    public override string ToString()
    {
        return $"mixin compose {Identifier} {Operator.ToAssignSymbol()} {ComposeValue}";
    }
}
public partial class MixinComposeAdd(Identifier identifier, Identifier source, TextLocation info) : EffectStatement(info)
{
    public Identifier Identifier { get; set; } = identifier;
    public Identifier Source { get; set; } = source;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"mixin compose {Identifier} += {Source}";
    }
}

public partial class ComposeParams(Mixin mixin, TextLocation info) : EffectStatement(info)
{
    public Mixin MixinName { get; set; } = mixin;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
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


