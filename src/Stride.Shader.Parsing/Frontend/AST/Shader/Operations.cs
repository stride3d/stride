using Eto.Parse;
using Stride.Shader.Parsing.AST.Directives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shader.Parsing.AST.Shader.OperatorTokenExtensions;

namespace Stride.Shader.Parsing.AST.Shader;

public abstract class Projector : ShaderToken
{
    public virtual Type InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public abstract ShaderToken ProjectConstant();
}

public class Operation : Projector
{
    public OperatorToken Op { get; set; }

    public ShaderToken Left { get; set; }
    public ShaderToken Right { get; set; }

    Type? inferredType;
    public override Type InferredType
    {
        get => inferredType ?? typeof(void);
        set => inferredType = value;
    }

    public override ShaderToken ProjectConstant()
    {

        if (Left is Projector)
            Left = ((Projector)Left).ProjectConstant();
        if (Right is Projector)
            Right = ((Projector)Right).ProjectConstant();

        return (Left, Right) switch
        {
            (NumberLiteral ln, NumberLiteral rn) => ApplyOperation(Op, ln, rn),
            (BoolLiteral ln, BoolLiteral rn) => ApplyOperation(Op, ln, rn),
            (StringLiteral ln, StringLiteral rn) => ApplyOperation(Op, ln, rn),
            _ => throw new Exception("Cannot process operation")
        };
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        MulExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new MulExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        SumExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new SumExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        ShiftExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new ShiftExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        AndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new AndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        XorExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new XorExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        OrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new OrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        TestExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new TestExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        EqualsExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new EqualsExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        LogicalAndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalAndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
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
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        LogicalOrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalOrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.ToOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class ConditionalExpression : Projector
{
    public ShaderToken Condition { get; set; }
    public ShaderToken TrueOutput { get; set; }
    public ShaderToken FalseOutput { get; set; }

    Type? inferredType;

    public override Type InferredType
    {
        get => inferredType ?? typeof(void);
        set => inferredType = value;
    }

    public ConditionalExpression(Match m)
    {
        Condition = GetToken(m.Matches[0]);
        TrueOutput = GetToken(m.Matches[1]);
        FalseOutput = GetToken(m.Matches[2]);
    }

    public override ShaderToken ProjectConstant()
    {
        if (Condition is Projector)
            Condition = ((Projector)Condition).ProjectConstant();

        if (TrueOutput is Projector )
            TrueOutput= ((Projector)TrueOutput).ProjectConstant();

        if (FalseOutput is Projector )
            FalseOutput= ((Projector)FalseOutput).ProjectConstant();

        if (Condition is BoolLiteral c)
            return c.Value ? TrueOutput : FalseOutput;
        else
            throw new Exception("Invalid condition");
    }
}


public class MethodCall : ShaderToken
{
    public string MethodName { get; set; }
    public IEnumerable<ShaderToken> Parameters { get; set; }

    public MethodCall(Match m)
    {
        Match = m;
        MethodName = m.Matches.First().StringValue;
        Parameters = m.Matches.Where(x => x.Name == "PrimaryExpression").Select(GetToken).ToList();
    }
}
