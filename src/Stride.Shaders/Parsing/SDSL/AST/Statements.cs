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
    public abstract void Compile(SymbolTable table, CompilerUnit compiler);
}

public class EmptyStatement(TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => ScalarType.From("void"); set { } }
    public override void Compile(SymbolTable table, CompilerUnit compiler) { }
    public override string ToString() => ";";
}

public class ExpressionStatement(Expression expression, TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => Expression.Type; set { } }
    public Expression Expression { get; set; } = expression;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        Expression.Compile(table, compiler);
        Type = ScalarType.From("void");
    }
    public override string ToString()
    {
        return $"{Expression};";
    }
}

public class Return(TextLocation info, Expression? expression = null) : Statement(info)
{
    public Expression? Value { get; set; } = expression;

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        SpirvValue? returnValue = null;

        Type = builder.CurrentFunction!.Value.FunctionType.ReturnType;
        if (Value != null)
        {
            var value = Value.CompileAsValue(table, compiler);
            returnValue = builder.Convert(context, value, Type);
        }
        builder.Return(returnValue);
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

    public override void Compile(SymbolTable table, CompilerUnit compiler)
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
    public TypeName TypeName { get; set; } = new("void", info);
    public List<Expression>? ArraySizes
    {
        get => TypeName.ArraySize;
        set => TypeName.ArraySize = value;
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        Variable.Type = TypeName.ResolveType(table, context);
        var initialValue = Value?.CompileAsValue(table, compiler);
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

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var compiledValues = new SpirvValue[Variables.Count];
        for (var index = 0; index < Variables.Count; index++)
        {
            if (Variables[index].Value != null)
                compiledValues[index] = Variables[index].Value!.CompileAsValue(table, compiler);
        }

        // Compute type
        SymbolType valueType;
        if (TypeName == "var")
        {
            if (Variables.Count == 1 && Variables[0].Value is not null)
            {
                valueType = Variables[0].Value!.ValueType;
            }
            else
            {
                table.Errors.Add(new(Info, SDSLErrorMessages.SDSL0104));
                return;
            }
        }
        else
        {
            valueType = TypeName.ResolveType(table, context);
            table.DeclaredTypes.TryAdd(TypeName.ToString(), valueType);
        }

        if (valueType is PointerType)
            throw new InvalidOperationException();

        Type = new PointerType(valueType, Specification.StorageClass.Function);

        var registeredType = context.GetOrRegister(Type);
        for (var index = 0; index < Variables.Count; index++)
        {
            var d = Variables[index];

            var variable = context.Bound++;
            builder.AddFunctionVariable(registeredType, variable);
            context.AddName(variable, d.Variable);

            table.CurrentFrame.Add(d.Variable, new(new(d.Variable, SymbolKind.Variable), Type, variable));

            if (builder.CurrentFunction is SpirvFunction f)
                f.Variables.Add(d.Variable, new(variable, registeredType, d.Variable));

            if (d.Value != null)
            {
                var source = compiledValues[index];

                // Make sure type is correct
                source = builder.Convert(context, source, valueType);

                builder.Insert(new OpStore(variable, source.Id, null));
            }
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

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        foreach (var variable in Variables)
        {
            var target = variable.Variable.Compile(table, compiler);
            var source = variable.Value!.CompileAsValue(table, compiler);
            if (variable.Variable.Type is not PointerType p)
                throw new InvalidOperationException("can only assign to pointer type");

            if (variable.Operator != AssignOperator.Simple)
            {
                var binaryOperator = (variable.Operator) switch
                {
                    AssignOperator.Plus => Operator.Plus,
                    AssignOperator.Minus => Operator.Minus,
                    AssignOperator.Mul => Operator.Mul,
                    AssignOperator.Div => Operator.Div,
                    AssignOperator.Mod => Operator.Mod,
                    AssignOperator.RightShift => Operator.RightShift,
                    AssignOperator.LeftShift => Operator.LeftShift,
                    AssignOperator.AND => Operator.AND,
                    AssignOperator.OR => Operator.OR,
                    AssignOperator.XOR => Operator.XOR,
                };

                var left = builder.AsValue(context, target);
                var right = builder.AsValue(context, source);

                source = builder.BinaryOperation(context, left, binaryOperator, right);
            }

            // Make sure to convert to proper type
            var resultType = target.GetValueType(context, true);
            source = builder.Convert(context, source, resultType);

            if (target.Swizzles != null)
            {
                var valueType = context.Types[p.BaseType];
                var loadId = builder.Insert(new OpLoad(valueType, context.Bound++, target.Id, null)).ResultId;
                // Shuffle with new data
                switch (p.BaseType)
                {
                    case VectorType v:
                        Span<int> shuffleIndices = stackalloc int[v.Size];
                        // Default: source values
                        for (int j = 0; j < v.Size; ++j)
                            shuffleIndices[j] = j;
                        // Update using swizzle target (from 2nd new value vector)
                        for (int j = 0; j < target.Swizzles.Length; ++j)
                            shuffleIndices[target.Swizzles[j]] = v.Size + j;
                        source = new(builder.InsertData(new OpVectorShuffle(valueType, context.Bound++, loadId, source.Id, new(shuffleIndices))));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            builder.Insert(new OpStore(target.Id, source.Id, null));
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

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        table.Push();
        var (builder, context) = compiler;
        foreach (var s in Statements)
        {
            s.Compile(table, compiler);
            if (SpirvBuilder.IsBlockTermination(builder.GetLastInstructionType()))
                break;
        }

        table.Pop();
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
