using Eto.Parse;
using SDSL.Parsing.AST.Directives;
using SDSL.Parsing.AST.Shader.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SDSL.Parsing.AST.Shader.OperatorTokenExtensions;

namespace SDSL.Parsing.AST.Shader;


public abstract class Expression : ShaderTokenTyped
{
    protected SymbolType? inferredType;
    public override SymbolType? InferredType
    {
        get => inferredType ?? throw new NotImplementedException();
        set => inferredType = value;
    }
    public override void TypeCheck(SymbolTable symbols, in SymbolType? expected) { }
}

public class Operation : Expression, IStreamCheck, IStaticCheck, IVariableCheck
{
    public OperatorToken Op { get; set; }

    public ShaderTokenTyped Left { get; set; }
    public ShaderTokenTyped Right { get; set; }

    public override void TypeCheck(SymbolTable symbols, in SymbolType? expected)
    {

        if (expected != null)
        {
            Left.TypeCheck(symbols, expected);
            Right.TypeCheck(symbols, expected);
            if (Left.InferredType.Equals(Right.InferredType) && Left.InferredType.Equals(expected))
                InferredType = Left.InferredType;
            else
                throw new NotImplementedException();
        }
        else
        {
            Left.TypeCheck(symbols,expected);
            Right.TypeCheck(symbols,expected);
            if (Left.InferredType != Right.InferredType)
            {
                // CheckImplicitCasting(Left, Right, expected);
            }
            else
                InferredType = Left.InferredType;
        }
    }

    public IEnumerable<string> GetUsedStream()
    {
        var result = Enumerable.Empty<string>();
        if (Left is IStreamCheck lsc)
            result = result.Concat(lsc.GetUsedStream());
        if (Right is IStreamCheck rsc)
            result = result.Concat(rsc.GetUsedStream());
        return result;
    }
    public IEnumerable<string> GetAssignedStream()
    {
        return Enumerable.Empty<string>();
    }

    public bool CheckStream(SymbolTable s)
    {
        return Left is IStreamCheck scl && scl.CheckStream(s)
            || Right is IStreamCheck scr && scr.CheckStream(s);
    }

    public bool CheckStatic(SymbolTable s)
    {
        return Left is IStaticCheck scl && scl.CheckStatic(s)
            || Right is IStaticCheck scr && scr.CheckStatic(s);
    }

    public void CheckVariables(SymbolTable s)
    {
        if(Left is IVariableCheck lvc) lvc.CheckVariables(s);
        if(Right is IVariableCheck rvc) rvc.CheckVariables(s);
    }
    public void CheckImplicitCasting(ShaderTokenTyped l, ShaderTokenTyped r, string expected)
    {
        InferredType = (l.InferredType, r.InferredType, expected) switch
        {
            // ("int","float", "int") => "int",
            // ("int","float", "float") => "float",
            // ("float","int", "int") => "int",
            // ("float","int", "float") => "float",
            // ("int","float", "") => "float",
            // ("float","int", "") => "float",
            _ => throw new Exception($"Cannot cast types")
        };
    }
}


public class MulExpression : Operation
{
    public static MulExpression Create(Match m, SymbolTable s)
    {
        var first = new MulExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        MulExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new MulExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}

public class SumExpression : Operation
{
    public static SumExpression Create(Match m, SymbolTable s)
    {
        var first = new SumExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        SumExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new SumExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}

public class ShiftExpression : Operation
{
    public static ShiftExpression Create(Match m, SymbolTable s)
    {
        var first = new ShiftExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        ShiftExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new ShiftExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}

public class AndExpression : Operation
{
    public static AndExpression Create(Match m, SymbolTable s)
    {
        var first = new AndExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        AndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new AndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}
public class XorExpression : Operation
{
    public static XorExpression Create(Match m, SymbolTable s)
    {
        var first = new XorExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        XorExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new XorExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}
public class OrExpression : Operation
{
    public static OrExpression Create(Match m, SymbolTable s)
    {
        var first = new OrExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        OrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new OrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}

public class TestExpression : Operation
{
    public static TestExpression Create(Match m, SymbolTable s)
    {
        var first = new TestExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        TestExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new TestExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}

public class EqualsExpression : Operation
{
    public static EqualsExpression Create(Match m, SymbolTable s)
    {
        var first = new EqualsExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        EqualsExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new EqualsExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}

public class LogicalAndExpression : Operation
{
    public static LogicalAndExpression Create(Match m, SymbolTable s)
    {
        var first = new LogicalAndExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        LogicalAndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalAndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }

}
public class LogicalOrExpression : Operation
{
    public static LogicalOrExpression Create(Match m, SymbolTable s)
    {
        var first = new LogicalOrExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0], s),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2], s)
        };

        LogicalOrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalOrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1], s)
            };
        }
        return tmp;
    }
}

public class ConditionalExpression : Expression
{
    public ShaderTokenTyped Condition { get; set; }
    public ShaderTokenTyped TrueOutput { get; set; }
    public ShaderTokenTyped FalseOutput { get; set; }

    string? inferredType;

    public string InferredType
    {
        get => inferredType;
        set => inferredType = value;
    }

    public ConditionalExpression(Match m, SymbolTable s)
    {
        Condition = (ShaderTokenTyped)GetToken(m.Matches[0], s);
        TrueOutput = (ShaderTokenTyped)GetToken(m.Matches[1], s);
        FalseOutput = (ShaderTokenTyped)GetToken(m.Matches[2], s);
    }
}


public class MethodCall : Expression
{
    public string MethodName { get; set; }
    public IEnumerable<Expression> Parameters { get; set; }

    public MethodCall(Match m, SymbolTable s)
    {
        Match = m;
        MethodName = m.Matches.First().StringValue;
        throw new NotImplementedException();
        // Parameters = m.Matches.Where(x => x.Name == "PrimaryExpression").Select(GetToken).Cast<Expression>().ToList();
    }
}

public class ValueMethodCall : Expression
{
    public string MethodName { get; set; }
    public IEnumerable<Expression> Parameters { get; set; }

    public ValueMethodCall(Match m, SymbolTable s)
    {
        Match = m;
        MethodName = m.Matches.First().StringValue;
        inferredType = s.Tokenize(m["ValueTypes"]);
        Parameters = m.Matches.Where(x => x.Name == "PrimaryExpression").Select(x => GetToken(x, s)).Cast<Expression>().ToList();
    }
    public override void TypeCheck(SymbolTable symbols, ISymbolType expected)
    {
        if(!inferredType.Equals(expected))
            symbols.AddError(Match, $"cannot cast {inferredType} to {expected}");
    }
}
