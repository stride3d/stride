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
    /// <summary>
    /// Compute <see cref="Type"/> and optionally emit diagnostics.
    /// </summary>
    /// <param name="table"></param>
    public virtual void ProcessSymbol(SymbolTable table) => throw new NotImplementedException($"Symbol table cannot process type : {GetType().Name}");

    public abstract void Compile(SymbolTable table, CompilerUnit compiler);
}

public class EmptyStatement(TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => ScalarType.Void; set { } }
    public override void Compile(SymbolTable table, CompilerUnit compiler) { }
    public override string ToString() => ";";
}

public class ExpressionStatement(Expression expression, TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => Expression.Type; set { } }
    public Expression Expression { get; set; } = expression;
    
    public override void ProcessSymbol(SymbolTable table)
    {
        Expression.ProcessSymbol(table);
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        Expression.ProcessSymbol(table);
        Expression.Compile(table, compiler);
        Type = ScalarType.Void;
    }
    public override string ToString()
    {
        return $"{Expression};";
    }
}

public class Return(TextLocation info, Expression? expression = null) : Statement(info)
{
    public Expression? Value { get; set; } = expression;
    
    public override void ProcessSymbol(SymbolTable table)
    {
        Value?.ProcessSymbol(table);
        Type = Value?.Type ?? ScalarType.Void;
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        SpirvValue? returnValue = null;

        var realReturnType = builder.CurrentFunction!.Value.FunctionType.ReturnType;
        if (Value != null)
        {
            var value = Value.CompileAsValue(table, compiler);
            returnValue = builder.Convert(context, value, realReturnType);
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
    
    public override void ProcessSymbol(SymbolTable table)
    {
        Value?.ProcessSymbol(table, TypeName.Type);
        SymbolType valueType;
        if (TypeName.Name == "var")
        {
            if (Value == null)
                table.Errors.Add(new(Info, "can't infer `var` type without a value"));
            valueType = Value.ValueType;
        }
        else
        {
            TypeName.ProcessSymbol(table);
            valueType = TypeName.Type;
        }
        Type = new PointerType(valueType, Specification.StorageClass.Function);
        Variable.Type = Type;

        // TODO: type check with conversion allowed
        //if (Value is not null && Value.Type != Variable.Type)
        //    table.AddError(new(TypeName.Info, "wrong type"));
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        TypeName.ProcessSymbol(table);
        var variableValueType = TypeName.Type;
        var initialValue = Value?.CompileAsValue(table, compiler, variableValueType);
        if (Value is not null && Value.ValueType != variableValueType)
            table.AddError(new(TypeName.Info, "wrong type"));

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

    public List<Symbol> VariableSymbols { get; } = new();

    public override void ProcessSymbol(SymbolTable table)
    {
        VariableSymbols.Clear();
        for (var index = 0; index < Variables.Count; index++)
        {
            var declaration = Variables[index];
            declaration.TypeName = new TypeName(TypeName.Name, info) { ArraySize = declaration.ArraySizes };
            declaration.ProcessSymbol(table);
            
            var variableSymbol = new Symbol(new(declaration.Variable, SymbolKind.Variable), declaration.Type, 0, OwnerType: table.CurrentShader);
            table.CurrentFrame.Add(declaration.Variable, variableSymbol);
            VariableSymbols.Add(variableSymbol);
        }
        
        Type = Variables[0].Type;
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        for (var index = 0; index < Variables.Count; index++)
        {
            var d = Variables[index];

            var variable = context.Bound++;
            var variableType = (PointerType)d.Type;
            var variableValueType = variableType.BaseType;
            var variableTypeId = context.GetOrRegister(variableType);
            builder.AddFunctionVariable(variableTypeId, variable);
            context.AddName(variable, d.Variable);

            VariableSymbols[index].IdRef = variable;

            // Check initial value
            if (d.Value != null)
            {
                var source = Variables[index].Value!.CompileAsValue(table, compiler, variableValueType);

                // Make sure type is correct
                source = builder.Convert(context, source, variableValueType);

                builder.Insert(new OpStore(variable, source.Id, null, []));
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

    public override void ProcessSymbol(SymbolTable table)
    {
        foreach (var variable in Variables)
        {
            variable.Variable.ProcessSymbol(table);
            variable.Value!.ProcessSymbol(table);
        }
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        foreach (var variable in Variables)
        {
            var target = variable.Variable.Compile(table, compiler);
            var source = variable.Value!.CompileAsValue(table, compiler);

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

                source = builder.BinaryOperation(table, context, left, binaryOperator, right, info);
            }

            // Make sure to convert to proper type
            var resultType = target.GetValueType(context);
            source = builder.Convert(context, source, resultType);

            variable.Variable.SetValue(table, compiler, source);
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

    public SymbolFrame SymbolFrame;

    public override void ProcessSymbol(SymbolTable table)
    {
        table.Push();
        foreach (var statement in Statements)
            statement.ProcessSymbol(table);
        SymbolFrame = table.Pop();
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        table.Push(SymbolFrame);
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
