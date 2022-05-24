using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Expressions;

public class Operation : ExpressionToken
{
    public string Op { get; set; }
    public ExpressionToken Left { get; set; }
    public ExpressionToken Right { get; set; }
}

public class TermExpression
{
    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Name: "Literals" } => Literals.GetSubToken(m.Matches[0]),
            { Name: "IntegerLiteral" } => new NumberLiteral(m),

            //{ Name: "VariableTerm" } => new VariableTerm(m.Matches[0]),
            _ => throw new NotImplementedException()
        };
    }
}


public class PrefixIncrement : ExpressionToken
{
    public string Operator { get; set; }
    public string Name { get; set; }
    public PrefixIncrement(Match m)
    {
        Match = m;
        Operator = m.Matches[0].StringValue;
        Name = m.Matches[1].StringValue;
    }
}

public class PostfixExpression : ExpressionToken
{
    
    public PostfixExpression(Match m)
    {
        
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Name: "TermExpression" } => TermExpression.GetSubToken(m.Matches[0]),
            //{ Name: "PostfixIncrement" } => new PostfixIncrement(m.Matches[0]),
            //{ Name: "ArrayAccessor" } => new ArrayAccessor(m.Matches[0]),
            //{ Name: "AccessorChain" } => new AccessorChain(m.Matches[0]),
            _ => throw new NotImplementedException()
        };
    }

}

