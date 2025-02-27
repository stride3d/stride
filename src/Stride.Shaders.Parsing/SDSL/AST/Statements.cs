using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;

namespace Stride.Shaders.Parsing.SDSL.AST;

public abstract class Statement(TextLocation info) : ValueNode(info);

public class EmptyStatement(TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => Scalar.From("void"); set { } }
    public override string ToString() => ";";
}

public class ExpressionStatement(Expression expression, TextLocation info) : Statement(info)
{
    public override SymbolType? Type { get => Expression.Type; set { } }
    public Expression Expression { get; set; } = expression;
    public override string ToString()
    {
        return $"{Expression};";
    }
}

public class Return(TextLocation info, Expression? expression = null) : Statement(info)
{
    public override SymbolType? Type { get => Value?.Type ?? Scalar.From("void"); set { } }
    public Expression? Value { get; set; } = expression;

    public override string ToString()
    {
        return $"return {Value};";
    }
}

public abstract class Declaration(TypeName typename, TextLocation info) : Statement(info)
{
    public TypeName TypeName { get; set; } = typename;
}

public class VariableAssign(Expression variable, bool isConst, TextLocation info, AssignOperator? op = null, Expression? value = null) : Node(info)
{
    public Expression Variable { get; set; } = variable;
    public AssignOperator? Operator { get; set; } = op;
    public Expression? Value { get; set; } = value;
    public bool IsConst { get; set; } = isConst;

    public override string ToString()
        => Value switch
        {
            null => Variable.ToString() ?? "",
            Expression v => $"{Variable} {Operator?.ToAssignSymbol()} {v}"
        };
}
public class DeclaredVariableAssign(Identifier variable, bool isConst, TextLocation info, AssignOperator? op = null, Expression? value = null) : Node(info)
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

    public override void ProcessSymbol(SymbolTable table)
    {
        if (TypeName == "var")
        {
            if (Variables.Count == 1 && Variables[0].Value is not null)
            {
                Variables[0].Value?.ProcessSymbol(table);
                Type = Variables[0].Value!.Type;
            }
            else
                table.Errors.Add(new(Info, SDSLErrorMessages.SDSL0104));
        }
        else
        {
            Type = TypeName.ToSymbol();
            table.DeclaredTypes.TryAdd(TypeName.ToString(), Type);
            foreach (var d in Variables)
            {
                d.Value?.ProcessSymbol(table);
                table.CurrentTable.Add(new(d.Variable, SymbolKind.Variable), new(new(d.Variable, SymbolKind.Variable), Type));
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
            if (variable.Variable is Identifier id)
            {
                if (table.TryFind(id, SymbolKind.Variable, out var symbol))
                    Type = symbol.Type;
                else throw new NotImplementedException();
            }
            else if (variable.Variable is AccessorChainExpression exp)
            {
                if (exp.Source is Identifier streams && streams == "streams")
                {
                    if (exp.Accessors[0] is not Identifier)
                        throw new NotImplementedException();
                    else
                    {
                        // Check type of first symbol
                        exp.Accessors[0].ProcessSymbol(table);
                        exp.Type = exp.Accessors[0].Type;
                        // If has more, dive into the type definition
                        // First case none
                        if (exp.Accessors.Count > 1)
                        {
                            foreach (var accessor in exp.Accessors[1..])
                            {
                                if (exp.Type is not null && exp.Type.TryAccess(accessor, out var type))
                                {
                                    exp.Type = type;
                                    accessor.Type = type;
                                }
                                else throw new NotImplementedException($"Cannot access {accessor.GetType().Name} from {exp.Type}");
                            }
                        }

                    }
                }
                else
                {
                    exp.Source.ProcessSymbol(table);
                    foreach (var accessor in exp.Accessors)
                    {
                        if (exp.Type is not null && exp.Type.TryAccess(accessor, out var type))
                        {
                            exp.Type = type;
                            accessor.Type = type;
                        }
                        else throw new NotImplementedException($"Cannot access {accessor.GetType().Name} from {exp.Type}");
                    }
                }
            }
            else throw new NotImplementedException();
            variable.Value?.ProcessSymbol(table);
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

    public override void ProcessSymbol(SymbolTable table)
    {
        foreach (var s in Statements)
            s.ProcessSymbol(table);
    }

    public override string ToString()
    {
        var builder = new StringBuilder().Append("Block : \n");
        foreach (var e in Statements)
            builder.AppendLine(e.ToString());
        return builder.AppendLine("End").ToString();
    }
}
