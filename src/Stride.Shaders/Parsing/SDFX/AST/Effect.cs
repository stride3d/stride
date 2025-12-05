using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDFX.AST;


public class ShaderEffect(TypeName name, bool isPartial, TextLocation info) : ShaderDeclaration(info)
{
    public TypeName Name { get; set; } = name;
    public List<EffectStatement> Members { get; set; } = [];
    public bool IsPartial { get; set; } = isPartial;

    public override string ToString()
    {
        return string.Join("", Members.Select(x => $"{x}\n"));
    }

    public void Compile(CompilerUnit compiler)
    {
        compiler.Builder.Insert(new OpSDSLEffect(Name.Name));

        foreach (var statement in Members)
        {
            statement.Compile(compiler);
        }

        compiler.Builder.Insert(new OpSDSLEffectEnd());
    }
}

public abstract class EffectStatement(TextLocation info) : Node(info)
{
    public abstract void Compile(CompilerUnit compiler);
}

public class ShaderSourceDeclaration(Identifier name, TextLocation info, Expression? value = null) : EffectStatement(info)
{
    public Identifier Name { get; set; } = name;
    public Expression? Value { get; set; } = value;
    public bool IsCollection => Name.Name.Contains("Collection");

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"ShaderSourceCollection {Name} = {Value}";
    }
}

public class EffectStatementBlock(TextLocation info) : EffectStatement(info)
{
    public List<EffectStatement> Statements { get; set; } = [];

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return string.Join("\n", Statements);
    }
}

public class MixinUse(List<Mixin> mixin, TextLocation info) : EffectStatement(info)
{
    public List<Mixin> MixinName { get; set; } = mixin;

    public override void Compile(CompilerUnit compiler)
    {
        foreach (var mixinName in MixinName)
        {
            if (mixinName.Generics != null || mixinName.Path.Count > 0)
                throw new NotImplementedException();

            compiler.Builder.Insert(new OpSDSLMixin(mixinName.Name));
        }
    }

    public override string ToString()
    {
        return $"mixin {MixinName}";
    }
}
public class MixinChild(Mixin mixin, TextLocation info) : EffectStatement(info)
{
    public Mixin MixinName { get; set; } = mixin;

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"mixin child {MixinName}";
    }
}

public class MixinClone(Mixin mixin, TextLocation info) : EffectStatement(info)
{
    public Mixin MixinName { get; set; } = mixin;

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"mixin clone {MixinName}";
    }
}

public class MixinConst(string identifier, TextLocation info) : EffectStatement(info)
{
    public string Identifier { get; set; } = identifier;

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

}

public abstract class Composable();


public abstract class ComposeValue(TextLocation info) : Node(info)
{
    public abstract void Compile(CompilerUnit compiler, Identifier identifier, AssignOperator @operator);
}

public class ComposePathValue(string path, TextLocation info) : ComposeValue(info)
{
    public string Path { get; set; } = path;

    public override void Compile(CompilerUnit compiler, Identifier identifier, AssignOperator @operator)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return Path.ToString();
    }
}
public class ComposeMixinValue(Mixin mixin, TextLocation info) : ComposeValue(info)
{
    public Mixin Mixin { get; set; } = mixin;

    public override void Compile(CompilerUnit compiler, Identifier identifier, AssignOperator @operator)
    {
        if (Mixin.Generics != null || Mixin.Path.Count > 0)
            throw new NotImplementedException();

        switch (@operator)
        {
            case AssignOperator.Simple:
                compiler.Builder.Insert(new OpSDSLMixinCompose(identifier.Name, Mixin.Name.Name));
                break;
            case AssignOperator.Plus:
                compiler.Builder.Insert(new OpSDSLMixinComposeArray(identifier.Name, Mixin.Name.Name));
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

public class MixinCompose(Identifier identifier, AssignOperator op, ComposeValue value, TextLocation info) : EffectStatement(info)
{
    public Identifier Identifier { get; set; } = identifier;
    AssignOperator Operator { get; set; } = op;
    public ComposeValue ComposeValue { get; set; } = value;

    public override void Compile(CompilerUnit compiler)
    {
        ComposeValue.Compile(compiler, Identifier, Operator);
    }


    public override string ToString()
    {
        return $"mixin compose {Identifier} {Operator.ToAssignSymbol()} {ComposeValue}";
    }
}
public class MixinComposeAdd(Identifier identifier, Identifier source, TextLocation info) : EffectStatement(info)
{
    public Identifier Identifier { get; set; } = identifier;
    public Identifier Source { get; set; } = source;

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"mixin compose {Identifier} += {Source}";
    }
}

public class ComposeParams(Mixin mixin, TextLocation info) : EffectStatement(info)
{
    public Mixin MixinName { get; set; } = mixin;

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

}
public class UsingParams(Identifier name, TextLocation info) : EffectStatement(info)
{
    public Identifier ParamsName { get; set; } = name;

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }


    public override string ToString()
    {
        return $"using params {ParamsName}";
    }
}

public class EffectBlock(TextLocation info) : EffectStatement(info)
{
    public List<EffectStatement> Statements { get; set; } = [];

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

}


public class EffectExpressionStatement(Statement statement, TextLocation info) : EffectStatement(info)
{
    public Statement Statement { get; set; } = statement;

    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class EffectDiscardStatement(TextLocation info) : EffectStatement(info)
{
    public override void Compile(CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}


