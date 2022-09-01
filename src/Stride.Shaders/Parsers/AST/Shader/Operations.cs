using Eto.Parse;
using Stride.Shaders.Parsing.AST.Directives;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Parsing.AST.Shader.OperatorTokenExtensions;

namespace Stride.Shaders.Parsing.AST.Shader;


public abstract class Expression : ShaderTokenTyped
{
    protected string? inferredType;
    public override string InferredType
    {
        get => inferredType ?? throw new NotImplementedException();
        set => inferredType = value;
    }

    public string GetInferredType()
    {
        return InferredType;
    }
    public override void TypeCheck(SymbolTable symbols, string expected = "") { }
}

public class Operation : Expression, IStreamCheck, IStaticCheck
{
    public OperatorToken Op { get; set; }

    public ShaderTokenTyped Left { get; set; }
    public ShaderTokenTyped Right { get; set; }
    
    public override async void TypeCheck(SymbolTable symbols, string expected = "")
    {
        
        if(expected != string.Empty)
        {
            Left.TypeCheck(symbols,expected);
            Right.TypeCheck(symbols,expected);
            if (Left.InferredType == Right.InferredType && Left.InferredType == expected)
                InferredType = Left.InferredType;
            else
                throw new NotImplementedException();
        }
        else 
        {
            Left.TypeCheck(symbols);
            Right.TypeCheck(symbols);
            if(Left.InferredType != Right.InferredType)
                throw new NotImplementedException();
            else
                InferredType = Left.InferredType;
        }
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
}


public class MulExpression : Operation
{
    public static MulExpression Create(Match m)
    {
        var first = new MulExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        MulExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new MulExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class SumExpression : Operation
{
    public static SumExpression Create(Match m)
    {
        var first = new SumExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        SumExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new SumExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class ShiftExpression : Operation
{
    public static ShiftExpression Create(Match m)
    {
        var first = new ShiftExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        ShiftExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new ShiftExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class AndExpression : Operation
{
    public static AndExpression Create(Match m)
    {
        var first = new AndExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        AndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new AndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}
public class XorExpression : Operation
{
    public static XorExpression Create(Match m)
    {
        var first = new XorExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        XorExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new XorExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}
public class OrExpression : Operation
{
    public static OrExpression Create(Match m)
    {
        var first = new OrExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        OrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new OrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class TestExpression : Operation
{
    public static TestExpression Create(Match m)
    {
        var first = new TestExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        TestExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new TestExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class EqualsExpression : Operation
{
    public static EqualsExpression Create(Match m)
    {
        var first = new EqualsExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        EqualsExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new EqualsExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class LogicalAndExpression : Operation
{
    public static LogicalAndExpression Create(Match m)
    {
        var first = new LogicalAndExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        LogicalAndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalAndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }

}
public class LogicalOrExpression : Operation
{
    public static LogicalOrExpression Create(Match m)
    {
        var first = new LogicalOrExpression
        {
            Match = m,
            Op = m.Matches[1].StringValue.ToOperatorToken(),
            Left = (ShaderTokenTyped)GetToken(m.Matches[0]),
            Right = (ShaderTokenTyped)GetToken(m.Matches[2])
        };

        LogicalOrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalOrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = (ShaderTokenTyped)GetToken(m.Matches[i + 1])
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

    public ConditionalExpression(Match m)
    {
        Condition = (ShaderTokenTyped)GetToken(m.Matches[0]);
        TrueOutput = (ShaderTokenTyped)GetToken(m.Matches[1]);
        FalseOutput = (ShaderTokenTyped)GetToken(m.Matches[2]);
    }
}


public class MethodCall : Expression
{
    public string MethodName { get; set; }
    public IEnumerable<Expression> Parameters { get; set; }

    public MethodCall(Match m)
    {
        Match = m;
        MethodName = m.Matches.First().StringValue;
        Parameters = m.Matches.Where(x => x.Name == "PrimaryExpression").Select(GetToken).Cast<Expression>().ToList();
    }
}
