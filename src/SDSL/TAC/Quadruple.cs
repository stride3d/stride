using SDSL.Parsing.AST.Shader;

namespace SDSL.TAC;


public record struct Quadruple(Operator Operator, Operand? Operand1 = null, Operand? Operand2 = null, Operand? Result = null)
{
    public static Quadruple Nop => new(Operator.Nop);

    public static implicit operator Quadruple(in ValueTuple<Operator,Operand,Operand,Operand> q) => new(q.Item1, q.Item2, q.Item3, q.Item4);
    public override readonly string ToString()
    {

        return Operator switch
        {
            Operator.Access => $"{Result} = {Operand1}[{Operand2}]",
            Operator.Call => $"{Result} = Call {Operand1}",
            Operator.Goto => $"Goto {Operand1}",
            Operator.If => $"If {Operand1} Goto {Operand2}",
            Operator.PushParam => $"PushParam {Operand1}",
            Operator.Load => $"{Result} = Load {Operand1}",
            Operator.Declare => $"{Operator} {Result} = {Operand1}{Operand2}",
            Operator.Nop => "",
            _ => $"{Result} = {Operand1} {Operator} {Operand2}"
        };
    }
}