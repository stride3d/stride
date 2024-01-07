using SDSL.Parsing.AST.Shader;

namespace SDSL.TAC;


public record struct Quadruple(Operator Operator, Operand? Operand1 = null, Operand? Operand2 = null, Operand? Result = null)
{
    public static Quadruple Nop => new(Operator.Nop);

    public static implicit operator Quadruple(in ValueTuple<Operator,Operand,Operand,Operand> q) => new(q.Item1, q.Item2, q.Item3, q.Item4);
}