public static class UnaryExpression
{
    public static ExpressionToken GetSubToken(Match m)
    {
        
        return m switch
        {
            { Name: "PostfixExpression" } => PostfixExpression.GetSubToken(m.Matches[0]),
            { Name: "PrefixIncrement" } => new PrefixIncrement(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class CastExpression : ExpressionToken
{
    public CastExpression(Match m)
    {
        // TODO implement cast
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => UnaryExpression.GetSubToken(m.Matches[0].Matches[0]),
            { Matches.Count: > 1 } => new CastExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class MulExpression : ExpressionToken
{
    public Operation Operations;
    public MulExpression(Match m)
    {
        var first = new Operation
        {
            Op = m.Matches[1].StringValue,
            Left = CastExpression.GetSubToken(m.Matches[0]),
            Right = CastExpression.GetSubToken(m.Matches[2])
        };

        Operation tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new Operation
            {
                Op = m.Matches[i].StringValue,
                Left = tmp,
                Right = OrExpression.GetSubToken(m.Matches[i + 1])
            };
        }
        Operations = tmp;
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => CastExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new MulExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class SumExpression : ExpressionToken
{
    public Operation Operations;
    public SumExpression(Match m)
    {
        var first = new Operation
        {
            Op = m.Matches[1].StringValue,
            Left = MulExpression.GetSubToken(m.Matches[0]),
            Right = MulExpression.GetSubToken(m.Matches[2])
        };

        Operation tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new Operation
            {
                Op = m.Matches[i].StringValue,
                Left = tmp,
                Right = MulExpression.GetSubToken(m.Matches[i + 1])
            };
        }
        Operations = tmp;
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => MulExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new SumExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class ShiftExpression : ExpressionToken
{
    public Operation Operations;
    public ShiftExpression(Match m)
    {
        var first = new Operation
        {
            Op = m.Matches[1].StringValue,
            Left = SumExpression.GetSubToken(m.Matches[0]),
            Right = SumExpression.GetSubToken(m.Matches[2])
        };

        Operation tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new Operation
            {
                Op = m.Matches[i].StringValue,
                Left = tmp,
                Right = SumExpression.GetSubToken(m.Matches[i + 1])
            };
        }
        Operations = tmp;
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => SumExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new ShiftExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class AndExpression : ExpressionToken
{
    public IEnumerable<ExpressionToken> Values;
    public AndExpression(Match m)
    {
        Values = m.Matches.Where(x => x.Name != "Operator").Select(ShiftExpression.GetSubToken);
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => ShiftExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new AndExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}
public class XorExpression : ExpressionToken
{
    public IEnumerable<ExpressionToken> Values;
    public XorExpression(Match m)
    {
        Values = m.Matches.Where(x => x.Name != "Operator").Select(AndExpression.GetSubToken);
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => AndExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new XorExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}
public class OrExpression : ExpressionToken
{
    public IEnumerable<ExpressionToken> Values;
    public OrExpression(Match m)
    {
        Values = m.Matches.Where(x => x.Name != "Operator").Select(XorExpression.GetSubToken);
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => XorExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new OrExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class TestExpression : ExpressionToken
{
    public Operation Operations;
    public TestExpression(Match m)
    {
        var first = new Operation
        {
            Op = m.Matches[1].StringValue,
            Left = OrExpression.GetSubToken(m.Matches[0]),
            Right = OrExpression.GetSubToken(m.Matches[2])
        };

        Operation tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new Operation
            {
                Op = m.Matches[i].StringValue,
                Left = tmp,
                Right = OrExpression.GetSubToken(m.Matches[i + 1])
            };
        }
        Operations = tmp;

    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => OrExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new TestExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class EqualsExpression : ExpressionToken
{
    public Operation Operations;
    public EqualsExpression(Match m)
    {
        var first = new Operation
        {
            Op = m.Matches[1].StringValue,
            Left = TestExpression.GetSubToken(m.Matches[0]),
            Right = TestExpression.GetSubToken(m.Matches[2])
        };
        
        Operation tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new Operation
            {
                Op = m.Matches[i].StringValue,
                Left = tmp,
                Right = TestExpression.GetSubToken(m.Matches[i + 1])
            };
        }
        Operations = tmp;
        
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => TestExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new EqualsExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class LogicalAndExpression : ExpressionToken
{
    public IEnumerable<ExpressionToken> Values;
    public LogicalAndExpression(Match m)
    {
        Values = m.Matches.Where(x => x.Name != "Operator").Select(EqualsExpression.GetSubToken);
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count: 1 } => EqualsExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: > 1 } => new LogicalAndExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}
public class LogicalOrExpression : ExpressionToken
{
    public IEnumerable<ExpressionToken> Values;
    public LogicalOrExpression(Match m)
    {
        Values = m.Matches.Where(x => x.Name != "Operator").Select(LogicalAndExpression.GetSubToken);
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        return m switch
        {
            { Matches.Count : 1} => LogicalAndExpression.GetSubToken(m.Matches[0]),
            { Matches.Count: >1} => new LogicalOrExpression(m),
            _ => throw new NotImplementedException()
        };
    }
}

public class ConditionalExpression : ExpressionToken
{
    public ExpressionToken Condition { get; set; }
    public ExpressionToken TrueOutput { get; set; }
    public ExpressionToken FalseOutput { get; set; }


    public ConditionalExpression(Match m)
    {
        Condition = LogicalOrExpression.GetSubToken(m.Matches[0]);
        TrueOutput = LogicalOrExpression.GetSubToken(m.Matches[1]);
        FalseOutput = LogicalOrExpression.GetSubToken(m.Matches[2]);
    }

    public static ExpressionToken GetSubToken(Match m)
    {
        var tmp = m.Matches[0];
        return tmp switch
        {
            { Name: "LogicalOrExpression", Matches.Count: 1} => LogicalOrExpression.GetSubToken(m.Matches[0]),
            { Name: "Ternary" } => new ConditionalExpression(tmp),
            _ => throw new NotImplementedException()
        };
    }
}


public class PrimaryExpression : ExpressionToken
{
    public static ExpressionToken GetSubToken(Match m)
    {
        return m.Matches[0].Name switch
        {
            "ConditionalExpression" => ConditionalExpression.GetSubToken(m.Matches[0]),
            _ => throw new NotImplementedException()
        };
    }
}

