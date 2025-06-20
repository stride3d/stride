using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Parsing.SDSL.AST;

public abstract class Statement(TextLocation info) : ValueNode(info)
{
    public abstract void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler);
}

public class EmptyStatement(TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => ScalarType.From("void"); set { } }
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler) { }
    public override string ToString() => ";";
}

public class ExpressionStatement(Expression expression, TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => Expression.Type; set { } }
    public Expression Expression { get; set; } = expression;

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Expression.Compile(table, shader, compiler);
        Type = ScalarType.From("void");
    }
    public override string ToString()
    {
        return $"{Expression};";
    }
}

public class Return(TextLocation info, Expression? expression = null) : Statement(info)
{
    public override SymbolType? Type { get => Value?.Type ?? ScalarType.From("void"); set { } }
    public Expression? Value { get; set; } = expression;

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, _, _) = compiler;
        builder.Return(Value?.Compile(table, shader, compiler));
        Type = Value?.Type ?? ScalarType.From("void");
    }
    public override string ToString()
    {
        return $"return {Value};";
    }
}

public abstract class Declaration(TypeName typename, TextLocation info) : Statement(info)
{
    public TypeName TypeName { get; set; } = typename;
}

public class VariableAssign(Expression variable, bool isConst, TextLocation info, AssignOperator? op = null, Expression? value = null) : Statement(info)
{
    public Expression Variable { get; set; } = variable;
    public AssignOperator? Operator { get; set; } = op;
    public Expression? Value { get; set; } = value;
    public bool IsConst { get; set; } = isConst;

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
        => Value switch
        {
            null => Variable.ToString() ?? "",
            Expression v => $"{Variable} {Operator?.ToAssignSymbol()} {v}"
        };
}
public class DeclaredVariableAssign(Identifier variable, bool isConst, TextLocation info, AssignOperator? op = null, Expression? value = null) : Statement(info)
{
    public Identifier Variable { get; set; } = variable;
    public AssignOperator? Operator { get; set; } = op;
    public Expression? Value { get; set; } = value;
    public bool IsConst { get; set; } = isConst;
    public TypeName TypeName { get; set; } = new("void", info, false);
    public List<Expression>? ArraySizes
    {
        get => TypeName.ArraySize;
        set => TypeName.ArraySize = value;
    }

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Variable.Type = TypeName.ResolveType(table);
        var initialValue = Value?.Compile(table, shader, compiler);
        if (Value is not null && Value.Type != Variable.Type)
            table.Errors.Add(new(TypeName.Info, "wrong type"));

        throw new NotImplementedException();
    }

    internal void ReplaceTypeName(TypeName typeName)
    {
        TypeName.Type = typeName.Type;
        TypeName.Info = typeName.Info;
    }

    public override string ToString()
        => Value switch
        {
            null => Variable.ToString() ?? "",
            Expression v => $"{Variable} {Operator?.ToAssignSymbol()} {v}"
        };
}

public class Declare(TypeName typename, TextLocation info) : Declaration(typename, info)
{
    public List<DeclaredVariableAssign> Variables { get; set; } = [];

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var compiledValues = new SpirvValue[Variables.Count];
        for (var index = 0; index < Variables.Count; index++)
        {
            if (Variables[index].Value != null)
                compiledValues[index] = Variables[index].Value!.Compile(table, shader, compiler);
        }

        // Compute type
        if (TypeName == "var")
        {
            if (Variables.Count == 1 && Variables[0].Value is not null)
            {
                Type = Variables[0].Value!.Type;
            }
            else
                table.Errors.Add(new(Info, SDSLErrorMessages.SDSL0104));
        }
        else
        {
            Type = TypeName.ResolveType(table);
            table.DeclaredTypes.TryAdd(TypeName.ToString(), Type);
            foreach (var d in Variables)
            {
                table.CurrentFrame.Add(d.Variable, new(new(d.Variable, SymbolKind.Variable), Type));
            }
        }

        var (builder, context, _) = compiler;
        var registeredType = context.GetOrRegister(new PointerType(Type!));
        foreach (var d in Variables)
        {
            var variable = context.Bound++;
            var instruction = builder.Buffer.InsertOpVariable(builder.Position++, variable, registeredType, Specification.StorageClass.Function, null);
            context.AddName(variable, d.Variable);

            if (builder.CurrentFunction is SpirvFunction f)
                f.Variables.Add(d.Variable, new(variable, registeredType, d.Variable));
        }
    }
    public override string ToString()
    {
        return $"{TypeName} {string.Join(", ", Variables.Select(v => v.ToString()))}";
    }
}

public class Assign(TextLocation info) : Statement(info)
{
    public List<VariableAssign> Variables { get; set; } = [];

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, _) = compiler;
        foreach (var variable in Variables)
        {
            var target = variable.Variable.Compile(table, shader, compiler);
            var source = variable.Value!.Compile(table, shader, compiler);
            if (variable.Variable.Type is not PointerType)
                throw new InvalidOperationException("can only assign to pointer type");
            if (variable.Value!.Type is PointerType p)
            {
                var sourceLoad = context.Bound++;
                var underlyingType = context.GetOrRegister(p.BaseType);
                builder.Buffer.InsertOpLoad(builder.Position++, sourceLoad, underlyingType, source.Id, Specification.MemoryAccessMask.None);
                source = new(sourceLoad, underlyingType);
            }

            var instruction = builder.Buffer.InsertOpStore(builder.Position++, target.Id, source.Id, null);
        }
    }
    public override string ToString()
    {
        return string.Join(", ", Variables.Select(x => x.ToString())) + ";";
    }
}



public class BlockStatement(TextLocation info) : Statement(info)
{
    public List<Statement> Statements { get; set; } = [];

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, _) = compiler;
        builder.CreateBlock(context);
        foreach (var s in Statements)
            s.Compile(table, shader, compiler);

    }

    public List<Statement>.Enumerator GetEnumerator() => Statements.GetEnumerator();

    public override string ToString()
    {
        var builder = new StringBuilder().Append("Block : \n");
        foreach (var e in Statements)
            builder.AppendLine(e.ToString());
        return builder.AppendLine("End").ToString();
    }
}
