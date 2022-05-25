using Eto.Parse;
using Stride.Shader.Parsing.AST.Directives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shader.Parsing.AST.Directives.OperatorTokenExtensions;

namespace Stride.Shader.Parsing.AST.Directives;

public abstract class Projector : DirectiveToken
{
    public override Type InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void EvaluateMacros(Dictionary<string, object> macros)
    {
        throw new NotImplementedException();
    }

    public abstract DirectiveToken ProjectConstant();
}

public class Operation : Projector
{
    public OperatorToken Op { get; set; }

    public DirectiveToken Left { get; set; }
    public DirectiveToken Right { get; set; }

    Type? inferredType;
    public override Type InferredType
    {
        get => inferredType ?? typeof(void);
        set => inferredType = value;
    }
    public override void EvaluateMacros(Dictionary<string, object> macros)
    {
        if (Left is VariableNameLiteral vl)
        {
            if (macros.TryGetValue(vl.Name, out object value))
            {
                Left = value switch
                {
                    float v => new NumberLiteral { Value = v },
                    double v => new NumberLiteral { Value = v },
                    byte v => new NumberLiteral { Value = v },
                    ushort v => new NumberLiteral { Value = v },
                    uint v => new NumberLiteral { Value = v },
                    ulong v => new NumberLiteral { Value = v },
                    int v => new NumberLiteral { Value = v },
                    long v => new NumberLiteral { Value = v },
                    short v => new NumberLiteral { Value = v },
                    sbyte v => new NumberLiteral { Value = v },
                    bool v => new BoolLiteral { Value = v },
                    string v => new StringLiteral { Value = v },
                    _ => throw new Exception("Unusable type")
                };
            }
            else
                throw new Exception("Macro does not exist");
        }
        if (Right is VariableNameLiteral vr)
        {
            if (macros.TryGetValue(vr.Name, out object value))
            {
                Right = value switch
                {
                    float v => new NumberLiteral { Value = v },
                    double v => new NumberLiteral { Value = v },
                    byte v => new NumberLiteral { Value = v },
                    ushort v => new NumberLiteral { Value = v },
                    uint v => new NumberLiteral { Value = v },
                    ulong v => new NumberLiteral { Value = v },
                    int v => new NumberLiteral { Value = v },
                    long v => new NumberLiteral { Value = v },
                    short v => new NumberLiteral { Value = v },
                    sbyte v => new NumberLiteral { Value = v },
                    bool v => new BoolLiteral { Value = v },
                    string v => new StringLiteral { Value = v },
                    _ => throw new Exception("Unusable type")
                };
            }
            else
                throw new Exception("Macro does not exist");
        }
    }

    public override DirectiveToken ProjectConstant()
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        MulExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new MulExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        SumExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new SumExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        ShiftExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new ShiftExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        AndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new AndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        XorExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new XorExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        OrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new OrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        TestExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new TestExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        EqualsExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new EqualsExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        LogicalAndExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalAndExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
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
            Op = m.Matches[1].StringValue.AsOperatorToken(),
            Left = GetToken(m.Matches[0]),
            Right = GetToken(m.Matches[2])
        };

        LogicalOrExpression tmp = first;
        for (int i = 3; i < m.Matches.Count - 2; i += 2)
        {
            tmp = new LogicalOrExpression
            {
                Match = m,
                Op = m.Matches[i].StringValue.AsOperatorToken(),
                Left = tmp,
                Right = GetToken(m.Matches[i + 1])
            };
        }
        return tmp;
    }
}

public class ConditionalExpression : Projector
{
    public DirectiveToken Condition { get; set; }
    public DirectiveToken TrueOutput { get; set; }
    public DirectiveToken FalseOutput { get; set; }

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

    public override void EvaluateMacros(Dictionary<string, object> macros)
    {
        if (Condition is VariableNameLiteral vcondition)
        {
            if (macros.TryGetValue(vcondition.Name, out object value))
            {
                vcondition.Value = value;
                vcondition.InferredType = value.GetType();
            }
            else
                throw new Exception("Macro does not exist");
        }
        if (TrueOutput is VariableNameLiteral tout)
        {
            if (macros.TryGetValue(tout.Name, out object value))
            {
                tout.Value = value;
                tout.InferredType = value.GetType();
            }
            else
                throw new Exception("Macro does not exist");
        }
        if (FalseOutput is VariableNameLiteral fout)
        {
            if (macros.TryGetValue(fout.Name, out object value))
            {
                fout.Value = value;
                fout.InferredType = value.GetType();
            }
            else
                throw new Exception("Macro does not exist");
        }

    }

    public override DirectiveToken ProjectConstant()
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
