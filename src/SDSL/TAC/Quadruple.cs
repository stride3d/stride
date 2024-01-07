namespace SDSL.TAC;


public record struct Quadruple(Operator Operator, Operand? Operand1, Operand? Operand2, Operand? Result)
{
    public static implicit operator Quadruple(ValueTuple<Operator,Operand,Operand,Operand> q) => new(q.Item1, q.Item2, q.Item3, q.Item4);
}