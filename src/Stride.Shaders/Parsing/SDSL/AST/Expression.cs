using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Parsing.SDSL.AST;



/// <summary>
/// Code expression, represents operations and literals
/// </summary>
public abstract class Expression(TextLocation info) : ValueNode(info)
{
    public override void ProcessSymbol(SymbolTable table) => ProcessSymbol(table, null, null);
    public virtual void ProcessSymbol(SymbolTable table, EntryPoint? entrypoint = null, StreamIO? io = null) => throw new NotImplementedException($"Symbol table cannot process type : {GetType().Name}");
    public abstract SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler);
}

public class MethodCall(Identifier name, ShaderExpressionList parameters, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Parameters = parameters;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, module) = compiler;
        var list = parameters.Values;
        Span<IdRef> compiledParams = stackalloc IdRef[list.Count];
        var tmp = 0;
        foreach(var p in list)
            compiledParams[tmp++] = p.Compile(table, shader, compiler).Id;
        return builder.CallFunction(context, Name, compiledParams);
    }
    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Parameters)})";
    }
}

/// <summary>
/// Represents an accessed mixin.
/// </summary>
public class MixinAccess(Mixin mixin, TextLocation info) : Expression(info)
{
    public Mixin Mixin { get; set; } = mixin;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return $"{Mixin}";
    }
}


public abstract class UnaryExpression(Expression expression, Operator op, TextLocation info) : Expression(info)
{
    public Expression Expression { get; set; } = expression;
    public Operator Operator { get; set; } = op;
}

public class PrefixExpression(Operator op, Expression expression, TextLocation info) : UnaryExpression(expression, op, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class CastExpression(string typeName, Operator op, Expression expression, TextLocation info) : PrefixExpression(op, expression, info)
{
    public string TypeName { get; set; } = typeName;
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class PostfixIncrement(Operator op, TextLocation info) : Expression(info)
{
    public Operator Operator { get; set; } = op;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return $"{Operator.ToSymbol()}";
    }
}

public class AccessorChainExpression(Expression source, TextLocation info) : Expression(info)
{
    public Expression Source { get; set; } = source;
    public List<Expression> Accessors { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table, EntryPoint? entrypoint = null, StreamIO? io = null)
    {
        if (Source is Identifier { Name: "streams" } streams && Accessors[0] is Identifier streamVar)
        {
            table.CurrentFunctionSymbols.Add(table.Streams);
            streamVar.ProcessSymbol(table, entrypoint, io);
            Type = streamVar.Type;

            ProcessAccessors();

            table.Pop();
            table.RootSymbols.StreamUsages.Add(new(streamVar, SymbolKind.Variable, Storage.Stream), new(entrypoint ?? EntryPoint.None, io ?? StreamIO.Output));
        }
        else
        {
            Source.ProcessSymbol(table, entrypoint, io ?? StreamIO.Output);
            Type = Source.Type;
            ProcessAccessors();
        }

        void ProcessAccessors()
        {
            foreach (var accessor in Accessors)
            {
                if (Type is not null && Type.TryAccess(accessor, out var type))
                {
                    Type = type;
                    accessor.Type = type;
                }
                else throw new NotImplementedException($"Cannot access {accessor.GetType().Name} from {Type}");
                if(accessor is not Identifier)
                    accessor.ProcessSymbol(table, entrypoint, io ?? StreamIO.Input);
            }
        }
    }
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, _) = compiler;
        var source = Source.Compile(table, shader, compiler);
        var variable = context.Bound++;

        if (Source is Identifier { Name: "streams" } streams && Accessors[0] is Identifier streamVar)
            throw new NotImplementedException();

        var currentType = Source.Type;
        Span<IdRef> indexes = stackalloc IdRef[Accessors.Count - 1];
        foreach (var accessor in Accessors)
        {
            if (currentType is StructType s && accessor is Identifier field)
            {
                var index = s.TryGetFieldIndex(field);
                if (index == -1)
                    throw new InvalidOperationException($"field {accessor} not found in struct type {s}");
            }
            else throw new NotImplementedException($"unknown accessor {accessor} in expression {this}");

            currentType = accessor.Type;
        }

        var resultType = context.GetOrRegister(Type);
        var result = builder.Buffer.InsertOpAccessChain(builder.Position, variable, resultType, source.Id, indexes);
        builder.Position += result.WordCount;
        return new(result, resultType);
    }

    public override string ToString()
    {
        var builder = new StringBuilder().Append(Source);
        foreach (var a in Accessors)
            if (a is NumberLiteral)
                builder.Append('[').Append(a).Append(']');
            else if (a is PostfixIncrement)
                builder.Append(a);
            else
                builder.Append('.').Append(a);
        return builder.ToString();
    }
}

public class BinaryExpression(Expression left, Operator op, Expression right, TextLocation info) : Expression(info)
{
    public Operator Op { get; set; } = op;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

    public override void ProcessSymbol(SymbolTable table, EntryPoint? entrypoint, StreamIO? io)
    {
        Left.ProcessSymbol(table, entrypoint, io);
        Right.ProcessSymbol(table, entrypoint, io);
        if (
            OperatorTable.BinaryOperationResultingType(
                Left.Type ?? throw new NotImplementedException("Missing type"),
                Right.Type ?? throw new NotImplementedException("Missing type"),
                Op,
                out var t
            )
        )
            Type = t;
        else
            table.Errors.Add(new(Info, SDSLErrorMessages.SDSL0104));
    }

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var left = Left.Compile(table, shader, compiler);
        var right = Right.Compile(table, shader, compiler);
        var (builder, context, _) = compiler;
        return builder.BinaryOperation(context, context.GetOrRegister(Type), left, Op, right);
    }

    public override string ToString()
    {
        return $"( {Left} {Op.ToSymbol()} {Right} )";
    }
}

public class TernaryExpression(Expression cond, Expression left, Expression right, TextLocation info) : Expression(info)
{
    public Expression Condition { get; set; } = cond;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

    public override void ProcessSymbol(SymbolTable table)
    {
        Condition.ProcessSymbol(table);
        Left.ProcessSymbol(table);
        Right.ProcessSymbol(table);
        if (Condition.Type is not ScalarType { TypeName: "bool" })
            table.Errors.Add(new(Condition.Info, SDSLErrorMessages.SDSL0106));
        if (Left.Type != Right.Type)
            table.Errors.Add(new(Condition.Info, SDSLErrorMessages.SDSL0106));
    }

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException(); 
    }

    public override string ToString()
    {
        return $"({Condition} ? {Left} : {Right})";
    }
}